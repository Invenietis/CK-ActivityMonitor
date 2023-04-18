using System;
using System.IO;
using NUnit.Framework;
using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using CK.Core.LogHandler;
using System.Threading.Tasks;
using System.Reflection.Metadata;

namespace CK.Core.Tests.Monitoring
{
    public class DocumentationCodeSnippets
    {
        public class Program
        {
            public static void SampleMain()
            {
                // An ActivityMonitor is a lightweight object that is tied to non concurrent
                // (sequential) set of calls (this perfectly complies with async/await calls).
                var m = new ActivityMonitor();
                int onError = 0, onSuccess = 0;
                foreach( var f in Directory.GetFiles( Environment.CurrentDirectory ) )
                {
                    using( m.OpenTrace( $"Processing file '{f}'." ) )
                    {
                        try
                        {
                            if( ProcessFile( m, f ) )
                            {
                                ++onSuccess;
                            }
                            else
                            {
                                ++onError;
                            }
                        }
                        catch( Exception ex )
                        {
                            m.Error( $"Unhandled error while processing file '{f}'. Continuing.", ex );
                            ++onError;
                        }
                    }
                }
                m.Info( $"Done: {onSuccess} files succeed and {onError} failed." );
            }

            /// When consuming a monitor, we always use the IActivityMonitor interface.
            static bool ProcessFile( IActivityMonitor m, string f )
            {
                int ticks = Environment.TickCount;
                m.Debug( $"Ticks: {ticks} for '{f}'." );
                /// Quick and dirty way to return a (not really) random boolean.
                return ticks % 2 == 0;
            }
        }

        [Test]
        public void SimpleUsage()
        {
            var f = new FileInfo( Path.Combine( TestHelper.SolutionFolder, "Tests", "CK.ActivityMonitor.Tests", "DocumentationCodeSnippets.cs" ) );
            DemoLogs( TestHelper.Monitor, f, new Exception() );
            DemoOpenGroupFarFromPerfect( TestHelper.Monitor );
            DemoOpenGroupBetter( TestHelper.Monitor );
            DemoOpenGroupThisWorksFine( TestHelper.Monitor );
            DemoOpenGroupWithDynamicConclusion( TestHelper.Monitor );
            DoSomething( TestHelper.Monitor, f );
        }

        void DemoOpenGroupFarFromPerfect( IActivityMonitor m )
        {
            m.OpenInfo( "Doing things..." );
            // ...
            m.CloseGroup( "Success." );
        }

        void DemoOpenGroupBetter( IActivityMonitor m )
        {
            using( m.OpenInfo( "Doing things..." ) )
            {
                // ...
            }
        }

        void DemoOpenGroupThisWorksFine( IActivityMonitor m )
        {
            using( m.OpenInfo( "Doing things..." ) )
            {
                // ...
                m.CloseGroup( "Success." );
            }
        }

        class Product
        {
            public bool IsAlive = (Environment.TickCount & 1) == 0;
        }

        void DemoHandler( IActivityMonitor m )
        {
            var products = new List<Product>();
            m.Debug( $"There is { products.Where( p => p.IsAlive ).Count() } live products( out of { products.Count})." );

            // Rewritten as:
            bool shouldAppend;
            LineDebug text = new LineDebug( 34, 2, m, out shouldAppend );
            if( shouldAppend )
            {
                text.AppendLiteral( "There is " );
                text.AppendFormatted( products.Where( ( Product p ) => p.IsAlive ).Count() );
                text.AppendLiteral( " live products( out of " );
                text.AppendFormatted( products.Count );
                text.AppendLiteral( ")." );
            }
            m.Debug( text, 96, "C:\\Dev\\CK\\CK-Core-Projects\\CK-ActivityMonitor\\Tests\\CK.ActivityMonitor.Tests\\DocumentationCodeSnippets.cs" );
        }

        void DemoOpenGroupWithDynamicConclusion( IActivityMonitor m )
        {
            int nbProcessed = 0;
            using( m.OpenInfo( "Doing things..." )
                                .ConcludeWith( () => String.Format( "{0} files.", nbProcessed ) ) )
            {
                // ...
                nbProcessed += 21;
                m.CloseGroup( "Success." );
                // The user Group conclusion is: "Success. - 21 files." (the two conclusions are concatenated).
            }
        }

        

