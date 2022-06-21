using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using System.Xml.Linq;
using System.Collections.Generic;
using FluentAssertions;
using System.Diagnostics;

namespace CK.Core.Tests.Monitoring
{
    public class ActivityMonitorTests
    {
        [SetUp]
        public void ResetGlobalState()
        {
            TestHelper.Monitor.MinimalFilter = LogFilter.Undefined;
        }

        [Test]
        public void automatic_configuration_of_monitors_just_uses_ActivityMonitor_AutoConfiguration_delegate()
        {
            StupidStringClient c = new StupidStringClient();

            ActivityMonitor.AutoConfiguration = null;
            ActivityMonitor.AutoConfiguration += m => m.Output.RegisterClient( c );
            int i = 0;
            ActivityMonitor.AutoConfiguration += m => m.UnfilteredLog( LogLevel.Info, null, $"This monitors has been created at {DateTime.UtcNow:O}, n°{++i}", null );

            ActivityMonitor monitor1 = new ActivityMonitor();
            ActivityMonitor monitor2 = new ActivityMonitor();

            c.ToString().Should().Contain( "This monitors has been created at" );
            c.ToString().Should().Contain( "n°1" );
            c.ToString().Should().Contain( "n°2" );

            ActivityMonitor.AutoConfiguration = null;
        }

        [Test]
        public void registering_multiple_times_the_same_client_is_an_error()
        {
            ActivityMonitor.AutoConfiguration = null;
            IActivityMonitor monitor = new ActivityMonitor();
            monitor.Output.Clients.Should().HaveCount( 0 );

            var counter = new ActivityMonitorErrorCounter();
            monitor.Output.RegisterClient( counter );
            monitor.Output.Clients.Should().HaveCount( 1 );
            Action fail = () => TestHelper.Monitor.Output.RegisterClient( counter );
            fail.Should().Throw<InvalidOperationException>( "Counter can be registered in one source at a time." );

            var pathCatcher = new ActivityMonitorPathCatcher();
            monitor.Output.RegisterClient( pathCatcher );
            monitor.Output.Clients.Should().HaveCount( 2 );
            fail = () => TestHelper.Monitor.Output.RegisterClient( pathCatcher );
            fail.Should().Throw<InvalidOperationException>( "PathCatcher can be registered in one source at a time." );

            monitor.Output.UnregisterClient( counter );
            monitor.Output.UnregisterClient( pathCatcher );
            monitor.Output.Clients.Should().HaveCount( 0 );
        }

