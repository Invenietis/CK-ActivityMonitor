using System;
using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace CK.Core.Tests.Monitoring;

public class LogKeyTests
{
    [Test]
    public void parsing_ActivityMonitor_LogKey()
    {
        var k = new ActivityMonitor.LogKey( "id", DateTimeStamp.UtcNow );
        var s = k.ToString();

        var k2 = ActivityMonitor.LogKey.Parse( s );
        k2.ShouldBe( k );

        ActivityMonitor.LogKey.TryParse( s, out var k3 ).ShouldBeTrue();
        k3.ShouldBe( k );

        string sWithRemainder = s + "remainder";
        var head = sWithRemainder.AsSpan();
        ActivityMonitor.LogKey.TryMatch( ref head, out var k4 ).ShouldBeTrue();
        k4.ShouldBe( k );
        head.SequenceEqual( "remainder" ).ShouldBeTrue();

        ActivityMonitor.LogKey.TryParse( sWithRemainder, out var failed1 ).ShouldBeFalse();
        failed1.ShouldBeNull();

        Util.Invokable( () => ActivityMonitor.LogKey.Parse( "" ) ).ShouldThrow<FormatException>();
        Util.Invokable( () => ActivityMonitor.LogKey.Parse( "no." ) ).ShouldThrow<FormatException>();
        Util.Invokable( () => ActivityMonitor.LogKey.Parse( "no.1254646cdececececededededsdsdde" ) ).ShouldThrow<FormatException>();

        ActivityMonitor.LogKey.TryParse( s.Remove( s.Length - 1 ), out var failed2 ).ShouldBeFalse();
        failed2.ShouldBeNull();
    }

}
