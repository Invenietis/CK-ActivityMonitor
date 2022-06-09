using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core.Tests.Monitoring
{
    [TestFixture]
    public class StaticLoggerTests
    {
        [Test]
        public void Log_and_receive()
        {
            var received = new List<string>();
            ActivityMonitor.StaticLogger.Handler h = delegate ( ref ActivityMonitorLogData d ) { received.Add( d.Text ); };
            ActivityMonitor.StaticLogger.OnStaticLog += h;
            ActivityMonitor.StaticLogger.UnfilteredLog( LogLevel.Debug, "text", null );
            received.Should().ContainSingle( "text" );
            received.Clear();

            ActivityMonitor.StaticLogger.OnStaticLog -= h;
            ActivityMonitor.StaticLogger.UnfilteredLog( LogLevel.Debug, "NOSHOW", null );
            received.Should().BeEmpty();

            ActivityMonitor.StaticLogger.OnStaticLog += h;
            ActivityMonitor.StaticLogger.OnStaticLog += h;
            ActivityMonitor.StaticLogger.UnfilteredLog( LogLevel.Debug, ActivityMonitor.Tags.UserConclusion, "twice", null );
            received.Should().BeEquivalentTo( new[] { "twice", "twice" } );
            received.Clear();

            ActivityMonitor.StaticLogger.OnStaticLog -= h;
            ActivityMonitor.StaticLogger.UnfilteredLog( LogLevel.Debug, "once", null );
            received.Should().ContainSingle( "once" );
            received.Clear();

            ActivityMonitor.StaticLogger.OnStaticLog -= h;
            ActivityMonitor.StaticLogger.UnfilteredLog( LogLevel.Debug, "NOSHOW", null );
            received.Should().BeEmpty();
        }

        static readonly CKTrait _myTag = ActivityMonitor.Tags.Register( nameof( level_and_tags_filtering ) );

        [Test]
        public void level_and_tags_filtering()
        {
            var received = new List<string>();
            ActivityMonitor.StaticLogger.Handler h = delegate ( ref ActivityMonitorLogData d ) { received.Add( d.Text ); };
            ActivityMonitor.StaticLogger.OnStaticLog += h;

            ActivityMonitor.DefaultFilter.Should().Be( LogFilter.Trace );

            ActivityMonitor.StaticLogger.Debug( "NOSHOW" );
            received.Should().BeEmpty();

            ActivityMonitor.StaticLogger.Trace( "Hop" );
            received.Should().ContainSingle( "Hop" );
            received.Clear();

            ActivityMonitor.DefaultFilter = LogFilter.Debug;

            ActivityMonitor.StaticLogger.Debug( "Debug!" );
            ActivityMonitor.StaticLogger.Trace( "Trace!" );
            ActivityMonitor.StaticLogger.Info( "Info!" );
            ActivityMonitor.StaticLogger.Warn( "Warn!" );
            ActivityMonitor.StaticLogger.Error( "Error!" );
            ActivityMonitor.StaticLogger.Fatal( "Fatal!" );
            received.Should().BeEquivalentTo( new[] { "Debug!", "Trace!", "Info!", "Warn!", "Error!", "Fatal!" } );
            received.Clear();

            ActivityMonitor.Tags.AddFilter( _myTag, new LogClamper( LogFilter.Release, true ) );

            ActivityMonitor.StaticLogger.Debug( _myTag, "Debug!" );
            ActivityMonitor.StaticLogger.Trace( _myTag, "Trace!" );
            ActivityMonitor.StaticLogger.Info( _myTag, "Info!" );
            ActivityMonitor.StaticLogger.Warn( _myTag, "Warn!" );
            ActivityMonitor.StaticLogger.Error( _myTag, "Error!" );
            ActivityMonitor.StaticLogger.Fatal( _myTag, "Fatal!" );
            received.Should().BeEquivalentTo( new[] { "Error!", "Fatal!" } );
            received.Clear();

            ActivityMonitor.Tags.RemoveFilter( _myTag );

            ActivityMonitor.StaticLogger.Debug( _myTag, "Debug!" );
            ActivityMonitor.StaticLogger.Trace( _myTag, "Trace!" );
            ActivityMonitor.StaticLogger.Info( _myTag, "Info!" );
            ActivityMonitor.StaticLogger.Warn( _myTag, "Warn!" );
            ActivityMonitor.StaticLogger.Error( _myTag, "Error!" );
            ActivityMonitor.StaticLogger.Fatal( _myTag, "Fatal!" );
            received.Should().BeEquivalentTo( new[] { "Debug!", "Trace!", "Info!", "Warn!", "Error!", "Fatal!" } );
            received.Clear();

            ActivityMonitor.DefaultFilter = LogFilter.Trace;
            ActivityMonitor.StaticLogger.OnStaticLog -= h;
        }
    }
}
