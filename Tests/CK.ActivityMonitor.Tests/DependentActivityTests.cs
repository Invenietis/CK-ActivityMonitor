using Shouldly;
using System.Threading.Tasks;
using NUnit.Framework;

namespace CK.Core.Tests.Monitoring;

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

        var monitor = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );
        monitor.SetTopic( "This is the monitor's topic." );

        ActivityMonitor.Token token;
        using( monitor.CollectEntries( out var entries, LogLevelFilter.Info ) )
        {
            token = monitor.CreateToken( message, topic );
            ActivityMonitor.Token.TryParseMessageAndTopic( entries[0].Text, out var creatingMessage, out var creatingTopic ).ShouldBeTrue();
            creatingMessage.ShouldBe( token.Message );
            creatingTopic.ShouldBe( token.Topic );
        }

        (isNormalizedNullMessage == token.Message is null).ShouldBeTrue();
        (isNormalizedNullTopic == token.Topic is null).ShouldBeTrue();

        var tokenString = ActivityMonitor.Token.Parse( token.ToString() );

        tokenString.OriginatorId.ShouldBe( token.OriginatorId );
        tokenString.CreationDate.ShouldBe( token.CreationDate );
        tokenString.Message.ShouldBe( token.Message );
        tokenString.Topic.ShouldBe( token.Topic );
        tokenString.ToString().ShouldBe( token.ToString() );

        string startingString;
        bool changeTopic = token.Topic != null;
        using( monitor.CollectEntries( out var entries, LogLevelFilter.Info ) )
        {
            monitor.StartDependentActivity( token ).Dispose();

            Throw.DebugAssert( entries != null );
            entries.Count.ShouldBe( changeTopic ? 3 : 1 );
            if( changeTopic )
            {
                entries[0].Text.ShouldBe( "Topic: " + token.Topic );
                startingString = entries[1].Text;
                entries[2].Text.ShouldBe( "Topic: " + monitor.Topic );
            }
            else
            {
                startingString = entries[0].Text;
            }
        }

        ActivityMonitor.Token.TryParseStartMessage( startingString, out var startToken ).ShouldBeTrue();
        Throw.DebugAssert( startToken != null );
        startToken.OriginatorId.ShouldBe( token.OriginatorId );
        startToken.CreationDate.ShouldBe( token.CreationDate );
        startToken.Message.ShouldBe( token.Message );
        startToken.Topic.ShouldBe( token.Topic );
        startToken.ToString().ShouldBe( token.ToString() );
    }

    [TestCase( true )]
    [TestCase( false )]
    public void parsing_dependent_token_and_start_and_create_messages_with_time_collision( bool useStartDependentGroup )
    {
        TestHelper.LogsToConsole = true;

        ActivityMonitor m = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );
        StupidStringClient cCreate = m.Output.RegisterClient( new StupidStringClient() );

        // Generates a token with time collision.
        int loopNeeded = 0;
        ActivityMonitor.Token token;
        while( (token = m.CreateToken( "Test Message.", "Test Topic." )).CreationDate.Uniquifier == 0
                && loopNeeded < 100 )
        {
            ++loopNeeded;
        }
        token.Topic.ShouldBe( "Test Topic." );
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
            ActivityMonitor.Token.TryParseMessageAndTopic( createMessage, out var message, out var topic )
               .ShouldBeTrue();
            message.ShouldBe( "Test Message." );
            topic.ShouldBe( "Test Topic." );
        }

        string tokenToString = token.ToString();
        {
            ActivityMonitor.Token t2 = ActivityMonitor.Token.Parse( tokenToString );
            t2.OriginatorId.ShouldBe( m.UniqueId );
            t2.CreationDate.ShouldBe( cCreate.Entries[loopNeeded].Data.LogTime, "CreationDate is the time of the log entry." );
            t2.Topic.ShouldBe( "Test Topic." );
        }
        StupidStringClient.Entry[] logs = RunDependentActivity( token, useStartDependentGroup );
        {
            logs[0].Data.Text.ShouldBe( ActivityMonitor.SetTopicPrefix + "Test Topic." );
            ActivityMonitor.Token.TryParseStartMessage( logs[1].Data.Text, out var t ).ShouldBeTrue();
            Throw.DebugAssert( t != null );
            t.OriginatorId.ShouldBe( m.UniqueId );
            t.CreationDate.ShouldBe( cCreate.Entries[loopNeeded].Data.LogTime );
            t.Message.ShouldBe( "Test Message." );
            t.Topic.ShouldBe( "Test Topic." );
        }
    }

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
    static StupidStringClient.Entry[] RunDependentActivity( ActivityMonitor.Token token, bool useStartDependentGroup )
    {
        string? depMonitorTopic = null;
        StupidStringClient.Entry[]? dependentLogs = null;
        var task = Task.Run( () =>
        {
            StupidStringClient cStarted = new StupidStringClient();
            var depMonitor = new ActivityMonitor( "I'm the topic of the runner." );
            depMonitor.Output.RegisterClient( cStarted );
            depMonitor.Topic.ShouldBe( "I'm the topic of the runner." );
            using( useStartDependentGroup
                    ? depMonitor.StartDependentActivity( token )
                    : depMonitor.StartDependentActivityGroup( token ) )
            {
                depMonitorTopic = depMonitor.Topic;
                depMonitor.Trace( "Hello!" );
            }
            depMonitor.Topic.ShouldBe( "I'm the topic of the runner." );
            dependentLogs = cStarted.Entries.ToArray();
        } );
        task.Wait();
        depMonitorTopic.ShouldBe( token.Topic );
        return dependentLogs!;
    }
}
