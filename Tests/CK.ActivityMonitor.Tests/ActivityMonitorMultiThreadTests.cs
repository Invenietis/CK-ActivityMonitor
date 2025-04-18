using Shouldly;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;



namespace CK.Core.Tests.Monitoring;

public class ActivityMonitorMultiThreadTests
{
    internal class BuggyActivityMonitorClient : ActivityMonitorClient
    {
        readonly IActivityMonitor _monitor;
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
        readonly int _number;
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
        readonly Action _action;
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

        monitor.Output.Clients.Length.ShouldBe( clientCount );

        try
        {
            _ = Task.Run( () => monitor.Info( "Test must work in task" ) );

            client.WaitForOnUnfilteredLog();

            var expectedMessage = Impl.ActivityMonitorResources.ActivityMonitorConcurrentThreadAccess
                                        .Replace( "{0}", ".*" ).Replace( "{1}", ".*" ).Replace( "{2}", ".*" );
            Util.Invokable( () => monitor.Info( "Test must fail" ) )
                   .ShouldThrow<CKException>()
                   .Message.ShouldMatch( expectedMessage );

            monitor.Output.Clients.Length.ShouldBe( clientCount, $"Still {clientCount}: Concurrent call: not the fault of the Client." );
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
              Util.Invokable( () => monitor.Info( "Test must fail reentrant client" ) )
                     .ShouldThrow<CKException>()
                     .Message.ShouldMatch( Impl.ActivityMonitorResources.ActivityMonitorReentrancyError.Replace( "{0}", ".*" ) );
          } ) );

        monitor.Info( "Test must work after reentrant client" );
        monitor.Output.Clients.Length.ShouldBe( clientCount, "The RegisterClient action above is ok: it checks that it triggered a reentrant call." );

        ++clientCount;
        monitor.Output.RegisterClient( new ActionActivityMonitorClient( () =>
          {
              monitor.Info( "Test must fail reentrant client" );
          } ) );

        monitor.Info( "Test must work after reentrant client" );
        monitor.Output.Clients.Length.ShouldBe( clientCount - 1, "The BUGGY RegisterClient action above is NOT ok: it triggers a reentrant call exception => We have removed it." );
    }

    [Test]
    public void simple_reentrancy_detection()
    {
        IActivityMonitor monitor = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );
        int clientCount = monitor.Output.Clients.Length;
        monitor.Output.Clients.Length.ShouldBe( clientCount );

        BuggyActivityMonitorClient client = new BuggyActivityMonitorClient( monitor );
        monitor.Output.RegisterClient( client );
        monitor.Output.Clients.Length.ShouldBe( clientCount + 1 );
        monitor.Info( "Test" );
        monitor.Output.Clients.Length.ShouldBe( clientCount );

        monitor.Info( "Test" );
    }

    [Test]
    public void concurrent_access_are_detected()
    {
        IActivityMonitor monitor = new ActivityMonitor();
        if( TestHelper.LogsToConsole ) monitor.Output.RegisterClient( new ActivityMonitorConsoleClient() );

        // Artificially slows down logging to ensure that concurrent access occurs.
        monitor.Output.RegisterClient( new ActionActivityMonitorClient( () => Thread.Sleep( 50 ) ) );
        AggregateException? ex = ConcurrentThrow( monitor );
        ex.ShouldNotBeNull();

        monitor.Info( "Test" );
    }

    static AggregateException? ConcurrentThrow( IActivityMonitor monitor )
    {
        object lockTasks = new object();
        object lockRunner = new object();
        int enteredThread = 0;

        void GetLock()
        {
            lock( lockTasks )
            {
                Interlocked.Increment( ref enteredThread );
                lock( lockRunner )
                    Monitor.Pulse( lockRunner );
                Monitor.Wait( lockTasks );
            }
        }

        Task[] tasks = new Task[]
        {
            new Task( () => { GetLock(); monitor.Info( "Test T1" ); } ),
            new Task( () => { GetLock(); monitor.Info( "Test T2", new Exception() ); } ),
            new Task( () => { GetLock(); monitor.Info( "Test T3" ); } )
        };

        try
        {
            foreach( var t in tasks ) t.Start();

            lock( lockRunner )
                while( enteredThread < tasks.Length )
                    Monitor.Wait( lockRunner );

            lock( lockTasks )
                Monitor.PulseAll( lockTasks );

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            Task.WaitAll( tasks );
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
        }
        catch( AggregateException ex )
        {
            tasks.Where( x => x.IsFaulted )
                 .SelectMany( x => x.Exception!.Flatten().InnerExceptions )
                 .ShouldAll( x => x.ShouldBeOfType<CKException>() );
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
        monitor.AutoTags.AtomicTraits.Contains( ActivityMonitor.Tags.StackTrace ).ShouldBeTrue();
        // 2 - ...or the & (Intersect) operator can do the job.
        ((monitor.AutoTags & ActivityMonitor.Tags.StackTrace) == ActivityMonitor.Tags.StackTrace).ShouldBeTrue();

        CheckConccurrentException( monitor, true );
        monitor.AutoTags = monitor.AutoTags.Except( ActivityMonitor.Tags.StackTrace );
        CheckConccurrentException( monitor, false );

        static void CheckConccurrentException( IActivityMonitor m, bool mustHaveCallStack )
        {
            AggregateException? ex = ConcurrentThrow( m );
            ex.ShouldNotBeNull();
            CKException one = ex!.Flatten().InnerExceptions.OfType<CKException>().First();
            if( mustHaveCallStack )
            {
                one.Message.ShouldContain( "-- Other Monitor's StackTrace" );
            }
            else
            {
                one.Message.ShouldNotContain( "-- Other Monitor's StackTrace" );
            }
        }
    }
}
