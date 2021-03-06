using System;
using System.IO;
using NUnit.Framework;
using FluentAssertions;

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
            m.OpenInfo().Send( "Doing things..." );
            // ...
            m.CloseGroup( "Success." );
        }

        void DemoOpenGroupBetter( IActivityMonitor m )
        {
            using( m.OpenInfo().Send( "Doing things..." ) )
            {
                // ...
            }
        }

        void DemoOpenGroupThisWorksFine( IActivityMonitor m )
        {
            using( m.OpenInfo().Send( "Doing things..." ) )
            {
                // ...
                m.CloseGroup( "Success." );
            }
        }

        void DemoOpenGroupWithDynamicConclusion( IActivityMonitor m )
        {
            int nbProcessed = 0;
            using( m.OpenInfo().Send( "Doing things..." )
                                .ConcludeWith( () => String.Format( "{0} files.", nbProcessed ) ) )
            {
                // ...
                nbProcessed += 21;
                m.CloseGroup( "Success." );
                // The user Group conclusion is: "Success. - 21 files." (the two conclusions are concatenated).
            }
        }

        void DemoLogs(IActivityMonitor m, FileInfo f, Exception ex)
        {
            m.Debug().Send( info => $"Content is: {File.ReadAllText( info.FullName )}'.", f );
            m.Trace().Send("Data from '{0}' processed.", f.Name);
            m.Info().Send(ex, "An error occurred while processing '{0}'. Process will be retried later.", f.Name);
            m.Warn().Send("File '{0}' is too big ({1} Kb). It must be less than 50Kb.", f.Name, f.Length / 1024);
            m.Error().Send(ex, "File '{0}' can not be processed.", f.Name);
            m.Fatal().Send(ex, "This will cancel the whole operation.");
        }

        void Create()
        {
            {
                var m = new ActivityMonitor();
            }
            {
                var m = new ActivityMonitor( applyAutoConfigurations: false );
            }
            {
                IActivityMonitor m = new ActivityMonitor();
                var counter = new ActivityMonitorErrorCounter();
                m.Output.RegisterClient( counter );

                m.Fatal().Send( "An horrible error occurred." );

                 counter.Current.FatalCount.Should().Be( 1 );
                m.Output.UnregisterClient( counter );
            }
            {
                IActivityMonitor m = new ActivityMonitor();

                int errorCount = 0;
                using( m.OnError( () => ++errorCount ) )
                {
                    m.Fatal().Send( "An horrible error occurred." );
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
                using( m.OpenWarn().Send( "Ouch..." ) )
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
            using( m.OpenInfo().Send( "Do something important on file '{0}'.", file.Name ) )
            {
                if( !file.Exists )
                {
                    m.Warn().Send( "File does not exist." );
                }
                else
                {
                    m.Trace().Send( "File last modified at {1:T}. {0} Kb to process.", file.Length, file.LastWriteTimeUtc );
                    try
                    {
                        // ... Process file ...
                    }
                    catch( Exception ex )
                    {
                        m.Error().Send( ex, "While processing." );
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
    }
}