        void DemoLogs( IActivityMonitor m, FileInfo f, Exception ex )
        {
            m.Debug( $"Content is: {File.ReadAllText( f.FullName )}'." );
            m.Trace( $"Data from '{f.Name}' processed." );
            m.Info( $"An error occurred while processing '{f.Name}'. Process will be retried later.", ex );
            m.Warn( $"File '{f.Name}' is too big ({f.Length / 1024} Kb). It must be less than 50Kb." );
            m.Error( $"File '{f.Name}' cannot be processed." );
            m.Fatal( "This will cancel the whole operation.", ex );
        }

        void Create()
        {
            {
                var m = new ActivityMonitor();
            }
            {
                var m = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );
            }
            {
                IActivityMonitor m = new ActivityMonitor();
                var counter = new ActivityMonitorErrorCounter();
                m.Output.RegisterClient( counter );

                m.Fatal( "An horrible error occurred." );

                counter.Current.FatalCount.Should().Be( 1 );
                m.Output.UnregisterClient( counter );
            }
            {
                IActivityMonitor m = new ActivityMonitor();

                int errorCount = 0;
                using( m.OnError( () => ++errorCount ) )
                {
                    m.Fatal( "An horrible error occurred." );
                }
                 errorCount.Should().Be(1 );
            }
            {
                IActivityMonitor m = new ActivityMonitor();
                m.MinimalFilter = LogFilter.Off;
                // ...
                m.MinimalFilter = LogFilter.Trace;
            }
            {
                IActivityMonitor m = new ActivityMonitor();
                m.MinimalFilter = LogFilter.Terse;
                using( m.TemporarilySetMinimalFilter( LogFilter.Trace ) )
                {
                     m.ActualFilter.Should().Be(LogFilter.Trace );
                }
                 m.ActualFilter.Should().Be(LogFilter.Terse, "Filter has been restored to previous value." );
            }
            {
                IActivityMonitor m = new ActivityMonitor();
                m.MinimalFilter = LogFilter.Off;
                // ...
                using( m.OpenWarn( "Ouch..." ) )
                {
                     m.ActualFilter.Should().Be(LogFilter.Off );
                    m.MinimalFilter = LogFilter.Trace;
                    // ... in debug filter ...
                }
                 m.ActualFilter.Should().Be(LogFilter.Off, "Back to Off." );

                var strange = new LogFilter( LogLevelFilter.Fatal, LogLevelFilter.Trace );
            }
        }

        bool DoSomething( IActivityMonitor m, FileInfo file )
        {
            using( m.OpenInfo( $"Do something important on file '{file.Name}'." ) )
            {
                if( !file.Exists )
                {
                    m.Warn( "File does not exist." );
                }
                else
                {
                    m.Trace( $"File last modified at {file.LastWriteTimeUtc:T}. {file.Length} Kb to process." );
                    try
                    {
                        // ... Process file ...
                    }
                    catch( Exception ex )
                    {
                        m.Error( "While processing.", ex );
                        return false;
                    }
                }
                m.SetTopic( "Changing my mind. Keeping it as-is." );
                return true;
            }
        }

        [Test]
        public void OnError()
        {
            var monitor = new ActivityMonitor();

            int errorCount = 0;
            using( monitor.OnError( () => ++errorCount ) )
            {
                monitor.Info( "This is not an error." );
                monitor.Error( "Ouch!" );
            }
            errorCount.Should().Be( 1 );

        }

        sealed class ActionEvent : EventMonitoredArgs
        {
            public ActionEvent( IActivityMonitor monitor, Action<IActivityMonitor, int>? action )
                : base( monitor )
            {
                Action = action;
            }

            public Action<IActivityMonitor, int>? Action { get; }
        }

        [Test]
        public void demo_using_CollectTexts()
        {
            var monitor = new ActivityMonitor();

            EventHandler<ActionEvent>? sender = null;

            sender += OnAction;

            using( monitor.CollectTexts( out var texts ) )
            {
                sender.Invoke( null, new ActionEvent( monitor, ( monitor, i ) => monitor.Info( $"Action {i}" ) ) );
                sender.Invoke( monitor, new ActionEvent( monitor, null ) );
                texts.Should().BeEquivalentTo( new[]
                {
            "Received Action and executing it.",
            "Action 3712",
            "Received a null Action. Ignoring it."
        } );
            }

            static void OnAction( object? sender, ActionEvent e )
            {
                if( e.Action == null ) e.Monitor.Warn( "Received a null Action. Ignoring it." );
                else
                {
                    e.Monitor.Info( "Received Action and executing it." );
                    e.Action( e.Monitor, 3712 );
                }
            }
        }

    }
}
