using System;
using System.Linq;
using NUnit.Framework;
using FluentAssertions;

namespace CK.Core.Tests.Monitoring
{
    public class LogKeyTests
    {
        [Test]
        public void RootLogPath_can_not_be_changed()
        {
            var k = new ActivityMonitor.LogKey( "id", DateTimeStamp.UtcNow );
            var s = k.ToString();

            var k2 = ActivityMonitor.LogKey.Parse( s );
            k2.Should().Be( k );

            ActivityMonitor.LogKey.TryParse( s, out var k3 ).Should().BeTrue();
            k3.Should().Be( k );

            var head = (s + "remainder").AsSpan();
            ActivityMonitor.LogKey.TryMatch( ref head, out var k4 ).Should().BeTrue();
            k4.Should().Be( k );
            head.SequenceEqual( "remaider" ).Should().BeTrue();

            FluentActions.Invoking( () => ActivityMonitor.LogKey.Parse( "" ) ).Should().Throw<FormatException>();
            FluentActions.Invoking( () => ActivityMonitor.LogKey.Parse( "no." ) ).Should().Throw<FormatException>();
            FluentActions.Invoking( () => ActivityMonitor.LogKey.Parse( "no.1254646cdececececededededsdsdde" ) ).Should().Throw<FormatException>();

            WIP
        }

    }
}
