using FluentAssertions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

#nullable enable

namespace CK.Core.Tests.Monitoring
{
    public class ActivityMonitorMultiThreadTests
    {
        internal class BuggyActivityMonitorClient : ActivityMonitorClient
        {
            private IActivityMonitor _monitor;
            internal BuggyActivityMonitorClient( IActivityMonitor monitor )
            {
                _monitor = monitor;
            }

            protected override void OnUnfilteredLog( ref ActivityMonitorLogData data )
            {
                _monitor.Info( "I'm buggy: I'm logging back in my monitor!" );
                base.OnUnfilteredLog( ref data );
            }
        }

        internal class NotBuggyActivityMonitorClient : ActivityMonitorClient
        {
            private int _number;
            internal NotBuggyActivityMonitorClient( int number )
            {
                _number = number;
            }

            protected override void OnUnfilteredLog( ref ActivityMonitorLogData data )
            {
                if( TestHelper.LogsToConsole ) Console.WriteLine( "NotBuggyActivityMonitorClient.OnUnfilteredLog n°{0}: {1}", _number, data.Text );
            }
        }

        internal class ActionActivityMonitorClient : ActivityMonitorClient
        {
            Action _action;
            internal ActionActivityMonitorClient( Action log )
            {
                _action = log;
            }

            protected override void OnUnfilteredLog( ref ActivityMonitorLogData data )
            {
                _action();
            }
        }

        internal class WaitActivityMonitorClient : ActivityMonitorClient
        {
            readonly object _locker = new object();
            bool _done = false;

            readonly object _outLocker = new object();
            bool _outDone = false;

            protected override void OnUnfilteredLog( ref ActivityMonitorLogData data )
            {
                lock( _locker )
                {
                    lock( _outLocker )
                    {
                        _outDone = true;
                        Monitor.PulseAll( _outLocker );
                    }
                    while( !_done )
                        Monitor.Wait( _locker );
                }
            }

            internal void WaitForOnUnfilteredLog()
            {
                lock( _outLocker )
                    while( !_outDone )
                        Monitor.Wait( _outLocker );
            }

            internal void Free()
            {
                lock( _locker )
                {
                    _done = true;
                    Monitor.PulseAll( _locker );
                }
            }
        }

        [Test]
        public void buggy_clients_are_removed_from_Output()
        {
            ActivityMonitor.AutoConfiguration = null;
            ActivityMonitor monitor = new ActivityMonitor();

            int clientCount = 0;
            if( TestHelper.LogsToConsole )
            {
                ++clientCount;
                monitor.Output.RegisterClient( new ActivityMonitorConsoleClient() );
            }
            ++clientCount;
            WaitActivityMonitorClient client = monitor.Output.RegisterClient( new WaitActivityMonitorClient() );

            monitor.Output.Clients.Should().HaveCount( clientCount );

            try
            {
                _ = Task.Factory.StartNew( () => monitor.Info( "Test must work in task" ) );

                client.WaitForOnUnfilteredLog();

                var expectedMessage = Impl.ActivityMonitorResources.ActivityMonitorConcurrentThreadAccess.Replace( "{0}", "*" ).Replace( "{1}", "*" ).Replace( "{2}", "*" );
                monitor.Invoking( sut => sut.Info( "Test must fail" ) )
                       .Should().Throw<CKException>()
                       .WithMessage( expectedMessage );

                monitor.Output.Clients.Should().HaveCount( clientCount, $"Still {clientCount}: Concurrent call: not the fault of the Client." );
            }
            finally
            {
                client.Free();
            }

            Thread.Sleep( 50 );
            monitor.Info( "Test must work after task" );

            ++clientCount;
            monitor.Output.RegisterClient( new ActionActivityMonitorClient( () =>
              {
                  monitor.Invoking( sut => sut.Info( "Test must fail reentrant client" ) )
                         .Should().Throw<CKException>()
                         .WithMessage( Impl.ActivityMonitorResources.ActivityMonitorReentrancyError.Replace( "{0}", "*" ) );
              } ) );

            monitor.Info( "Test must work after reentrant client" );
            monitor.Output.Clients.Should().HaveCount( clientCount, "The RegisterClient action above is ok: it checks that it triggered a reentrant call." );

            ++clientCount;
            monitor.Output.RegisterClient( new ActionActivityMonitorClient( () =>
              {
                  monitor.Info( "Test must fail reentrant client" );
              } ) );

            monitor.Info( "Test must work after reentrant client" );
            monitor.Output.Clients.Should().HaveCount( clientCount - 1, "The BUGGY RegisterClient action above is NOT ok: it triggers a reentrant call exception => We have removed it." );
        }

