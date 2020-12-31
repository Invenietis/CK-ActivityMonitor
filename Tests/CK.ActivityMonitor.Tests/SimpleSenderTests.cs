using System;
using System.Linq;
using NUnit.Framework;
using FluentAssertions;
using CK.Text;

namespace CK.Core.Tests.Monitoring
{
    public class SimpleSenderTests
    {
        [Test]
        public void simple_sender_Log_tests()
        {
            var m = new ActivityMonitor( false );
            var client = m.Output.RegisterClient( new StupidStringClient() );

            m.Log( LogLevel.Fatal, "Text1" );
            m.Log( LogLevel.Fatal, new Exception( "EX1" ) );
            m.Log( LogLevel.Fatal, "Text2", new Exception( "EX2" ) );
            m.Log( LogLevel.Fatal, "Text3", ActivityMonitor.Tags.Register( "Tag1" ) );
            m.Log( LogLevel.Fatal, "Text4", new Exception( "EX3" ), ActivityMonitor.Tags.Register( "Tag2" ) );
            m.Log( LogLevel.Fatal, new Exception( "EX4" ), ActivityMonitor.Tags.Register( "Tag3" ) );
            client.Entries.Where( e => e.Level == (LogLevel.Fatal|LogLevel.IsFiltered) )
                            .Select( e => e.Text + e.Exception?.Message + e.Tags )
                            .Concatenate()
                .Should().Be( "Text1, EX1EX1, Text2EX2, Text3Tag1, Text4EX3Tag2, EX4EX4Tag3" );

            m.Log( LogLevel.Info, () => "Text1" );
            m.Log( LogLevel.Info, new Exception( "EX1" ) );
            m.Log( LogLevel.Info, () => "Text2", new Exception( "EX2" ) );
            m.Log( LogLevel.Info, () => "Text3", ActivityMonitor.Tags.Register( "Tag1" ) );
            m.Log( LogLevel.Info, () => "Text4", new Exception( "EX3" ), ActivityMonitor.Tags.Register( "Tag2" ) );
            m.Log( LogLevel.Info, new Exception( "EX4" ), ActivityMonitor.Tags.Register( "Tag3" ) );
            client.Entries.Where( e => e.Level == (LogLevel.Fatal | LogLevel.IsFiltered) )
                            .Select( e => e.Text + e.Exception?.Message + e.Tags )
                            .Concatenate()
                .Should().Be( "Text1, EX1EX1, Text2EX2, Text3Tag1, Text4EX3Tag2, EX4EX4Tag3" );


            (m.ActualFilter.Line == LogLevelFilter.None && ActivityMonitor.DefaultFilter.Line > LogLevelFilter.Debug )
                .Should().BeTrue( "The Default filter is set to Trace and should not be changed by this unit tests." );

            // The text is never called.
            m.Log( LogLevel.Debug, () => throw new Exception("Never called") );
        }

        [Test]
        public void simple_sender_Info_tests_works_like_the_other_ones_since_they_are_T4_generated()
        {
            var m = new ActivityMonitor( false );
            var client = m.Output.RegisterClient( new StupidStringClient() );

            m.Info( "Text1" );
            m.Info( new Exception( "EX1" ) );
            m.Info( "Text2", new Exception( "EX2" ) );
            m.Info( "Text3", ActivityMonitor.Tags.Register( "Tag1" ) );
            m.Info( "Text4", new Exception( "EX3" ), ActivityMonitor.Tags.Register( "Tag2" ) );
            m.Info( new Exception( "EX4" ), ActivityMonitor.Tags.Register( "Tag3" ) );
            m.Info( () => "F1" );
            m.Info( () => "F2", new Exception( "X2" ) );
            m.Info( () => "F3", ActivityMonitor.Tags.Register( "T1" ) );
            m.Info( () => "F4", new Exception( "X3" ), ActivityMonitor.Tags.Register( "T2" ) );
            client.Entries.Where( e => e.Level == (LogLevel.Info | LogLevel.IsFiltered) )
                            .Select( e => e.Text + e.Exception?.Message + e.Tags )
                            .Concatenate()
                .Should().Be( "Text1, EX1EX1, Text2EX2, Text3Tag1, Text4EX3Tag2, EX4EX4Tag3, F1, F2X2, F3T1, F4X3T2" );

            m.MinimalFilter = LogFilter.Monitor;
            // The text is never called.
            m.Info( () => throw new Exception( "Never called" ) );
        }

