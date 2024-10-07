using System;
using System.Linq;
using NUnit.Framework;
using FluentAssertions;

namespace CK.Core.Tests.Monitoring;

public class LogKeyTests
{
    [Test]
    public void parsing_ActivityMonitor_LogKey()
    {
        var k = new ActivityMonitor.LogKey( "id", DateTimeStamp.UtcNow );
        var s = k.ToString();

        var k2 = ActivityMonitor.LogKey.Parse( s );
        k2.Should().Be( k );

        ActivityMonitor.LogKey.TryParse( s, out var k3 ).Should().BeTrue();
        k3.Should().Be( k );

        string sWithRemainder = s + "remainder";
        var head = sWithRemainder.AsSpan();
        ActivityMonitor.LogKey.TryMatch( ref head, out var k4 ).Should().BeTrue();
        k4.Should().Be( k );
        head.SequenceEqual( "remainder" ).Should().BeTrue();

        ActivityMonitor.LogKey.TryParse( sWithRemainder, out var failed1 ).Should().BeFalse();
        failed1.Should().BeNull();

        FluentActions.Invoking( () => ActivityMonitor.LogKey.Parse( "" ) ).Should().Throw<FormatException>();
        FluentActions.Invoking( () => ActivityMonitor.LogKey.Parse( "no." ) ).Should().Throw<FormatException>();
        FluentActions.Invoking( () => ActivityMonitor.LogKey.Parse( "no.1254646cdececececededededsdsdde" ) ).Should().Throw<FormatException>();

        ActivityMonitor.LogKey.TryParse( s.Remove( s.Length - 1 ), out var failed2 ).Should().BeFalse();
        failed2.Should().BeNull();
    }

}
