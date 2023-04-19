using NUnit.Framework;
using FluentAssertions;

namespace CK.Core.Tests.Monitoring
{
    [TestFixture]
    public class InitialReplayLogsTests
    {
        [Test]
        public void replay_handles_the_initial_topic_and_can_be_stopped()
        {
            var m = new ActivityMonitor( ActivityMonitorOptions.WithInitialReplay, "YES!" );
            var c0 = new StupidStringClient();
            m.Output.RegisterClient( c0, true );
            c0.ToString().Should().Contain( "Topic: YES!" );

            for( int i = 0; i < 10; ++i ) m.UnfilteredLog( LogLevel.Info, null, $"a{i}", null );

            var c1 = new StupidStringClient();
            m.Output.RegisterClient( c1, true );
            c1.ToString().Should().Contain( "Topic: YES!" );

            for( int i = 0; i < 10; ++i ) m.UnfilteredLog( LogLevel.Info, null, $"b{i}", null );

            var c2 = new StupidStringClient();
            m.Output.RegisterClient( c2, true);
            c2.ToString().Should().Contain( "Topic: YES!" );

            m.Output.MaxInitialReplayCount = 0;
            for( int i = 0; i < 10; ++i ) m.UnfilteredLog( LogLevel.Info, null, $"c{i}", null );

            var cNo = new StupidStringClient();
            m.Output.RegisterClient( cNo, true );

            c2.ToString().Should().Contain( "a0" ).And.Contain( "a9" )
                              .And.Contain( "b0" ).And.Contain( "b9" )
                              .And.Contain( "c0" ).And.Contain( "c9" );
            c1.ToString().Should().Be( c0.ToString() );
            c2.ToString().Should().Be( c1.ToString() );

            cNo.ToString().Should().BeEmpty();
        }


        [TestCase( 3712 )]
        [TestCase( 1000 )] // The default
        [TestCase( 5 )] 
        public void replay_automatically_stops_after_the_first_MaxInitialReplayCount_logs( int maxCount )
        {
            var m = new ActivityMonitor( ActivityMonitorOptions.WithInitialReplay );
            m.Output.MaxInitialReplayCount.Should().Be( 1000 );
            // We want the maxcount 
            m.Output.MaxInitialReplayCount = maxCount;

            var c0 = new StupidStringClient();
            m.Output.RegisterClient( c0, true );
            for( int i = 0; i < maxCount; ++i ) m.UnfilteredLog( LogLevel.Info, null, $"x{i}", null );

            var c1 = new StupidStringClient();
            m.Output.RegisterClient( c1, true );
            c1.ToString().Should().Contain( "x0" ).And.EndWith( $"x{maxCount-1}" );

            m.UnfilteredLog( LogLevel.Info, null, $"LAST", null );
            c1.ToString().Should().Contain( "LAST" )
                              .And.Contain( $"Replay logs reached its maximal count ({maxCount}). It is stopped." )
                              .And.Be( c0.ToString() );
            
            var cNo = new StupidStringClient();
            m.Output.RegisterClient( cNo, true );
            cNo.ToString().Should().BeEmpty();
        }
    }
}