        [Test]
        public void registering_a_null_client_is_an_error()
        {
            IActivityMonitor monitor = new ActivityMonitor();
            Action fail = () => monitor.Output.RegisterClient( null! );
            fail.Should().Throw<ArgumentNullException>();
            fail = () => monitor.Output.UnregisterClient( null! );
            fail.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void RegisterUniqueClient_skips_null_client_from_factory_and_returns_null()
        {
            var monitor = new ActivityMonitor( applyAutoConfigurations: false );
            monitor.Output.RegisterUniqueClient<ActivityMonitorConsoleClient>( c => false, () => null ).Should().BeNull();
        }

        [Test]
        public void closing_group_restores_previous_AutoTags_and_MinimalFilter()
        {
            ActivityMonitor monitor = new ActivityMonitor();
            using( monitor.OpenTrace( "G1" ) )
            {
                monitor.AutoTags = ActivityMonitor.Tags.Register( "Tag" );
                monitor.MinimalFilter = LogFilter.Monitor;
                using( monitor.OpenWarn( "G2" ) )
                {
                    monitor.AutoTags = ActivityMonitor.Tags.Register( "A|B|C" );
                    monitor.MinimalFilter = LogFilter.Release;
                    monitor.AutoTags.ToString().Should().Be( "A|B|C" );
                    monitor.MinimalFilter.Should().Be( LogFilter.Release );
                }
                monitor.AutoTags.ToString().Should().Be( "Tag" );
                monitor.MinimalFilter.Should().Be( LogFilter.Monitor );
            }
            monitor.AutoTags.Should().BeSameAs( ActivityMonitor.Tags.Empty );
            monitor.MinimalFilter.Should().Be( LogFilter.Undefined );
        }

        [Test]
        public void Off_FilterLevel_prevents_all_logs_even_UnfilteredLogs()
        {
            var m = new ActivityMonitor( false );
            var c = m.Output.RegisterClient( new StupidStringClient() );
            m.Trace( "Trace1" );
            m.MinimalFilter = LogFilter.Off;
            m.UnfilteredLog( LogLevel.Fatal, ActivityMonitor.Tags.Empty, "NOSHOW-1", null );
            m.UnfilteredOpenGroup( LogLevel.Fatal, ActivityMonitor.Tags.Empty, "NOSHOW-2", null );
            m.UnfilteredLog( LogLevel.Error, ActivityMonitor.Tags.Empty, "NOSHOW-3", null );
            // Off will be restored by the group closing.
            m.MinimalFilter = LogFilter.Trace;
            m.CloseGroup( "NOSHOW-4" );
            m.MinimalFilter = LogFilter.Trace;
            m.Trace( "Trace2" );

            var s = c.ToString();
            s.Should().Contain( "Trace1" ).And.Contain( "Trace2" );
            s.Should().NotContain( "NOSHOW" );
        }

        [Test]
        public void sending_a_null_or_empty_text_is_transformed_into_no_log_text()
        {
            var m = new ActivityMonitor( false );
            var c = m.Output.RegisterClient( new StupidStringClient() );
            m.Trace( "" );
            m.UnfilteredLog( LogLevel.Error, null, null, null );
            m.OpenTrace( (Exception?)null! );
            m.OpenInfo( "" );

            c.Entries.Should().HaveCount( 4 );
            c.Entries.All( e => e.Data.Text == ActivityMonitor.NoLogText );
        }

        [Test]
        public void display_conclusions()
        {
            IActivityMonitor monitor = new ActivityMonitor( false );
            monitor.Output.RegisterClients( new StupidStringClient(), new StupidXmlClient( new StringWriter() ) );
            monitor.Output.Clients.Should().HaveCount( 2 );

            var tag1 = ActivityMonitor.Tags.Register( "Product" );
            var tag2 = ActivityMonitor.Tags.Register( "Sql" );
            var tag3 = ActivityMonitor.Tags.Register( "Combined Tag|Sql|Engine V2|Product" );

            using( monitor.OpenError( "MainGroupError" ).ConcludeWith( () => "EndMainGroupError" ) )
            {
                using( monitor.OpenTrace( "MainGroup" ).ConcludeWith( () => "EndMainGroup" ) )
                {
                    monitor.Trace( tag1, "First" );
                    using( monitor.TemporarilySetAutoTags( tag1 ) )
                    {
                        monitor.Trace( "Second" );
                        monitor.Trace( tag3, "Third" );
                        using( monitor.TemporarilySetAutoTags( tag2 ) )
                        {
                            monitor.Info( "First" );
                        }
                    }
                    using( monitor.OpenInfo( "InfoGroup" ).ConcludeWith( () => "Conclusion of Info Group (no newline)." ) )
                    {
                        monitor.Info( "Second" );
                        monitor.Trace( "Fourth" );

                        string warnConclusion = "Conclusion of Warn Group" + Environment.NewLine + "with more than one line int it.";
                        using( monitor.OpenWarn( $"WarnGroup {4} - Now = {DateTime.UtcNow}" ).ConcludeWith( () => warnConclusion ) )
                        {
                            monitor.Info( "Warn!" );
                            monitor.CloseGroup( "User conclusion with multiple lines."
                                + Environment.NewLine + "It will be displayed on "
                                + Environment.NewLine + "multiple lines." );
                        }
                        monitor.CloseGroup( "Conclusions on one line are displayed separated by dash." );
                    }
                }

                if( TestHelper.LogsToConsole )
                {
                    Console.WriteLine( monitor.Output.Clients.OfType<StupidStringClient>().Single().Writer );
                    Console.WriteLine( monitor.Output.Clients.OfType<StupidXmlClient>().Single().InnerWriter );
                }
            }

            IReadOnlyList<XElement> elements = monitor.Output.Clients.OfType<StupidXmlClient>().Single().XElements;

            elements.Descendants( "Info" ).Should().HaveCount( 3 );
            elements.Descendants( "Trace" ).Should().HaveCount( 2 );
        }

        [Test]
        public void exceptions_are_deeply_dumped()
        {
            IActivityMonitor l = new ActivityMonitor( applyAutoConfigurations: false );
            var wLogLovely = new StringBuilder();
            var rawLog = new StupidStringClient();
            l.Output.RegisterClient( rawLog );
            var logLovely = new ActivityMonitorTextWriterClient( ( s ) => wLogLovely.Append( s ) );
            l.Output.RegisterClient( logLovely );

            l.Error( new Exception( "EXERROR-1" ) );
            using( l.OpenFatal( "EXERROR-TEXT2", new Exception( "EXERROR-2" ) ) )
            {
                try
                {
                    throw new Exception( "EXERROR-3" );
                }
                catch( Exception ex )
                {
                    l.Trace( "EXERROR-TEXT3", ex );
                }
            }
            rawLog.ToString().Should().Contain( "EXERROR-1" );
            rawLog.ToString().Should().Contain( "EXERROR-2" ).And.Contain( "EXERROR-TEXT2" );
            rawLog.ToString().Should().Contain( "EXERROR-3" ).And.Contain( "EXERROR-TEXT3" );

            string text = wLogLovely.ToString();
            text.Should().Contain( "EXERROR-1" );
            text.Should().Contain( "EXERROR-2" ).And.Contain( "EXERROR-TEXT2" );
            text.Should().Contain( "EXERROR-3" ).And.Contain( "EXERROR-TEXT3" );
            text.Should().Contain( "Stack:" );
        }

        [Test]
        public void ending_a_monitor_send_an_unfilitered_MonitorEnd_tagged_info()
        {
            IActivityMonitor m = new ActivityMonitor( applyAutoConfigurations: false );
            var rawLog = new StupidStringClient();
            m.Output.RegisterClient( rawLog );
            m.OpenFatal( "a group" );
            // OpenFatal or OpenError sets their scoped filter to Debug.
            m.MinimalFilter = LogFilter.Release;
            m.OpenInfo( "a (filtered) group" );
            m.Fatal( "a line" );
            m.Info( "a (filtered) line" );
            m.MonitorEnd();
            m.CloseGroup().Should().BeFalse();
            string logs = rawLog.ToString();
            logs.Should().NotContain( "(filtered)" );
            logs.Should().Match( "*a group*a line*Done.*", "We used the default 'Done.' end text." );
        }

        [Test]
        public void AggregatedException_are_handled_specifically()
        {
            IActivityMonitor l = new ActivityMonitor( applyAutoConfigurations: false );
            var wLogLovely = new StringBuilder();
            var logLovely = new ActivityMonitorTextWriterClient( ( s ) => wLogLovely.Append( s ) );
            l.Output.RegisterClient( logLovely );


            l.Error( new Exception( "EXERROR-1" ) );
            using( l.OpenFatal( "EXERROR-TEXT2", new Exception( "EXERROR-2" ) ) )
            {
                try
                {
                    throw new AggregateException(
                        new Exception( "EXERROR-Aggreg-1" ),
                        new AggregateException(
                            new Exception( "EXERROR-Aggreg-2-1" ),
                            new Exception( "EXERROR-Aggreg-2-2" )
                        ),
                        new Exception( "EXERROR-Aggreg-3" ) );
                }
                catch( Exception ex )
                {
                    l.Error( "EXERROR-TEXT3", ex );
                }
            }
            string text = wLogLovely.ToString();
            text.Should().Contain( "EXERROR-Aggreg-1" );
            text.Should().Contain( "EXERROR-Aggreg-2-1" );
            text.Should().Contain( "EXERROR-Aggreg-2-2" );
            text.Should().Contain( "EXERROR-Aggreg-3" );
        }

        [Test]
        public void testing_filtering_levels()
        {
            LogFilter FatalFatal = new LogFilter( LogLevelFilter.Fatal, LogLevelFilter.Fatal );
            LogFilter WarnWarn = new LogFilter( LogLevelFilter.Warn, LogLevelFilter.Warn );

            IActivityMonitor l = new ActivityMonitor( false );
            var log = l.Output.RegisterClient( new StupidStringClient() );
            using( l.TemporarilySetMinimalFilter( LogLevelFilter.Error, LogLevelFilter.Error ) )
            {
                l.Debug( "NO SHOW" );
                l.Trace( "NO SHOW" );
                l.Info( "NO SHOW" );
                l.Warn( "NO SHOW" );
                l.Error( "Error n°1." );
                using( l.TemporarilySetMinimalFilter( WarnWarn ) )
                {
                    l.Debug( "NO SHOW" );
                    l.Trace( "NO SHOW" );
                    l.Info( "NO SHOW" );
                    l.Warn( "Warn n°1." );
                    l.Error( "Error n°2." );
                    using( l.OpenWarn( "GroupWarn: this appears." ) )
                    {
                        l.MinimalFilter.Should().Be( WarnWarn, "Groups does not change the current filter level." );
                        l.Debug( "NO SHOW" );
                        l.Trace( "NO SHOW" );
                        l.Info( "NO SHOW" );
                        l.Warn( "Warn n°2." );
                        l.Error( "Error n°3." );
                        // Changing the level inside a Group.
                        l.MinimalFilter = FatalFatal;
                        l.Error( "NO SHOW" );
                        l.Fatal( "Fatal n°1." );
                    }
                    using( l.OpenInfo( "GroupInfo: NO SHOW." ) )
                    {
                        l.MinimalFilter.Should().Be( WarnWarn, "Groups does not change the current filter level." );
                        l.Debug( "NO SHOW" );
                        l.Trace( "NO SHOW" );
                        l.Info( "NO SHOW" );
                        l.Warn( "Warn n°2-bis." );
                        l.Error( "Error n°3-bis." );
                        // Changing the level inside a Group.
                        l.MinimalFilter = FatalFatal;
                        l.Error( "NO SHOW" );
                        l.Fatal( "Fatal n°1." );
                        using( l.OpenError( "GroupError: NO SHOW." ) )
                        {
                        }
                    }
                    l.MinimalFilter.Should().Be( WarnWarn, "But Groups restores the original filter level when closed." );
                    l.Debug( "NO SHOW" );
                    l.Trace( "NO SHOW" );
                    l.Info( "NO SHOW" );
                    l.Warn( "Warn n°3." );
                    l.Error( "Error n°4." );
                    l.Fatal( "Fatal n°2." );
                }
                l.Debug( "NO SHOW" );
                l.Trace( "NO SHOW" );
                l.Info( "NO SHOW" );
                l.Warn( "NO SHOW" );
                l.Error( "Error n°5." );
            }
            string result = log.Writer.ToString();
            result.Should().NotContain( "NO SHOW" );
            result.Should().Contain( "Error n°1." )
                        .And.Contain( "Error n°2." )
                        .And.Contain( "Error n°3." )
                        .And.Contain( "Error n°3-bis." )
                        .And.Contain( "Error n°4." )
                        .And.Contain( "Error n°5." );
            result.Should().Contain( "Warn n°1." )
                        .And.Contain( "Warn n°2." )
                        .And.Contain( "Warn n°2-bis." )
                        .And.Contain( "Warn n°3." );
            result.Should().Contain( "Fatal n°1." )
                        .And.Contain( "Fatal n°2." );
        }

        [Test]
        public void mismatch_of_explicit_Group_disposing_is_handled()
        {
            IActivityMonitor l = new ActivityMonitor();
            var log = l.Output.RegisterClient( new StupidStringClient() );
            {
                IDisposable g0 = l.OpenTrace( "First" );
                IDisposable g1 = l.OpenTrace( "Second" );
                IDisposable g2 = l.OpenTrace( "Third" );

                g1.Dispose();
                l.Trace( "Inside First" );
                g0.Dispose();
                l.Trace( "At root" );

                var end = "Trace: Inside First" + Environment.NewLine + "-" + Environment.NewLine + "Trace: At root";
                log.Writer.ToString().Should().EndWith( end );
            }
            {
                // g2 is closed after g1.
                IDisposable g0 = l.OpenTrace( "First" );
                IDisposable g1 = l.OpenTrace( "Second" );
                IDisposable g2 = l.OpenTrace( "Third" );
                log.Writer.GetStringBuilder().Clear();
                g1.Dispose();
                // g2 has already been disposed by g1. 
                // Nothing changed.
                g2.Dispose();
                l.Trace( "Inside First" );
                g0.Dispose();
                l.Trace( "At root" );

                var end = "Trace: Inside First" + Environment.NewLine + "-" + Environment.NewLine + "Trace: At root";
                log.Writer.ToString().Should().EndWith( end );
            }
        }

        class ObjectAsConclusion
        {
            public override string ToString()
            {
                return "Explicit User Conclusion";
            }
        }

        [Test]
        public void appending_multiple_conclusions_to_a_group_is_possible()
        {
            IActivityMonitor l = new ActivityMonitor();
            l.Output.RegisterClient( new ActivityMonitorErrorCounter( true ) );
            var log = l.Output.RegisterClient( new StupidStringClient() );

            // No explicit close conclusion: Success!
            using( l.OpenTrace( "G" ).ConcludeWith( () => "From Opener" ) )
            {
                l.Error( "Pouf" );
                l.CloseGroup( new ObjectAsConclusion() );
            }
            log.Writer.ToString().Should().Contain( "Explicit User Conclusion, From Opener, 1 Error" );
        }

        [Test]
        public void ActivityMonitorPathCatcher_is_aClient_that_maintains_the_current_Group_path()
        {
            var monitor = new ActivityMonitor();
            ActivityMonitorPathCatcher p = monitor.Output.RegisterClient( new ActivityMonitorPathCatcher() );
            monitor.MinimalFilter = LogFilter.Debug;

            using( monitor.OpenDebug( "!D" ) )
            using( monitor.OpenTrace( "!T" ) )
            using( monitor.OpenInfo( "!I" ) )
            using( monitor.OpenWarn( "!W" ) )
            using( monitor.OpenError( "!E" ) )
            using( monitor.OpenFatal( "!F" ) )
            {
                p.DynamicPath.ToStringPath()
                   .Should().Contain( "!D" ).And.Contain( "!T" ).And.Contain( "!I" ).And.Contain( "!W" ).And.Contain( "!E" ).And.Contain( "!F" );
            }
        }

        [Test]
        public void ActivityMonitorPathCatcher_tests()
        {

            var monitor = new ActivityMonitor( applyAutoConfigurations: false );
            ActivityMonitorPathCatcher p = new ActivityMonitorPathCatcher();
            monitor.Output.RegisterClient( p );

            monitor.Trace( "Trace n°1" );
            p.DynamicPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ).Single().Should().Be( "Trace|Trace n°1" );
            p.LastErrorPath.Should().BeEmpty();
            p.LastWarnOrErrorPath.Should().BeEmpty();

            monitor.Trace( "Trace n°2" );
            p.DynamicPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ).Single().Should().Be( "Trace|Trace n°2" );
            p.LastErrorPath.Should().BeEmpty();
            p.LastWarnOrErrorPath.Should().BeEmpty();

