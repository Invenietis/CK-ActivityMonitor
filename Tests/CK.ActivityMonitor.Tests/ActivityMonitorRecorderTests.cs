
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
    public class ActivityMonitorRecorderTests
    {
        [Test]
        public void simple_replay_with_topic_reset()
        {
            var primary = new ActivityMonitor( false, "Testing ActivityMonitorRecorder" );
            var logs = primary.Output.RegisterClient( new StupidStringClient() );
            var over = new ActivityMonitor( false );
            var memory = over.Output.RegisterClient( new ActivityMonitorRecorder() );
            primary.Info( "Start" );
            using( over.OpenInfo( "Mem Open 1" ) )
            using( over.OpenDebug( "NOSHOW" ) )
            {
                over.Error( "Mem Error 1" );
                over.SetTopic( "Mem Topic." );
                over.Info( "Mem end." );
            }
            primary.Info( "Done" );
            memory.Replay( primary );
            memory.Clear();
            memory.Replay( primary );

            primary.Topic.Should().Be( "Testing ActivityMonitorRecorder" );
            logs.Entries.Select( e => e.Text )
                .SequenceEqual( new[] {
                    "Start",
                    "Done",
                    "Mem Open 1",
                    "Mem Error 1",
                    "Topic: Mem Topic.",
                    "Mem end.",
                    "Restoring changed Topic.",
                    "Topic: Testing ActivityMonitorRecorder" } )
                .Should().BeTrue();
        }
    }
}
