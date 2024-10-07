using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using CK.Core.Impl;
using FluentAssertions;
using NUnit.Framework;

namespace CK.Core.Tests.Monitoring;

[TestFixture]
public class InteractiveUserClientTests
{
    class ConsoleLikeClient : ActivityMonitorTextWriterClient, IActivityMonitorInteractiveUserClient
    {
        readonly StringBuilder _b;

        public ConsoleLikeClient()
        {
            _b = new StringBuilder();
            Writer = s => _b.Append( s );
        }

        public override string ToString() => _b.ToString();
    }

    [Test]
    public void SetInteractiveUserFilter_tests()
    {
        var m = new ActivityMonitor();
        var c = m.Output.RegisterClient( new ConsoleLikeClient() );
        m.Trace( "A" );
        m.SetInteractiveUserFilter( new LogClamper( LogFilter.Terse, true ) );
        m.Trace( "NOSHOW" );
        m.SetInteractiveUserFilter( LogClamper.Undefined );
        m.Trace( "B" );
        c.ToString().Should().Contain( "A" ).And.Contain( "B" ).And.NotContain( "NOSHOW" );
    }

    [Test]
    public void TemporarilySetInteractiveUserFilter_tests()
    {
        var m = new ActivityMonitor();
        m.MinimalFilter = LogFilter.Debug;
        var c1 = m.Output.RegisterClient( new ConsoleLikeClient() );
        var c2 = m.Output.RegisterClient( new ConsoleLikeClient() { MinimalFilter = new LogClamper( LogFilter.Fatal, true ) } );
        m.Trace( "A in 1" );
        using( m.TemporarilySetInteractiveUserFilter( new LogClamper( LogFilter.Terse, true ) ) )
        {
            m.Trace( "NOSHOW" );
            using( m.TemporarilySetInteractiveUserFilter( new LogClamper( LogFilter.Trace, true ) ) )
            {
                m.Trace( "B in both" );
                m.Debug( "NOSHOW" );
            }
            m.Error( "C in both" );
            m.Trace( "NOSHOW" );
        }
        m.Error( "D in 1" );
        c1.ToString().Should().Contain( "A in 1" ).And.Contain( "B in both" ).And.Contain( "C in both" ).And.Contain( "D in 1" ).And.NotContain( "NOSHOW" );
        c2.ToString().Should().Contain( "B in both" ).And.Contain( "C in both" ).And.NotContain( "NOSHOW" );
    }

}
