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
            ActivityMonitor.ExternalLog.Handler h = delegate(ref ActivityMonitorLogData d) { received.Add( d.Text ); };
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
    }
}
