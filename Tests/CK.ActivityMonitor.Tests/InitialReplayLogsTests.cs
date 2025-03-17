using NUnit.Framework;
using Shouldly;

namespace CK.Core.Tests.Monitoring;

[TestFixture]
public class InitialReplayLogsTests
{
    [Test]
    public void replay_handles_the_initial_topic_and_can_be_stopped()
    {
        var m = new ActivityMonitor( ActivityMonitorOptions.WithInitialReplay, "YES!" );
        var c0 = new StupidStringClient();
        m.Output.RegisterClient( c0, true );
        c0.ToString().ShouldContain( "Topic: YES!" );

        for( int i = 0; i < 10; ++i ) m.UnfilteredLog( LogLevel.Info, null, $"a{i}", null );

        var c1 = new StupidStringClient();
        m.Output.RegisterClient( c1, true );
        c1.ToString().ShouldContain( "Topic: YES!" );

        for( int i = 0; i < 10; ++i ) m.UnfilteredLog( LogLevel.Info, null, $"b{i}", null );

        var c2 = new StupidStringClient();
        m.Output.RegisterClient( c2, true );
        c2.ToString().ShouldContain( "Topic: YES!" );

        m.Output.MaxInitialReplayCount = 0;
        for( int i = 0; i < 10; ++i ) m.UnfilteredLog( LogLevel.Info, null, $"c{i}", null );

        var cNo = new StupidStringClient();
        m.Output.RegisterClient( cNo, true );

        c2.ToString().ShouldContain( "a0" ).ShouldContain( "a9" )
                          .ShouldContain( "b0" ).ShouldContain( "b9" )
                          .ShouldContain( "c0" ).ShouldContain( "c9" );
        c1.ToString().ShouldBe( c0.ToString() );
        c2.ToString().ShouldBe( c1.ToString() );

        cNo.ToString().ShouldBeEmpty();
    }


    [TestCase( 3712 )]
    [TestCase( 1000 )] // The default
    [TestCase( 5 )]
    public void replay_automatically_stops_after_the_first_MaxInitialReplayCount_logs( int maxCount )
    {
        var m = new ActivityMonitor( ActivityMonitorOptions.WithInitialReplay );
        m.Output.MaxInitialReplayCount.ShouldBe( 1000 );
        // We want the maxcount 
        m.Output.MaxInitialReplayCount = maxCount;

        var c0 = new StupidStringClient();
        m.Output.RegisterClient( c0, true );
        for( int i = 0; i < maxCount; ++i ) m.UnfilteredLog( LogLevel.Info, null, $"x{i}", null );

        var c1 = new StupidStringClient();
        m.Output.RegisterClient( c1, true );
        c1.ToString().ShouldContain( "x0" ).ShouldEndWith( $"x{maxCount - 1}" );

        m.UnfilteredLog( LogLevel.Info, null, $"LAST", null );
        c1.ToString().ShouldContain( "LAST" )
                     .ShouldContain( $"Replay logs reached its maximal count ({maxCount}). It is stopped." )
                     .ShouldBe( c0.ToString() );

        var cNo = new StupidStringClient();
        m.Output.RegisterClient( cNo, true );
        cNo.ToString().ShouldBeEmpty();
    }
}
