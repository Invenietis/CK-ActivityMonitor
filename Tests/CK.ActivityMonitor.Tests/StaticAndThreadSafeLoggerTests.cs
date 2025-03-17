using Shouldly;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace CK.Core.Tests.Monitoring;

[TestFixture]
public class StaticAndThreadSafeLoggerTests
{
    [Test]
    public void Log_and_receive()
    {
        var received = new List<string>();
        ActivityMonitor.StaticLogHandler h = delegate ( ref ActivityMonitorLogData d )
        {
            d.LogTime.TimeUtc.ShouldNotBe( Util.UtcMinValue ).ShouldBeLessThanOrEqualTo( DateTime.UtcNow );
            received.Add( d.Text );
        };
        ActivityMonitor.OnStaticLog += h;
        ActivityMonitor.StaticLogger.UnfilteredLog( LogLevel.Debug, null, "text", null );
        received.ShouldHaveSingleItem().ShouldBe( "text" );
        received.Clear();

        ActivityMonitor.OnStaticLog -= h;
        ActivityMonitor.StaticLogger.UnfilteredLog( LogLevel.Debug, null, "NOSHOW", null );
        received.ShouldBeEmpty();

        ActivityMonitor.OnStaticLog += h;
        ActivityMonitor.OnStaticLog += h;
        ActivityMonitor.StaticLogger.UnfilteredLog( LogLevel.Debug, ActivityMonitor.Tags.UserConclusion, "twice", null );
        received.ShouldBe( new[] { "twice", "twice" } );
        received.Clear();

        ActivityMonitor.OnStaticLog -= h;
        ActivityMonitor.StaticLogger.UnfilteredLog( LogLevel.Debug, null, "once", null );
        received.ShouldHaveSingleItem().ShouldBe( "once" );
        received.Clear();

        ActivityMonitor.OnStaticLog -= h;
        ActivityMonitor.StaticLogger.UnfilteredLog( LogLevel.Debug, null, "NOSHOW", null );
        received.ShouldBeEmpty();
    }

    static readonly CKTrait _myTag = ActivityMonitor.Tags.Register( nameof( level_and_tags_filtering ) );

    [Test]
    public void level_and_tags_filtering()
    {
        var received = new List<string>();
        ActivityMonitor.StaticLogHandler h = delegate ( ref ActivityMonitorLogData d )
        {
            d.LogTime.TimeUtc.ShouldNotBe( Util.UtcMinValue ).ShouldBeLessThanOrEqualTo( DateTime.UtcNow );
            received.Add( d.Text );
        };
        ActivityMonitor.OnStaticLog += h;

        ActivityMonitor.DefaultFilter.ShouldBe( LogFilter.Trace );

        ActivityMonitor.StaticLogger.ShouldLogLine( LogLevel.Debug, null, out _ ).ShouldBeFalse();
        ActivityMonitor.StaticLogger.Debug( "NOSHOW" );
        received.ShouldBeEmpty();

        ActivityMonitor.StaticLogger.Trace( "Hop" );
        received.ShouldHaveSingleItem().ShouldBe( "Hop" );
        received.Clear();

        ActivityMonitor.DefaultFilter = LogFilter.Debug;

        ActivityMonitor.StaticLogger.ShouldLogLine( LogLevel.Debug, null, out _ ).ShouldBeTrue();

        ActivityMonitor.StaticLogger.Debug( "Debug!" );
        ActivityMonitor.StaticLogger.Trace( "Trace!" );
        ActivityMonitor.StaticLogger.Info( "Info!" );
        ActivityMonitor.StaticLogger.Warn( "Warn!" );
        ActivityMonitor.StaticLogger.Error( "Error!" );
        ActivityMonitor.StaticLogger.Fatal( "Fatal!" );
        received.ShouldBe( new[] { "Debug!", "Trace!", "Info!", "Warn!", "Error!", "Fatal!" } );
        received.Clear();

        ActivityMonitor.Tags.AddFilter( _myTag, new LogClamper( LogFilter.Release, true ) );

        ActivityMonitor.StaticLogger.ShouldLogLine( LogLevel.Debug, _myTag, out _ ).ShouldBeFalse();
        ActivityMonitor.StaticLogger.ShouldLogLine( LogLevel.Warn, _myTag, out _ ).ShouldBeFalse();

        ActivityMonitor.StaticLogger.Debug( _myTag, "Debug!" );
        ActivityMonitor.StaticLogger.Trace( _myTag, "Trace!" );
        ActivityMonitor.StaticLogger.Info( _myTag, "Info!" );
        ActivityMonitor.StaticLogger.Warn( _myTag, "Warn!" );
        ActivityMonitor.StaticLogger.Error( _myTag, "Error!" );
        ActivityMonitor.StaticLogger.Fatal( _myTag, "Fatal!" );
        received.ShouldBe( new[] { "Error!", "Fatal!" } );
        received.Clear();

        ActivityMonitor.Tags.RemoveFilter( _myTag );

        ActivityMonitor.StaticLogger.ShouldLogLine( LogLevel.Debug, _myTag, out _ ).ShouldBeTrue();

        ActivityMonitor.StaticLogger.Debug( _myTag, "Debug!" );
        ActivityMonitor.StaticLogger.Trace( _myTag, "Trace!" );
        ActivityMonitor.StaticLogger.Info( _myTag, "Info!" );
        ActivityMonitor.StaticLogger.Warn( _myTag, "Warn!" );
        ActivityMonitor.StaticLogger.Error( _myTag, "Error!" );
        ActivityMonitor.StaticLogger.Fatal( _myTag, "Fatal!" );
        received.ShouldBe( new[] { "Debug!", "Trace!", "Info!", "Warn!", "Error!", "Fatal!" } );
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
            d.LogTime.TimeUtc.ShouldNotBe( Util.UtcMinValue ).ShouldBeLessThanOrEqualTo( DateTime.UtcNow );
            received.Add( d.Text );
        };
        ActivityMonitor.OnStaticLog += h;

        var m = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );
        m.ParallelLogger.ShouldNotBeNull();
        using( m.CollectTexts( out var logs ) )
        {
            m.Debug( "NOSHOW - Regular log." );
            m.ParallelLogger.Debug( "NOSHOW - Thread safe log." );

            m.Trace( "Regular log." );
            m.ParallelLogger.Trace( "Thread safe log." );

            logs.ShouldBe( new[] { "Regular log." } );
            received.ShouldBe( new[] { "Thread safe log." } );
        }
        ActivityMonitor.OnStaticLog -= h;
    }
}