            monitor.Warn( "W1" );
            p.DynamicPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ).Single().Should().Be( "Warn|W1" );
            p.LastErrorPath.Should().BeEmpty();
            p.LastWarnOrErrorPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ).Single().Should().Be( "Warn|W1" );

            monitor.Error( "E2" );
            monitor.Warn( "W1bis" );
            p.DynamicPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ).Single().Should().Be( "Warn|W1bis" );
            p.LastErrorPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ).Single().Should().Be( "Error|E2" );
            p.LastWarnOrErrorPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ).Single().Should().Be( "Warn|W1bis" );

            p.ClearLastWarnPath();
            p.LastErrorPath.Should().NotBeNull();
            p.LastWarnOrErrorPath.Should().BeEmpty();

            p.ClearLastErrorPath();
            p.LastErrorPath.Should().BeEmpty();

            using( monitor.OpenTrace( "G1" ) )
            {
                using( monitor.OpenInfo( "G2" ) )
                {
                    String.Join( ">", p.DynamicPath.Select( e => e.Text ) ).Should().Be( "G1>G2" );
                    p.LastErrorPath.Should().BeEmpty();
                    using( monitor.OpenTrace( "G3" ) )
                    {
                        using( monitor.OpenInfo( "G4" ) )
                        {
                            monitor.Warn( "W1" );

                            String.Join( ">", p.DynamicPath.Select( e => e.Text ) ).Should().Be( "G1>G2>G3>G4>W1" );

                            monitor.Info(
                                "Test With an exception: a Group is created. Since the text of the log is given, the Exception.Message must be displayed explicitly.",
                                new Exception( "An exception logged as an Info.",
                                    new Exception( "With an inner exception. Since these exceptions have not been thrown, there is no stack trace." ) )
                                    );

                            string.Join( ">", p.DynamicPath.Select( e => e.Text ) ).Should().Be( "G1>G2>G3>G4>Test With an exception: a Group is created. Since the text of the log is given, the Exception.Message must be displayed explicitly." );

                            try
                            {
                                try
                                {
                                    try
                                    {
                                        try
                                        {
                                            throw new Exception( "Deepest exception." );
                                        }
                                        catch( Exception ex )
                                        {
                                            throw new Exception( "Yet another inner with inner Exception.", ex );
                                        }
                                    }
                                    catch( Exception ex )
                                    {
                                        throw new Exception( "Exception with inner Exception.", ex );
                                    }
                                }
                                catch( Exception ex )
                                {
                                    throw new Exception( "Log without log text: the text of the entry is the Exception.Message.", ex );
                                }
                            }
                            catch( Exception ex )
                            {
                                monitor.Trace( ex );
                                p.DynamicPath.ToStringPath().Length.Should().BeGreaterThan( 0 );
                            }

                            p.LastErrorPath.Should().BeEmpty();
                            string.Join( ">", p.LastWarnOrErrorPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ) ).Should().Be( "Trace|G1>Info|G2>Trace|G3>Info|G4>Warn|W1" );
                        }
                        String.Join( ">", p.DynamicPath.Select( e => e.ToString() ) ).Should().Be( "G1>G2>G3>G4" );
                        p.LastErrorPath.Should().BeEmpty();
                        String.Join( ">", p.LastWarnOrErrorPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ) ).Should().Be( "Trace|G1>Info|G2>Trace|G3>Info|G4>Warn|W1" );

                        monitor.Error( "E1" );
                        String.Join( ">", p.DynamicPath.Select( e => e.Text ) ).Should().Be( "G1>G2>G3>E1" );
                        String.Join( ">", p.LastErrorPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ) ).Should().Be( "Trace|G1>Info|G2>Trace|G3>Error|E1" );
                        String.Join( ">", p.LastWarnOrErrorPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ) ).Should().Be( "Trace|G1>Info|G2>Trace|G3>Error|E1" );
                    }
                    String.Join( ">", p.DynamicPath.Select( e => e.Text ) ).Should().Be( "G1>G2>G3" );
                    String.Join( ">", p.LastErrorPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ) ).Should().Be( "Trace|G1>Info|G2>Trace|G3>Error|E1" );
                    String.Join( ">", p.LastWarnOrErrorPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ) ).Should().Be( "Trace|G1>Info|G2>Trace|G3>Error|E1" );
                }
                String.Join( ">", p.DynamicPath.Select( e => e.Text ) ).Should().Be( "G1>G2" );
                using( monitor.OpenTrace( "G2Bis" ) )
                {
                    String.Join( ">", p.DynamicPath.Select( e => e.Text ) ).Should().Be( "G1>G2Bis" );
                    String.Join( ">", p.LastErrorPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ) ).Should().Be( "Trace|G1>Info|G2>Trace|G3>Error|E1" );
                    String.Join( ">", p.LastWarnOrErrorPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ) ).Should().Be( "Trace|G1>Info|G2>Trace|G3>Error|E1" );

                    monitor.Warn( "W2" );
                    String.Join( ">", p.DynamicPath.Select( e => e.Text ) ).Should().Be( "G1>G2Bis>W2" );
                    String.Join( ">", p.LastWarnOrErrorPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ) ).Should().Be( "Trace|G1>Trace|G2Bis>Warn|W2" );
                    String.Join( ">", p.LastErrorPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ) ).Should().Be( "Trace|G1>Info|G2>Trace|G3>Error|E1" );
                }
                monitor.Fatal( "F1" );
                String.Join( ">", p.DynamicPath.Select( e => e.Text ) ).Should().Be( "G1>F1" );
                String.Join( ">", p.LastWarnOrErrorPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ) ).Should().Be( "Trace|G1>Fatal|F1" );
                String.Join( ">", p.LastErrorPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ) ).Should().Be( "Trace|G1>Fatal|F1" );

                // Extraneous closing are ignored.
                monitor.CloseGroup( null );

                monitor.Warn( "W3" );
                String.Join( ">", p.DynamicPath.Select( e => e.Text ) ).Should().Be( "W3" );
                String.Join( ">", p.LastWarnOrErrorPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ) ).Should().Be( "Warn|W3" );
                String.Join( ">", p.LastErrorPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ) ).Should().Be( "Trace|G1>Fatal|F1" );

                // Extraneous closing are ignored.
                monitor.CloseGroup( null );

                monitor.Warn( "W4" );
                String.Join( ">", p.DynamicPath.Select( e => e.Text ) ).Should().Be( "W4" );
                String.Join( ">", p.LastWarnOrErrorPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ) ).Should().Be( "Warn|W4" );
                String.Join( ">", p.LastErrorPath.Select( e => e.MaskedLevel.ToString() + '|' + e.Text ) ).Should().Be( "Trace|G1>Fatal|F1" );

                p.ClearLastWarnPath( true );
                p.LastErrorPath.Should().BeEmpty();
                p.LastWarnOrErrorPath.Should().BeEmpty();
            }
        }

        [Test]
        public void ActivityMonitorErrorCounter_and_ActivityMonitorPathCatcher_Clients_work_together()
        {
            var monitor = new ActivityMonitor( applyAutoConfigurations: false );
            // Registers the ErrorCounter first: it will be the last one to be called, but
            // this does not prevent the PathCatcher to work: the path elements reference the group
            // so that any conclusion arriving after PathCatcher.OnClosing are available.
            ActivityMonitorErrorCounter c = new ActivityMonitorErrorCounter();
            monitor.Output.RegisterClient( c );

            // Registers the PathCatcher now: it will be called BEFORE the ErrorCounter.
            ActivityMonitorPathCatcher p = new ActivityMonitorPathCatcher();
            monitor.Output.RegisterClient( p );

            c.GenerateConclusion.Should().BeFalse( "False by default." );
            c.GenerateConclusion = true;
            c.Root.MaxLogLevel.Should().Be( LogLevel.None );

            monitor.Trace( "T1" );
            c.Root.HasWarnOrError.Should().BeFalse();
            c.Root.HasError.Should().BeFalse();
            c.Root.MaxLogLevel.Should().Be( LogLevel.Trace );
            c.Root.ToString().Should().BeNull();

            monitor.Warn( "W1" );
            c.Root.HasWarnOrError.Should().BeTrue();
            c.Root.HasError.Should().BeFalse();
            c.Root.MaxLogLevel.Should().Be( LogLevel.Warn );
            c.Root.ToString().Should().NotBeNullOrEmpty();

            monitor.Error( "E2" );
            c.Root.HasWarnOrError.Should().BeTrue();
            c.Root.HasError.Should().BeTrue();
            c.Root.ErrorCount.Should().Be( 1 );
            c.Root.MaxLogLevel.Should().Be( LogLevel.Error );
            c.Root.ToString().Should().NotBeNullOrEmpty();

            c.Root.ClearError();
            c.Root.HasWarnOrError.Should().BeTrue();
            c.Root.HasError.Should().BeFalse();
            c.Root.ErrorCount.Should().Be( 0 );
            c.Root.MaxLogLevel.Should().Be( LogLevel.Warn );
            c.Root.ToString().Should().NotBeNull();

            c.Root.ClearWarn();
            c.Root.HasWarnOrError.Should().BeFalse();
            c.Root.HasError.Should().BeFalse();
            c.Root.MaxLogLevel.Should().Be( LogLevel.Info );
            c.Root.ToString().Should().BeNull();

            using( monitor.OpenTrace( "G1" ) )
            {
                string errorMessage;
                using( monitor.OpenInfo( "G2" ) )
                {
                    monitor.Error( "E1" );
                    monitor.Fatal( "F1" );
                    c.Root.HasWarnOrError.Should().BeTrue();
                    c.Root.HasError.Should().BeTrue();
                    c.Root.ErrorCount.Should().Be( 1 );
                    c.Root.FatalCount.Should().Be( 1 );
                    c.Root.WarnCount.Should().Be( 0 );

                    using( monitor.OpenInfo( "G3" ) )
                    {
                        c.Current.HasWarnOrError.Should().BeFalse();
                        c.Current.HasError.Should().BeFalse();
                        c.Current.ErrorCount.Should().Be( 0 );
                        c.Current.FatalCount.Should().Be( 0 );
                        c.Current.WarnCount.Should().Be( 0 );

                        monitor.Error( "An error..." );

                        c.Current.HasWarnOrError.Should().BeTrue();
                        c.Current.HasError.Should().BeTrue();
                        c.Current.ErrorCount.Should().Be( 1 );
                        c.Current.FatalCount.Should().Be( 0 );
                        c.Current.WarnCount.Should().Be( 0 );

                        errorMessage = String.Join( "|", p.LastErrorPath.Select( e => e.Text + '-' + e.GroupConclusion?.ToStringGroupConclusion() ) );
                        errorMessage.Should().Be( "G1-|G2-|G3-|An error...-", "Groups are not closed: no conclusion exist yet." );
                    }
                    errorMessage = String.Join( "|", p.LastErrorPath.Select( e => e.Text + '-' + e.GroupConclusion?.ToStringGroupConclusion() ) );
                    errorMessage.Should().Be( "G1-|G2-|G3-1 Error|An error...-", "G3 is closed: its conclusion is available." );
                }
                errorMessage = String.Join( "|", p.LastErrorPath.Select( e => e.Text + '-' + e.GroupConclusion?.ToStringGroupConclusion() ) );
                errorMessage.Should().Be( "G1-|G2-1 Fatal error, 2 Errors|G3-1 Error|An error...-" );
                monitor.Error( "E3" );
                monitor.Fatal( "F2" );
                monitor.Warn( "W2" );
                c.Root.HasWarnOrError.Should().BeTrue();
                c.Root.HasError.Should().BeTrue();
                c.Root.FatalCount.Should().Be( 2 );
                c.Root.ErrorCount.Should().Be( 3 );
                c.Root.MaxLogLevel.Should().Be( LogLevel.Fatal );
            }
            String.Join( ">", p.LastErrorPath.Select( e => e.Text + '-' + e.GroupConclusion?.ToStringGroupConclusion() ) ).Should().Be( "G1-2 Fatal errors, 3 Errors, 1 Warning>F2-" );
            String.Join( ">", p.LastWarnOrErrorPath.Select( e => e.Text + '-' + e.GroupConclusion?.ToStringGroupConclusion() ) ).Should().Be( "G1-2 Fatal errors, 3 Errors, 1 Warning>W2-" );
        }

        [Test]
        public void ActivityMonitorSimpleCollector_is_a_Client_that_filters_and_stores_its_Capacity_count_of_last_log_entries()
        {
            IActivityMonitor d = new ActivityMonitor( applyAutoConfigurations: false );
            var c = new ActivityMonitorSimpleCollector();
            d.Output.RegisterClient( c );
            d.Warn( "1" );
            d.Error( "2" );
            d.Fatal( "3" );
            d.Trace( "4" );
            d.Info( "5" );
            d.Warn( "6" );
            String.Join( ",", c.Entries.Select( e => e.Text ) ).Should().Be( "2,3" );

            c.MinimalFilter = LogLevelFilter.Fatal;
            String.Join( ",", c.Entries.Select( e => e.Text ) ).Should().Be( "3" );

            c.MinimalFilter = LogLevelFilter.Off;
            String.Join( ",", c.Entries.Select( e => e.Text ) ).Should().Be( "" );

            c.MinimalFilter = LogLevelFilter.Warn;
            using( d.OpenWarn( "1" ) )
            {
                d.Error( "2" );
                using( d.OpenFatal( "3" ) )
                {
                    d.Trace( "4" );
                    d.Info( "5" );
                }
            }
            d.Warn( "6" );
            String.Join( ",", c.Entries.Select( e => e.Text ) ).Should().Be( "1,2,3,6" );

            c.MinimalFilter = LogLevelFilter.Fatal;
            String.Join( ",", c.Entries.Select( e => e.Text ) ).Should().Be( "3" );

            c.MinimalFilter = LogLevelFilter.Debug;
            d.MinimalFilter = LogFilter.Debug;
            using( d.OpenDebug( "d1" ) )
            {
                d.Debug( "d2" );
                using( d.OpenFatal( "f1" ) )
                {
                    d.Debug( "d3" );
                    d.Info( "i1" );
                }
            }
            String.Join( ",", c.Entries.Select( e => e.Text ) ).Should().Be( "3,d1,d2,f1,d3,i1" );
        }

        [Test]
        public void ActivityMonitorTextWriterClient_has_its_own_LogFilter()
        {
            StringBuilder sb = new StringBuilder();

            IActivityMonitor d = new ActivityMonitor();
            d.MinimalFilter = LogFilter.Trace;

            var c = new ActivityMonitorTextWriterClient( s => sb.Append( s ), new LogClamper( LogFilter.Release, true ) );
            d.Output.RegisterClient( c );

            d.Trace( "NO SHOW" );
            d.Trace( "NO SHOW" );
            using( d.OpenTrace( "NO SHOW" ) )
            {
                d.Info( "NO SHOW" );
                d.Info( "NO SHOW" );
            }

            d.Error( "Error line at root" );
            using( d.OpenInfo( "NO SHOW" ) )
            {
                d.Warn( "NO SHOW" );
                d.Error( "Send error line inside group" );
                using( d.OpenError( "Open error group" ) )
                {
                    d.Error( "Send error line inside sub group" );
                }
            }

            sb.ToString().Should().NotContain( "NO SHOW" );
            sb.ToString().Should().Contain( "Error line at root" );
            sb.ToString().Should().Contain( "Send error line inside group" );
            sb.ToString().Should().Contain( "Open error group" );
            sb.ToString().Should().Contain( "Send error line inside sub group" );
        }

        [Test]
        public void OnError_fires_synchronously()
        {
            var m = new ActivityMonitor( false );
            bool hasError = false;
            using( m.OnError( () => hasError = true ) )
            using( m.OpenInfo( "Handling StObj objects." ) )
            {
                m.Fatal( "Oops!" );
                hasError.Should().BeTrue();
                hasError = false;
                m.OpenFatal( "Oops! (Group)" ).Dispose();
                hasError.Should().BeTrue();
                hasError = false;
            }
            hasError = false;
            m.Fatal( "Oops!" );
            hasError.Should().BeFalse();

            bool hasFatal = false;
            using( m.OnError( () => hasFatal = true, () => hasError = true ) )
            {
                m.Fatal( "Big Oops!" );
                hasFatal.Should().BeTrue();
                hasError.Should().BeFalse();
                m.Error( "Oops!" );
                hasFatal.Should().BeTrue();
                hasError.Should().BeTrue();
                hasFatal = hasError = false;
                m.OpenError( "Oops! (Group)" ).Dispose();
                hasFatal.Should().BeFalse();
                hasError.Should().BeTrue();
                m.OpenFatal( "Oops! (Group)" ).Dispose();
                hasFatal.Should().BeTrue(); hasError.Should().BeTrue();
                hasFatal = hasError = false;
            }
            m.Fatal( "Oops!" );
            hasFatal.Should().BeFalse();
            hasError.Should().BeFalse();
        }

        [Test]
        public void setting_the_MininimalFilter_of_a_bound_Client_is_thread_safe()
        {
            ActivityMonitor m = new ActivityMonitor( false );
            var tester = m.Output.RegisterClient( new ActivityMonitorClientTester() );

            m.ActualFilter.Should().Be( LogFilter.Undefined );
            tester.AsyncSetMinimalFilterAndWait( LogFilter.Monitor );
            m.ActualFilter.Should().Be( LogFilter.Monitor );
        }

        [Test]
        public void BoundClient_IsDead_flag_is_handled()
        {
            ActivityMonitor m = new ActivityMonitor( false );
            var tester = m.Output.RegisterClient( new ActivityMonitorClientTester() );

            m.Error( "Hello World!" );
            tester.ReceivedTexts.Should().Contain( s => s.Contains( "Hello World!" ) );
            // This is totally artificial since IsDead is normally hidden 
            // by IActivityMonitorBoundClients.
            tester.IsDead = true;
            tester.MinimalFilter = LogFilter.Debug;
            // This triggers the processing of the clients (since the level is lowered).
            m.Error( "NEVER RECEIVED" );

            m.Output.Clients.Should().BeEmpty();
            tester.ReceivedTexts.Should().NotContain( s => s.Contains( "NEVER RECEIVED" ) );

        }

        [Test]
        public void BoundClient_IsDead_flag_must_use_SignalChange_to_signal_its_change()
        {
            ActivityMonitor m = new ActivityMonitor( false );
            var tester = m.Output.RegisterClient( new ActivityMonitorClientTester() );

            m.Error( "Hello World!" );
            tester.ReceivedTexts.Should().Contain( s => s.Contains( "Hello World!" ) );
            tester.AsyncDieAndWait( 20 );
            m.Error( "NEVER RECEIVED" );

            m.Output.Clients.Should().BeEmpty();
            tester.ReceivedTexts.Should().NotContain( s => s.Contains( "NEVER RECEIVED" ) );

        }

        class CheckAlwaysFilteredClient : ActivityMonitorClient
        {
            protected override void OnOpenGroup( IActivityLogGroup group )
            {
                (group.Data.Level & LogLevel.IsFiltered).Should().NotBe( 0 );
            }

            protected override void OnUnfilteredLog( ref ActivityMonitorLogData data )
            {
                data.IsFilteredLog.Should().BeTrue();
            }
        }


        [Test]
        public void in_a_OpenError_or_OpenFatal_group_level_is_automatically_sets_to_Debug()
        {
            ActivityMonitor m = new ActivityMonitor( false );
            var tester = m.Output.RegisterClient( new ActivityMonitorClientTester() );
            m.MinimalFilter = LogFilter.Release;
            m.Debug( "Here I am in NOT in Debug." );
            using( m.OpenError( "An error." ) )
            {
                m.Debug( "Here I am in Debug." );
            }
            tester.ReceivedTexts
                .Should().Match( e => e.Any( t => t.Contains( "Here I am in Debug." ) ) )
                         .And.Match( e => !e.All( t => t.Contains( "Here I am NOT in Debug." ) ) );
        }

        [Test]
        [SetCulture("en-US")]
        public void an_empty_exception_message_is_handled_with_no_log_string()
        {
            var ex = new Exception( null );
            ex.Message.Should().Be( "Exception of type 'System.Exception' was thrown." );

            ActivityMonitor m = new ActivityMonitor( false );
            var tester = m.Output.RegisterClient( new ActivityMonitorClientTester() );
            m.Fatal( new Exception( "" ) );
            tester.ReceivedTexts
                .Should().Match( e => e.Any( t => t.Contains( "[no-log]" ) ) );

        }
    }
}
