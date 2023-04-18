using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core.Tests.Monitoring
{
    [TestFixture]
    public class StaticAndThreadSafeLoggerTests
    {
        [Test]
        public void Log_and_receive()
        {
            var received = new List<string>();
            ActivityMonitor.StaticLogHandler h = delegate ( ref ActivityMonitorLogData d )
            {
                d.LogTime.TimeUtc.Should().NotBe( Util.UtcMinValue ).And.BeOnOrBefore( DateTime.UtcNow );
                received.Add( d.Text );
            };
            ActivityMonitor.OnStaticLog += h;
            ActivityMonitor.StaticLogger.UnfilteredLog( LogLevel.Debug, null, "text", null );
            received.Should().ContainSingle( "text" );
            received.Clear();

            ActivityMonitor.OnStaticLog -= h;
            ActivityMonitor.StaticLogger.UnfilteredLog( LogLevel.Debug, null, "NOSHOW", null );
            received.Should().BeEmpty();

            ActivityMonitor.OnStaticLog += h;
            ActivityMonitor.OnStaticLog += h;
            ActivityMonitor.StaticLogger.UnfilteredLog( LogLevel.Debug, ActivityMonitor.Tags.UserConclusion, "twice", null );
            received.Should().BeEquivalentTo( new[] { "twice", "twice" } );
            received.Clear();

            ActivityMonitor.OnStaticLog -= h;
            ActivityMonitor.StaticLogger.UnfilteredLog( LogLevel.Debug, null, "once", null );
            received.Should().ContainSingle( "once" );
            received.Clear();

            ActivityMonitor.OnStaticLog -= h;
            ActivityMonitor.StaticLogger.UnfilteredLog( LogLevel.Debug, null, "NOSHOW", null );
            received.Should().BeEmpty();
        }

        static readonly CKTrait _myTag = ActivityMonitor.Tags.Register( nameof( level_and_tags_filtering ) );

        [Test]
        public void level_and_tags_filtering()
        {
            var received = new List<string>();
            ActivityMonitor.StaticLogHandler h = delegate ( ref ActivityMonitorLogData d )
            {
                d.LogTime.TimeUtc.Should().NotBe( Util.UtcMinValue ).And.BeOnOrBefore( DateTime.UtcNow );
                received.Add( d.Text );
            };
            ActivityMonitor.OnStaticLog += h;

            ActivityMonitor.DefaultFilter.Should().Be( LogFilter.Trace );

            ActivityMonitor.StaticLogger.ShouldLogLine( LogLevel.Debug, null, out _ ).Should().BeFalse();
            ActivityMonitor.StaticLogger.Debug( "NOSHOW" );
            received.Should().BeEmpty();

            ActivityMonitor.StaticLogger.Trace( "Hop" );
            received.Should().ContainSingle( "Hop" );
            received.Clear();

            ActivityMonitor.DefaultFilter = LogFilter.Debug;

            ActivityMonitor.StaticLogger.ShouldLogLine(LogLevel.Debug, null, out _).Should().BeTrue();

            ActivityMonitor.StaticLogger.Debug( "Debug!" );
            ActivityMonitor.StaticLogger.Trace( "Trace!" );
            ActivityMonitor.StaticLogger.Info( "Info!" );
            ActivityMonitor.StaticLogger.Warn( "Warn!" );
            ActivityMonitor.StaticLogger.Error( "Error!" );
            ActivityMonitor.StaticLogger.Fatal( "Fatal!" );
            received.Should().BeEquivalentTo( new[] { "Debug!", "Trace!", "Info!", "Warn!", "Error!", "Fatal!" } );
            received.Clear();

            ActivityMonitor.Tags.AddFilter( _myTag, new LogClamper( LogFilter.Release, true ) );

            ActivityMonitor.StaticLogger.ShouldLogLine( LogLevel.Debug, _myTag, out _ ).Should().BeFalse();
            ActivityMonitor.StaticLogger.ShouldLogLine( LogLevel.Warn, _myTag, out _ ).Should().BeFalse();

            ActivityMonitor.StaticLogger.Debug( _myTag, "Debug!" );
            ActivityMonitor.StaticLogger.Trace( _myTag, "Trace!" );
            ActivityMonitor.StaticLogger.Info( _myTag, "Info!" );
            ActivityMonitor.StaticLogger.Warn( _myTag, "Warn!" );
            ActivityMonitor.StaticLogger.Error( _myTag, "Error!" );
            ActivityMonitor.StaticLogger.Fatal( _myTag, "Fatal!" );
            received.Should().BeEquivalentTo( new[] { "Error!", "Fatal!" } );
            received.Clear();

            ActivityMonitor.Tags.RemoveFilter( _myTag );

            ActivityMonitor.StaticLogger.ShouldLogLine( LogLevel.Debug, _myTag, out _ ).Should().BeTrue();

            ActivityMonitor.StaticLogger.Debug( _myTag, "Debug!" );
            ActivityMonitor.StaticLogger.Trace( _myTag, "Trace!" );
            ActivityMonitor.StaticLogger.Info( _myTag, "Info!" );
            ActivityMonitor.StaticLogger.Warn( _myTag, "Warn!" );
            ActivityMonitor.StaticLogger.Error( _myTag, "Error!" );
            ActivityMonitor.StaticLogger.Fatal( _myTag, "Fatal!" );
            received.Should().BeEquivalentTo( new[] { "Debug!", "Trace!", "Info!", "Warn!", "Error!", "Fatal!" } );
            received.Clear();

            ActivityMonitor.DefaultFilter = LogFilter.Trace;
            ActivityMonitor.OnStaticLog -= h;
        }

        [Test]
        public void ThreadSafeLogger_on_monitor()
        {
            var received = new List<string>();
            ActivityMonitor.StaticLogHandler h = delegate ( ref ActivityMonitorLogData d )
            {
                d.LogTime.TimeUtc.Should().NotBe( Util.UtcMinValue ).And.BeOnOrBefore( DateTime.UtcNow );
                received.Add( d.Text );
            };
            ActivityMonitor.OnStaticLog += h;

            var noLogger = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration, null );
            noLogger.ParallelLogger.Should().BeNull( "No DateTimeStampProvider." );

            var m = new ActivityMonitor( ActivityMonitorOptions.WithParallel| ActivityMonitorOptions.SkipAutoConfiguration );
            m.ParallelLogger.Should().NotBeNull();
            using( m.CollectTexts( out var logs ) )
            {
                m.Debug( "NOSHOW - Regular log." );
                m.ParallelLogger!.Debug( "NOSHOW - Thread safe log." );

                m.Trace( "Regular log." );
                m.ParallelLogger!.Trace( "Thread safe log." );

                logs.Should().BeEquivalentTo( new[] { "Regular log." } );
                received.Should().BeEquivalentTo( new[] { "Thread safe log." } );
            }
            ActivityMonitor.OnStaticLog -= h;
        }
    }
}
