
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Linq;
using CK.Core.Impl;
using System.Threading;
using System.Diagnostics;

namespace CK.Core.Tests.Monitoring
{
    [TestFixture]
    public class InternalActivityMonitorTests
    {
        class TalkingClient : ActivityMonitorClient, IActivityMonitorBoundClient
        {
            IActivityMonitorImpl? _source;

            public bool IsDead => false;

            public TimeSpan SleepTime { get; set; }

            public void SetMonitor( IActivityMonitorImpl? source, bool forceBuggyRemove )
            {
                Debug.Assert( source != null );
                _source = source;
            }

            protected override void OnOpenGroup( IActivityLogGroup group )
            {
                Debug.Assert( _source != null );
                Thread.Sleep( SleepTime );
                if( group.Data.Text == "TalkingClient MUST leave an opened Group on the InternalMonitor." )
                    _source.InternalMonitor.OpenInfo( "Talk: OnOpenGroup (Unclosed)" );
                else _source.InternalMonitor.Info( "Talk: OnOpenGroup" );
                if( SleepTime != TimeSpan.Zero ) _source.InternalMonitor.Info( $"SleepTime: {SleepTime}." );
            }

            protected override void OnUnfilteredLog( ref ActivityMonitorLogData data )
            {
                Debug.Assert( _source != null );
                Thread.Sleep( SleepTime );
                _source.InternalMonitor.Info( "Talk: OnUnfilteredLog" );
                if( SleepTime != TimeSpan.Zero ) _source.InternalMonitor.Info( $"SleepTime: {SleepTime}." );
            }

            public void TalkToInternalMonitor( string msg )
            {
                Debug.Assert( _source != null );
                using( _source.ReentrancyAndConcurrencyLock() )
                {
                    _source.Invoking( sut => sut.ReentrancyAndConcurrencyLock() )
                           .Should().Throw<CKException>( "Reentrant lock can be obtained only once." );
                    Thread.Sleep( SleepTime );
                    _source.InternalMonitor.Info( "Talk: " + msg );
                    if( SleepTime != TimeSpan.Zero ) _source.InternalMonitor.Info( $"SleepTime: {SleepTime}." );
                }
            }

            public void LeaveAnUnclosedGroupInInternalMonitor( string msg )
            {
                Debug.Assert( _source != null );
                using( _source.ReentrancyAndConcurrencyLock() )
                {
                    Thread.Sleep( SleepTime );
                    _source.InternalMonitor.OpenInfo( "Talk: Unclosed Group." );
                    if( SleepTime != TimeSpan.Zero ) _source.InternalMonitor.Info( $"SleepTime: {SleepTime}." );
                }
            }

            public void CannotTalkWithoutLock()
            {
                Debug.Assert( _source != null );
                _source.Invoking( sut => sut.InternalMonitor.Info( "Fail." ) )
                       .Should().Throw<InvalidOperationException>();
            }
        }

        [TestCase( "TalkingClientFirst" )]
        [TestCase( "CollectorClientFirst" )]
        public void InternalMonitor_works_by_replaying_its_logs_into_its_primary_monitor( string order )
        {
            var m = new ActivityMonitor( false );
            TalkingClient c;
            StupidStringClient logs;
            if( order == "TalkingClientFirst" )
            {
                c = m.Output.RegisterClient( new TalkingClient() );
                logs = m.Output.RegisterClient( new StupidStringClient() );
            }
            else
            {
                logs = m.Output.RegisterClient( new StupidStringClient() );
                c = m.Output.RegisterClient( new TalkingClient() );
            }
            using( m.OpenInfo( "Group" ) )
            {
                m.Info( "Line" );
                c.TalkToInternalMonitor( "Hello from outside." );
            }
            c.CannotTalkWithoutLock();
            logs.Entries.Select( e => e.Data.Text )
                .SequenceEqual( new[]
                {
                    "Group", "Talk: OnOpenGroup",
                    "Line", "Talk: OnUnfilteredLog",
                    "Talk: Hello from outside."
                } ).Should().BeTrue();
        }