        [Test]
        public void simple_reentrancy_detection()
        {
            IActivityMonitor monitor = new ActivityMonitor();
            using( monitor.Output.CreateBridgeTo( TestHelper.Monitor.Output.BridgeTarget ) )
            {
                int clientCount = monitor.Output.Clients.Count;
                monitor.Output.Clients.Should().HaveCount( clientCount );

                BuggyActivityMonitorClient client = new BuggyActivityMonitorClient( monitor );
                monitor.Output.RegisterClient( client );
                monitor.Output.Clients.Should().HaveCount( clientCount + 1 );
                monitor.Info( "Test" );
                monitor.Output.Clients.Should().HaveCount( clientCount );

                monitor.Info( "Test" );
            }
        }

        [Test]
        public void concurrent_access_are_detected()
        {
            IActivityMonitor monitor = new ActivityMonitor();
            if( TestHelper.LogsToConsole ) monitor.Output.RegisterClient( new ActivityMonitorConsoleClient() );

            // Artificially slows down logging to ensure that concurrent access occurs.
            monitor.Output.RegisterClient( new ActionActivityMonitorClient( () => Thread.Sleep( 50 ) ) );
            AggregateException? ex = ConcurrentThrow( monitor );
            ex.Should().NotBeNull();

            monitor.Info( "Test" );
        }

        static AggregateException? ConcurrentThrow( IActivityMonitor monitor )
        {
            object lockTasks = new object();
            object lockRunner = new object();
            int enteredThread = 0;

            Action getLock = () =>
            {
                lock( lockTasks )
                {
                    Interlocked.Increment( ref enteredThread );
                    lock( lockRunner )
                        Monitor.Pulse( lockRunner );
                    Monitor.Wait( lockTasks );
                }
            };

            Task[] tasks = new Task[]
            {
                new Task( () => { getLock(); monitor.Info( "Test T1" ); } ),
                new Task( () => { getLock(); monitor.Info( "Test T2", new Exception() ); } ),
                new Task( () => { getLock(); monitor.Info( "Test T3" ); } )
            };

            try
            {
                foreach( var t in tasks ) t.Start();

                lock( lockRunner )
                    while( enteredThread < tasks.Length )
                        Monitor.Wait( lockRunner );

                lock( lockTasks )
                    Monitor.PulseAll( lockTasks );

                Task.WaitAll( tasks );
            }
            catch( AggregateException ex )
            {
                tasks.Where( x => x.IsFaulted )
                     .SelectMany( x => x.Exception!.Flatten().InnerExceptions )
                     .Should().AllBeOfType<CKException>();
                return ex;
            }
            return null;
        }

        [Test]
        public void StackTrace_is_available_on_concurrent_errors_thanks_to_the_StackTrace_tag()
        {
            IActivityMonitor monitor = new ActivityMonitor();
            // Artificially slows down logging to ensure that concurrent access occurs.
            monitor.Output.RegisterClient( new ActionActivityMonitorClient( () => Thread.Sleep( 80 ) ) );
            // Use a temporary bridge to redirect the logs to the TestHelper.Monitor.
            //using( monitor.Output.CreateBridgeTo( TestHelper.Monitor.Output.BridgeTarget ) )
            {
                CheckConccurrentException( monitor, false );
                // This activates the Concurrent Access stack trace capture.
                monitor.AutoTags += ActivityMonitor.Tags.StackTrace;
                CheckConccurrentException( monitor, true );
                using( monitor.OpenInfo( $"No more Stack in this group." ) )
                {
                    // This removes the tracking.
                    monitor.AutoTags -= ActivityMonitor.Tags.StackTrace;
                    CheckConccurrentException( monitor, false );
                }
                // AutoTags are preserved (just like MinimalFilter).

                // To test if the StackTrace is enabled:
                // 1 - The Contains method can be used on the Atomic traits...
                monitor.AutoTags.AtomicTraits.Contains( ActivityMonitor.Tags.StackTrace ).Should().BeTrue();
                // 2 - ...or the & (Intersect) operator can do the job.
                ((monitor.AutoTags & ActivityMonitor.Tags.StackTrace) == ActivityMonitor.Tags.StackTrace).Should().BeTrue();

                CheckConccurrentException( monitor, true );
                monitor.AutoTags = monitor.AutoTags.Except( ActivityMonitor.Tags.StackTrace );
                CheckConccurrentException( monitor, false );
            }

            static void CheckConccurrentException( IActivityMonitor m, bool mustHaveCallStack )
            {
                AggregateException? ex = ConcurrentThrow( m );
                ex.Should().NotBeNull();
                CKException one = ex!.Flatten().InnerExceptions.OfType<CKException>().First();
                if( mustHaveCallStack )
                {
                    one.Message.Should().Contain( "-- Other Monitor's StackTrace" );
                }
                else
                {
                    one.Message.Should().NotContain( "-- Other Monitor's StackTrace" );
                }
            }
        }
    }
}
