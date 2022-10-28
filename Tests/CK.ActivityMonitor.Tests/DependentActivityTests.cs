using FluentAssertions;
using System;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Diagnostics;
using System.Collections.Generic;

namespace CK.Core.Tests.Monitoring
{
    public class DependentActivityTests
    {

        [TestCase( null, "A topic!" )]
        [TestCase( "", "A 'topic' with quote." )]
        [TestCase( " ", "A 'topic' with quote." )]
        [TestCase( "A message.", "A 'topic' \r\n with \"quote\" and new lines." )]
        [TestCase( "A message.", null )]
        [TestCase( "A message.", " " )]
        [TestCase( "A message.", "" )]
        [TestCase( null, null )]
        public void parsing_DependentToken_with_topics( string? message, string? topic )
        {
            bool isNormalizedNullMessage = string.IsNullOrWhiteSpace( message );
            bool isNormalizedNullTopic = string.IsNullOrWhiteSpace( topic );

            var monitor = new ActivityMonitor( applyAutoConfigurations: false );
            monitor.SetTopic( "This is the monitor's topic." );

            ActivityMonitor.DependentToken token;
            using( monitor.CollectEntries( out var entries, LogLevelFilter.Info ) )
            {
                token = monitor.CreateDependentToken( message, topic );
                ActivityMonitor.DependentToken.TryParseCreateMessage( entries[0].Text, out var creatingMessage, out var creatingTopic ).Should().BeTrue();
                creatingMessage.Should().Be( token.Message );
                creatingTopic.Should().Be( token.Topic );
            }

            (isNormalizedNullMessage == token.Message is null).Should().BeTrue();
            (isNormalizedNullTopic == token.Topic is null).Should().BeTrue();

            var tokenString = ActivityMonitor.DependentToken.Parse( token.ToString() );

            tokenString.OriginatorId.Should().Be( token.OriginatorId );
            tokenString.CreationDate.Should().Be( token.CreationDate );
            tokenString.Message.Should().Be( token.Message );
            tokenString.Topic.Should().Be( token.Topic );
            tokenString.ToString().Should().Be( token.ToString() );

            string startingString;
            bool changeTopic = token.Topic != null;
            using( monitor.CollectEntries( out var entries, LogLevelFilter.Info ) )
            {
                monitor.StartDependentActivity( token ).Dispose();

                Debug.Assert( entries != null );
                entries.Should().HaveCount( changeTopic ? 3 : 1 );
                if( changeTopic )
                {
                    entries[0].Text.Should().Be( "Topic: " + token.Topic );
                    startingString = entries[1].Text;
                    entries[2].Text.Should().Be( "Topic: " + monitor.Topic );
                }
                else
                {
                    startingString = entries[0].Text;
                }
            }

            ActivityMonitor.DependentToken.TryParseStartMessage( startingString, out var startToken ).Should().BeTrue();
            Debug.Assert( startToken != null );
            startToken.OriginatorId.Should().Be( token.OriginatorId );
            startToken.CreationDate.Should().Be( token.CreationDate );
            startToken.Message.Should().Be( token.Message );
            startToken.Topic.Should().Be( token.Topic );
            startToken.ToString().Should().Be( token.ToString() );
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
                ActivityMonitor.DependentToken.TryParseCreateMessage( createMessage, out var message, out var topic )
                   .Should().BeTrue();
                message.Should().Be( "Test Message." );
                topic.Should().Be( "Test Topic." );
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