        [Test]
        public void simple_sender_OpenInfo_tests_works_like_the_other_ones_since_they_are_T4_generated()
        {
            var m = new ActivityMonitor( false );
            var client = m.Output.RegisterClient( new StupidStringClient() );

            using( m.OpenInfo( "Text1" ).ConcludeWith( () => "/Text1" ) )
            using( m.OpenInfo( new Exception( "EX1" ) ) )
            using( m.OpenInfo( "Text2", new Exception( "EX2" ) ) )
            using( m.OpenInfo( "Text3", ActivityMonitor.Tags.Register( "Tag1" ) ) )
            using( m.OpenInfo( "Text4", new Exception( "EX3" ), ActivityMonitor.Tags.Register( "Tag2" ) ) )
            using( m.OpenInfo( new Exception( "EX4" ), ActivityMonitor.Tags.Register( "Tag3" ) ) )
            using( m.OpenInfo( () => "F1" ) )
            using( m.OpenInfo( () => "F2", new Exception( "X2" ) ) )
            using( m.OpenInfo( () => "F3", ActivityMonitor.Tags.Register( "T1" ) ) )
            using( m.OpenInfo( () => "F4", new Exception( "X3" ), ActivityMonitor.Tags.Register( "T2" ) ) )
            {
            }
            client.Entries.Where( e => e.Level == (LogLevel.Info | LogLevel.IsFiltered) )
                            .Select( e => e.Text + e.Exception?.Message + e.Tags + e.Conclusions.ToStringGroupConclusion() )
                            .Concatenate()
                .Should().Be( "Text1/Text1, EX1EX1, Text2EX2, Text3Tag1, Text4EX3Tag2, EX4EX4Tag3, F1, F2X2, F3T1, F4X3T2" );

            m.MinimalFilter = LogFilter.Release;
            // The text is never called.
            IDisposableGroup g = m.OpenInfo( () => throw new Exception( "Never called" ) );
            g.IsRejectedGroup.Should().BeTrue();
            // The conclude function is never called.
            IDisposable d = g.ConcludeWith( () => throw new Exception( "Never called" ) );
            d.Dispose();
        }

        [Test]
        public void simple_sender_OpenGroup_tests()
        {
            var m = new ActivityMonitor( false );
            var client = m.Output.RegisterClient( new StupidStringClient() );

            using( m.OpenGroup( LogLevel.Fatal, "Text1" ) )
            using( m.OpenGroup( LogLevel.Fatal, new Exception( "EX1" ) ) )
            using( m.OpenGroup( LogLevel.Fatal, "Text2", new Exception( "EX2" ) ) )
            using( m.OpenGroup( LogLevel.Fatal, "Text3", ActivityMonitor.Tags.Register( "Tag1" ) ).ConcludeWith( () => "/Text3" ) )
            using( m.OpenGroup( LogLevel.Fatal, "Text4", new Exception( "EX3" ), ActivityMonitor.Tags.Register( "Tag2" ) ) )
            using( m.OpenGroup( LogLevel.Fatal, new Exception( "EX4" ), ActivityMonitor.Tags.Register( "Tag3" ) ) )
            using( m.OpenGroup( LogLevel.Info, () => "Text1" ).ConcludeWith( () => "/Text1" ) )
            using( m.OpenGroup( LogLevel.Info, new Exception( "EX1" ) ) )
            using( m.OpenGroup( LogLevel.Info, () => "Text2", new Exception( "EX2" ) ) )
            using( m.OpenGroup( LogLevel.Info, () => "Text3", ActivityMonitor.Tags.Register( "Tag1" ) ) )
            using( m.OpenGroup( LogLevel.Info, () => "Text4", new Exception( "EX3" ), ActivityMonitor.Tags.Register( "Tag2" ) ) )
            using( m.OpenGroup( LogLevel.Info, new Exception( "EX4" ), ActivityMonitor.Tags.Register( "Tag3" ) ) )
            {
            }
            client.Entries.Where( e => e.Level == (LogLevel.Fatal | LogLevel.IsFiltered) )
                            .Select( e => e.Text + e.Exception?.Message + e.Tags + e.Conclusions.ToStringGroupConclusion() )
                            .Concatenate()
                .Should().Be( "Text1, EX1EX1, Text2EX2, Text3Tag1/Text3, Text4EX3Tag2, EX4EX4Tag3" );

            client.Entries.Where( e => e.Level == (LogLevel.Info | LogLevel.IsFiltered) )
                            .Select( e => e.Text + e.Exception?.Message + e.Tags + e.Conclusions.ToStringGroupConclusion() )
                            .Concatenate()
                .Should().Be( "Text1/Text1, EX1EX1, Text2EX2, Text3Tag1, Text4EX3Tag2, EX4EX4Tag3" );

            m.MinimalFilter = LogFilter.Release;
            // The text is never called.
            IDisposableGroup g = m.OpenGroup( LogLevel.Info, () => throw new Exception( "Never called" ) );
            g.IsRejectedGroup.Should().BeTrue();
            // The conclude function is never called.
            IDisposable d = g.ConcludeWith( () => throw new Exception( "Never called" ) );
            d.Dispose();
        }


    }
}