        [Test]
        public void InternalMonitor_tags_its_logs_with_InternalMonitor_tag()
        {
            var m = new ActivityMonitor( false );
            // Register the output first and then the talking client.
            var logs = m.Output.RegisterClient( new StupidStringClient() );
            var c = m.Output.RegisterClient( new TalkingClient() );
            using( m.OpenInfo( "Group" ) )
            {
                m.Info( "Line" );
                c.TalkToInternalMonitor( "Hello from outside." );
            }
            logs.Entries[1].Data.Tags.Should().BeSameAs( ActivityMonitor.Tags.InternalMonitor );
            logs.Entries[3].Data.Tags.Should().BeSameAs( ActivityMonitor.Tags.InternalMonitor );
            logs.Entries[4].Data.Tags.Should().BeSameAs( ActivityMonitor.Tags.InternalMonitor );
        }

        [Test]
        public void Unclosed_Groups_in_InternalMonitor_are_automatically_closed()
        {
            var m = new ActivityMonitor( false );
            var logs = m.Output.RegisterClient( new StupidStringClient() );
            var c = m.Output.RegisterClient( new TalkingClient() );
            using( m.OpenInfo( "TalkingClient MUST leave an opened Group on the InternalMonitor." ) )
            {
                c.LeaveAnUnclosedGroupInInternalMonitor( "Auto Closed!" );
            }
            logs.Entries.Select( e => e.Data.Text )
                .SequenceEqual( new[]
                {
                    "TalkingClient MUST leave an opened Group on the InternalMonitor.",
                    "Talk: OnOpenGroup (Unclosed)",
                    "Talk: Unclosed Group."
                } ).Should().BeTrue();
            m.CloseGroup().Should().BeFalse( "Back to root (both groups left opened have been closed)." );
        }

        [Test]
        public void InternalMonitor_logs_time_is_preserved()
        {
            TimeSpan beforeLogs = TimeSpan.FromMilliseconds( 300 );
            TimeSpan beforeTalk = TimeSpan.FromMilliseconds( 100 );
            GetTextAndTimes( beforeLogs, beforeTalk, out string[] texts, out DateTime[] times );
            TimeSpan[] diffs = DiffTimes( times );
            diffs.All( d => d >= TimeSpan.Zero ).Should().BeTrue();

            // Group -> Talk: OnOpenGroup
            diffs[0].Should().BeGreaterOrEqualTo( beforeTalk ).And.BeLessThan( beforeLogs );
            // Talk: OnOpenGroup -> SleepTime: 00:00:00.1000000.
            diffs[1].Should().BeCloseTo( TimeSpan.Zero, TimeSpan.FromMilliseconds( 5 ) );
            // SleepTime: 00:00:00.1000000. -> Line
            diffs[2].Should().BeGreaterThan( beforeLogs );
            // Line -> Talk: OnUnfilteredLog
            diffs[3].Should().BeGreaterOrEqualTo( beforeTalk ).And.BeLessThan( beforeLogs );
            // Talk: OnUnfilteredLog -> SleepTime: 00:00:00.1000000.
            diffs[4].Should().BeCloseTo( TimeSpan.Zero, TimeSpan.FromMilliseconds( 5 ) );
            // SleepTime: 00:00:00.1000000. -> Talk: Hello from outside.
            diffs[5].Should().BeGreaterThan( beforeLogs + beforeTalk );
            // Talk: Hello from outside. -> SleepTime: 00:00:00.1000000.
            diffs[6].Should().BeCloseTo( TimeSpan.Zero, TimeSpan.FromMilliseconds( 5 ) );
        }

        static void GetTextAndTimes( TimeSpan beforeLogs, TimeSpan beforeTalk, out string[] texts, out DateTime[] times )
        {
            var m = new ActivityMonitor( false );
            var c = m.Output.RegisterClient( new TalkingClient() { SleepTime = beforeTalk } );
            var logs = m.Output.RegisterClient( new StupidStringClient() );
            using( m.OpenInfo( "Group" ) )
            {
                Thread.Sleep( beforeLogs );
                m.Info( "Line" );
                Thread.Sleep( beforeLogs );
                c.TalkToInternalMonitor( "Hello from outside." );
            }
            texts = logs.Entries.Select( e => e.Data.Text ).ToArray();
            times = logs.Entries.Select( e => e.Data.LogTime.TimeUtc ).ToArray();
        }

        static TimeSpan[] DiffTimes( DateTime[] times )
        {
            TimeSpan[] diff = new TimeSpan[times.Length - 1];
            for( int i = 0; i < diff.Length; ++i )
            {
                diff[i] = times[i + 1] - times[i];
            }
            return diff;
        }
    }
}
