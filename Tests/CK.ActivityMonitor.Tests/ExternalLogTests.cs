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
    public class ExternalLogTests
    {
        [Test]
        public void Log_and_receive()
        {
            var received = new List<string>();
            ActivityMonitor.ExternalLog.Handler h = delegate ( ref ActivityMonitorLogData d ) { received.Add( d.Text ); };
            ActivityMonitor.ExternalLog.OnExternalLog += h;
            ActivityMonitor.ExternalLog.UnfilteredLog( LogLevel.Debug, "text", null );
            received.Should().ContainSingle( "text" );
            received.Clear();

            ActivityMonitor.ExternalLog.OnExternalLog -= h;
            ActivityMonitor.ExternalLog.UnfilteredLog( LogLevel.Debug, "NOSHOW", null );
            received.Should().BeEmpty();

            ActivityMonitor.ExternalLog.OnExternalLog += h;
            ActivityMonitor.ExternalLog.OnExternalLog += h;
            ActivityMonitor.ExternalLog.UnfilteredLog( LogLevel.Debug, ActivityMonitor.Tags.UserConclusion, "twice", null );
            received.Should().BeEquivalentTo( new[] { "twice", "twice" } );
            received.Clear();

            ActivityMonitor.ExternalLog.OnExternalLog -= h;
            ActivityMonitor.ExternalLog.UnfilteredLog( LogLevel.Debug, "once", null );
            received.Should().ContainSingle( "once" );
            received.Clear();

            ActivityMonitor.ExternalLog.OnExternalLog -= h;
            ActivityMonitor.ExternalLog.UnfilteredLog( LogLevel.Debug, "NOSHOW", null );
            received.Should().BeEmpty();
        }

        static readonly CKTrait _myTag = ActivityMonitor.Tags.Register( nameof( level_and_tags_filtering ) );

        [Test]
        public void level_and_tags_filtering()
        {
            var received = new List<string>();
            ActivityMonitor.ExternalLog.Handler h = delegate ( ref ActivityMonitorLogData d ) { received.Add( d.Text ); };
            ActivityMonitor.ExternalLog.OnExternalLog += h;

            ActivityMonitor.DefaultFilter.Should().Be( LogFilter.Trace );

            ActivityMonitor.ExternalLog.Debug( "NOSHOW" );
            received.Should().BeEmpty();

            ActivityMonitor.ExternalLog.Trace( "Hop" );
            received.Should().ContainSingle( "Hop" );
            received.Clear();

            ActivityMonitor.DefaultFilter = LogFilter.Debug;

            ActivityMonitor.ExternalLog.Debug( "Debug!" );
            ActivityMonitor.ExternalLog.Trace( "Trace!" );
            ActivityMonitor.ExternalLog.Info( "Info!" );
            ActivityMonitor.ExternalLog.Warn( "Warn!" );
            ActivityMonitor.ExternalLog.Error( "Error!" );
            ActivityMonitor.ExternalLog.Fatal( "Fatal!" );
            received.Should().BeEquivalentTo( new[] { "Debug!", "Trace!", "Info!", "Warn!", "Error!", "Fatal!" } );
            received.Clear();

            ActivityMonitor.Tags.AddFilter( _myTag, new LogClamper( LogFilter.Release, true ) );

            ActivityMonitor.ExternalLog.Debug( _myTag, "Debug!" );
            ActivityMonitor.ExternalLog.Trace( _myTag, "Trace!" );
            ActivityMonitor.ExternalLog.Info( _myTag, "Info!" );
            ActivityMonitor.ExternalLog.Warn( _myTag, "Warn!" );
            ActivityMonitor.ExternalLog.Error( _myTag, "Error!" );
            ActivityMonitor.ExternalLog.Fatal( _myTag, "Fatal!" );
            received.Should().BeEquivalentTo( new[] { "Error!", "Fatal!" } );
            received.Clear();

            ActivityMonitor.Tags.RemoveFilter( _myTag );

            ActivityMonitor.ExternalLog.Debug( _myTag, "Debug!" );
            ActivityMonitor.ExternalLog.Trace( _myTag, "Trace!" );
            ActivityMonitor.ExternalLog.Info( _myTag, "Info!" );
            ActivityMonitor.ExternalLog.Warn( _myTag, "Warn!" );
            ActivityMonitor.ExternalLog.Error( _myTag, "Error!" );
            ActivityMonitor.ExternalLog.Fatal( _myTag, "Fatal!" );
            received.Should().BeEquivalentTo( new[] { "Debug!", "Trace!", "Info!", "Warn!", "Error!", "Fatal!" } );
            received.Clear();

            ActivityMonitor.DefaultFilter = LogFilter.Trace;
            ActivityMonitor.ExternalLog.OnExternalLog -= h;
        }
    }
}
