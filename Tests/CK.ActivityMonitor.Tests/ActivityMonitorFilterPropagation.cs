using System.Threading.Tasks;
using NUnit.Framework;
using FluentAssertions;

namespace CK.Core.Tests.Monitoring;

public class ActivityMonitorFilterPropagation
{
    [Test]
    public void Client_minimal_filter_changes_is_thread_safe()
    {
        var monitor = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );
        var c = monitor.Output.RegisterClient( new ActivityMonitorClientTester() );
        Parallel.For( 0, 20, i => c.AsyncSetMinimalFilterAndWait( new LogFilter( LogLevelFilter.Info, (LogLevelFilter)(i % 5 + 1) ), 1 ) );
    }

    [Test]
    public void Client_filter_propagates_to_monitor()
    {
        var monitor = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );
        var client = new ActivityMonitorConsoleClient();
        monitor.Output.RegisterClient( client );

        monitor.MinimalFilter.Should().Be( LogFilter.Undefined );

        client.MinimalFilter = new LogClamper( LogFilter.Release, true );

        client.MinimalFilter.Should().Be( new LogClamper( LogFilter.Release, true ) );
        monitor.ActualFilter.Should().Be( LogFilter.Release );
    }

    [Test]
    public void ultimate_default_filter_is_the_static_ActivityMonitor_DefaultFilter_that_is_Trace_by_default()
    {
        var monitor = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );
        var client = new StupidStringClient();
        monitor.Output.RegisterClient( client );

        ActivityMonitor.DefaultFilter.Should().Be( LogFilter.Trace );
        monitor.MinimalFilter.Should().Be( LogFilter.Undefined );
        monitor.ActualFilter.Should().Be( LogFilter.Undefined );
        monitor.Trace( "n°1" );
        monitor.Debug( "NOSHOW 1" );

        ActivityMonitor.DefaultFilter = LogFilter.Debug;
        monitor.MinimalFilter.Should().Be( LogFilter.Undefined );
        monitor.ActualFilter.Should().Be( LogFilter.Undefined );
        monitor.Trace( "n°2" );
        monitor.Debug( "Debug works." );
        ActivityMonitor.DefaultFilter = LogFilter.Trace;

        client.ToString().Should().Match( "*n°1*n°2*Debug works.*" );
        client.ToString().Should().NotMatch( "*NOSHOW*" );
    }

}
