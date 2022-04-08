using FluentAssertions;
using System;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Diagnostics;

namespace CK.Core.Tests.Monitoring
{
    public class DependentActivityTests
    {

        [TestCase( "A topic!" )]
        [TestCase( "A 'topic' with quote." )]
        [TestCase( "A 'topic' \r\n with \"quote\" and new lines." )]
        [TestCase( "" )]
        [TestCase( " " )]
        public void parsing_DependentToken_with_topics( string? topic )
        {
            var monitor = new ActivityMonitor( applyAutoConfigurations: false );
            monitor.SetTopic( "This is the monitor's topic." );
            var t1 = monitor.CreateDependentToken( "Received command n°1.", topic );
            var t2 = monitor.CreateDependentToken( "Received command n°2.", monitor.Topic );
            var t3 = monitor.CreateDependentToken( "Received command n°3." );

            var r1 = ActivityMonitor.DependentToken.Parse( t1.ToString() );

            r1.OriginatorId.Should().Be( t1.OriginatorId );
            r1.CreationDate.Should().Be( t1.CreationDate );
            r1.Message.Should().Be( t1.Message );
            r1.Topic.Should().Be( t1.Topic );
            r1.ToString().Should().Be( t1.ToString() );
            r1.IsMonitorTopic.Should().BeFalse();

            var r2 = ActivityMonitor.DependentToken.Parse( t2.ToString() );
            r2.OriginatorId.Should().Be( t2.OriginatorId );
            r2.CreationDate.Should().Be( t2.CreationDate );
            r2.Message.Should().Be( t2.Message );
            r2.Topic.Should().Be( t2.Topic );
            r2.ToString().Should().Be( t2.ToString() );
            r2.IsMonitorTopic.Should().BeTrue();

            var r3 = ActivityMonitor.DependentToken.Parse( t3.ToString() );
            r3.OriginatorId.Should().Be( t3.OriginatorId );
            r3.CreationDate.Should().Be( t3.CreationDate );
            r3.Message.Should().Be( t3.Message );
            r3.Topic.Should().Be( null );
            r3.ToString().Should().Be( t3.ToString() );
            r3.IsMonitorTopic.Should().BeFalse();
        }

        [Test]
        public void parsing_dependent_token_and_start_and_create_messages_with_time_collision()
        {
            TestHelper.LogsToConsole = true;

            ActivityMonitor m = new ActivityMonitor( applyAutoConfigurations: false );
            StupidStringClient cCreate = m.Output.RegisterClient( new StupidStringClient() );

            // Generates a token with time collision.
            int loopNeeded = 0;
            ActivityMonitor.DependentToken token;
            while( (token = m.CreateDependentToken( "Test Message.", "Test Topic." )).CreationDate.Uniquifier == 0
                    && loopNeeded < 100 )
            {
                ++loopNeeded;
            }
            token.Topic.Should().Be( "Test Topic." );
            if( loopNeeded == 100 )
            {
                m.Info( $"Unable to generate time collision in {loopNeeded} loops." );
            }
            else
            {
                m.Trace( $"Generating time collision required {loopNeeded} loops." );
            }
            string createMessage = cCreate.Entries[loopNeeded].Data.Text;
            {
                ActivityMonitor.DependentToken.TryParseCreateMessage( createMessage, out var message, out var topic, out var isMonitorTopic )
                   .Should().BeTrue();
                message.Should().Be( "Test Message." );
                topic.Should().Be( "Test Topic." );
                isMonitorTopic.Should().BeFalse();
            }

            string tokenToString = token.ToString();
            {
                ActivityMonitor.DependentToken t2 = ActivityMonitor.DependentToken.Parse( tokenToString );
                t2.OriginatorId.Should().Be( m.UniqueId );
                t2.CreationDate.Should().Be( cCreate.Entries[loopNeeded].Data.LogTime, "CreationDate is the time of the log entry." );
                t2.Topic.Should().Be( "Test Topic." );
            }
            StupidStringClient.Entry[] logs = RunDependentActivity( token );
            {
                logs[0].Data.Text.Should().Be( ActivityMonitor.SetTopicPrefix + "Test Topic." );
                ActivityMonitor.DependentToken.TryParseStartMessage( logs[1].Data.Text, out var t ).Should().BeTrue();
                Debug.Assert( t != null );
                t.OriginatorId.Should().Be( m.UniqueId );
                t.CreationDate.Should().Be( cCreate.Entries[loopNeeded].Data.LogTime );
                t.Message.Should().Be( "Test Message." );
                t.Topic.Should().Be( "Test Topic." );
                t.IsMonitorTopic.Should().BeFalse();
            }
        }

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        static StupidStringClient.Entry[] RunDependentActivity( ActivityMonitor.DependentToken token )
        {
            string? depMonitorTopic = null;
            StupidStringClient.Entry[]? dependentLogs = null;
            var task = Task.Run( () =>
            {
                StupidStringClient cStarted = new StupidStringClient();
                var depMonitor = new ActivityMonitor( "I'm the topic of the runner." );
                depMonitor.Output.RegisterClient( cStarted );
                depMonitor.Topic.Should().Be( "I'm the topic of the runner." );
                using( depMonitor.StartDependentActivity( token ) )
                {
                    depMonitorTopic = depMonitor.Topic;
                    depMonitor.Trace( "Hello!" );
                }
                depMonitor.Topic.Should().Be( "I'm the topic of the runner." );
                dependentLogs = cStarted.Entries.ToArray();
            } );
            task.Wait();
            depMonitorTopic.Should().Be( token.Topic );
            return dependentLogs!;
        }
    }
}
