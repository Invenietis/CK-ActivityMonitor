using System;
using System.Linq;
using NUnit.Framework;
using FluentAssertions;

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
            m.Log( LogLevel.Fatal, TestHelper.Tag1, "Text3" );
            m.Log( LogLevel.Fatal, TestHelper.Tag2, "Text4", new Exception( "EX3" ) );
            m.Log( LogLevel.Fatal, TestHelper.Tag3, new Exception( "EX4" ) );
            client.Entries.Where( e => e.Data.Level == (LogLevel.Fatal|LogLevel.IsFiltered) )
                            .Select( e => e.Data.Text + e.Data.Exception?.Message + e.Data.Tags )
                            .Concatenate()
                .Should().Be( "Text1, EX1EX1, Text2EX2, Text3Tag1, Text4EX3Tag2, EX4EX4Tag3" );


            (m.ActualFilter.Line == LogLevelFilter.None && ActivityMonitor.DefaultFilter.Line > LogLevelFilter.Debug )
                .Should().BeTrue( "The Default filter is set to Trace and should not be changed by this unit tests." );

            client.Entries.Clear();
            var hole = Environment.TickCount;
            m.Log( LogLevel.Fatal, $"Text1{hole}" );
            m.Log( LogLevel.Error, new Exception( "EX1" ) );
            m.Log( LogLevel.Warn, $"Text2{hole}", new Exception( "EX2" ) );
            m.Log( LogLevel.Info, TestHelper.Tag1, $"Text3{hole}" );
            m.Log( LogLevel.Trace, TestHelper.Tag2, $"Text4{hole}", new Exception( "EX3" ) );
            // Filtered out (Trace level).
            m.Log( LogLevel.Debug, TestHelper.Tag3, new Exception( "EX4" ) );
            client.Entries.Select( e => e.Data.Text + e.Data.Exception?.Message + e.Data.Tags )
                          .Concatenate()
                .Should().Be( $"Text1{hole}, EX1EX1, Text2{hole}EX2, Text3{hole}Tag1, Text4{hole}EX3Tag2" );
        }

        [Test]
        public void simple_sender_Info_tests_works_like_the_other_ones_since_they_are_T4_generated()
        {
            var m = new ActivityMonitor( false );
            var client = m.Output.RegisterClient( new StupidStringClient() );

            var hole = Environment.TickCount;
            m.Info( "Text1" );
            m.Info( new Exception( "EX1" ) );
            m.Info( "Text2", new Exception( "EX2" ) );
            m.Info( TestHelper.Tag1, "Text3" );
            m.Info( TestHelper.Tag2, "Text4", new Exception( "EX3" ) );
            m.Info( TestHelper.Tag3, new Exception( "EX4" ) );
            m.Info( $"F1{hole}" );
            m.Info( $"F2{hole}", new Exception( "X2" ) );
            m.Info( TestHelper.Tag4, $"F3{hole}" );
            m.Info( TestHelper.Tag5, $"F4{hole}", new Exception( "X3" ) );
            client.Entries.Where( e => e.Data.Level == (LogLevel.Info | LogLevel.IsFiltered) )
                            .Select( e => e.Data.Text + e.Data.Exception?.Message + e.Data.Tags )
                            .Concatenate()
                .Should().Be( $"Text1, EX1EX1, Text2EX2, Text3Tag1, Text4EX3Tag2, EX4EX4Tag3, F1{hole}, F2{hole}X2, F3{hole}Tag4, F4{hole}X3Tag5" );

        }

        [Test]
        public void simple_sender_OpenInfo_tests_works_like_the_other_ones_since_they_are_T4_generated()
        {
            var m = new ActivityMonitor( false );
            var client = m.Output.RegisterClient( new StupidStringClient() );

            var hole = Environment.TickCount;
            using( m.OpenInfo( "Text1" ).ConcludeWith( () => "/Text1" ) )
            using( m.OpenInfo( new Exception( "EX1" ) ) )
            using( m.OpenInfo( "Text2", new Exception( "EX2" ) ) )
            using( m.OpenInfo( TestHelper.Tag1, "Text3" ) )
            using( m.OpenInfo( TestHelper.Tag2, "Text4", new Exception( "EX3" ) ) )
            using( m.OpenInfo( TestHelper.Tag3, new Exception( "EX4" ) ) )
            using( m.OpenInfo( $"F1{hole}" ) )
            using( m.OpenInfo( $"F2{hole}", new Exception( "X2" ) ) )
            using( m.OpenInfo( TestHelper.Tag4, $"F3{hole}" ) )
            using( m.OpenInfo( TestHelper.Tag5, $"F4{hole}", new Exception( "X3" ) ) )
            {
            }
            client.Entries.Where( e => e.Data.Level == (LogLevel.Info | LogLevel.IsFiltered) )
                            .Select( e => e.Data.Text + e.Data.Exception?.Message + e.Data.Tags + e.Conclusions!.ToStringGroupConclusion() )
                            .Concatenate()
                .Should().Be( $"Text1/Text1, EX1EX1, Text2EX2, Text3Tag1, Text4EX3Tag2, EX4EX4Tag3, F1{hole}, F2{hole}X2, F3{hole}Tag4, F4{hole}X3Tag5" );

            m.MinimalFilter = LogFilter.Release;
            // The text is never called.
            IDisposableGroup g = m.OpenInfo( $"{(0 == 0 ? throw new Exception( "Never called" ) : "")}" );
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

            var hole = Environment.TickCount;
            using( m.OpenGroup( LogLevel.Fatal, "Text1" ) )
            using( m.OpenGroup( LogLevel.Fatal, new Exception( "EX1" ) ) )
            using( m.OpenGroup( LogLevel.Fatal, "Text2", new Exception( "EX2" ) ) )
            using( m.OpenGroup( LogLevel.Fatal, TestHelper.Tag1, "Text3" ).ConcludeWith( () => "/Text3" ) )
            using( m.OpenGroup( LogLevel.Fatal, TestHelper.Tag2, "Text4", new Exception( "EX3" ) ) )
            using( m.OpenGroup( LogLevel.Fatal, TestHelper.Tag3, new Exception( "EX4" ) ) )
            using( m.OpenGroup( LogLevel.Info, $"Text1{hole}" ).ConcludeWith( () => "/Text1" ) )
            using( m.OpenGroup( LogLevel.Info, new Exception( "EX1" ) ) )
            using( m.OpenGroup( LogLevel.Info, $"Text2{hole}", new Exception( "EX2" ) ) )
            using( m.OpenGroup( LogLevel.Info, TestHelper.Tag1, $"Text3{hole}" ) )
            using( m.OpenGroup( LogLevel.Info, TestHelper.Tag2, $"Text4{hole}", ex: new Exception( "EX3" ) ) )
            using( m.OpenGroup( LogLevel.Info, TestHelper.Tag3, new Exception( "EX4" ) ) )
            {
            }
            client.Entries.Where( e => e.Data.Level == (LogLevel.Fatal | LogLevel.IsFiltered) )
                            .Select( e => e.Data.Text + e.Data.Exception?.Message + e.Data.Tags + e.Conclusions!.ToStringGroupConclusion() )
                            .Concatenate()
                .Should().Be( $"Text1, EX1EX1, Text2EX2, Text3Tag1/Text3, Text4EX3Tag2, EX4EX4Tag3" );

            client.Entries.Where( e => e.Data.Level == (LogLevel.Info | LogLevel.IsFiltered) )
                            .Select( e => e.Data.Text + e.Data.Exception?.Message + e.Data.Tags + e.Conclusions!.ToStringGroupConclusion() )
                            .Concatenate()
                .Should().Be( $"Text1{hole}/Text1, EX1EX1, Text2{hole}EX2, Text3{hole}Tag1, Text4{hole}EX3Tag2, EX4EX4Tag3" );

            m.MinimalFilter = LogFilter.Release;
            // The text is never called.
            IDisposableGroup g = m.OpenGroup( LogLevel.Info, $"Bug {( 1 == 1 ? throw new Exception( "Never called" ) : 0)}" );
            g.IsRejectedGroup.Should().BeTrue();
            // The conclude function is never called.
            IDisposable d = g.ConcludeWith( () => throw new Exception( "Never called" ) );
            d.Dispose();
        }


    }
}
