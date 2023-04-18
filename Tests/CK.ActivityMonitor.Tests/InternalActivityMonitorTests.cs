
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
                if( data.Text.StartsWith( "OPEN AND NOT CLOSE GROUP InternalMonitor" ) )
                {
                    _source.InternalMonitor.OpenInfo( "Talk: " + data.Text );
                }
                else
                {
                    _source.InternalMonitor.Info( "Talk: OnUnfilteredLog" );
                }
                if( SleepTime != TimeSpan.Zero ) _source.InternalMonitor.Info( $"SleepTime: {SleepTime}." );
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
        public void InternalMonitor_works_by_replaying_its_logs_into_its_primary_monitor_and_tags_its_logs_with_InternalMonitor_tag( string order )
        {
            var m = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );
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
            }
            logs.Entries.Select( e => e.Data.Text )
                .SequenceEqual( new[]
                {
                    "Group",
                    "Talk: OnOpenGroup",
                    "Line",
                    "Talk: OnUnfilteredLog",
                } ).Should().BeTrue();
            logs.Entries[1].Data.Tags.Should().BeSameAs( ActivityMonitor.Tags.InternalMonitor );
            logs.Entries[3].Data.Tags.Should().BeSameAs( ActivityMonitor.Tags.InternalMonitor );
        }

        [Test]
        public void InternalMonitor_cannot_be_uesd_from_the_outside()
        {
            var m = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );
            var c = m.Output.RegisterClient( new TalkingClient() );
            c.CannotTalkWithoutLock();
        }

        [Test]
        public void Unclosed_Groups_in_InternalMonitor_are_automatically_closed()
        {
            var m = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );
            var logs = m.Output.RegisterClient( new StupidStringClient() );
            var c = m.Output.RegisterClient( new TalkingClient() );
            using( m.OpenInfo( "TalkingClient MUST leave an opened Group on the InternalMonitor." ) )
            {
                m.Info( "OPEN AND NOT CLOSE GROUP InternalMonitor - 1" );
                m.Info( "OPEN AND NOT CLOSE GROUP InternalMonitor - 2" );
            }
            logs.Entries.Select( e => e.Data.Text )
                .SequenceEqual( new[]
                {
                    "TalkingClient MUST leave an opened Group on the InternalMonitor.",
                    "Talk: OnOpenGroup (Unclosed)",
                    "OPEN AND NOT CLOSE GROUP InternalMonitor - 1",
                    "Talk: OPEN AND NOT CLOSE GROUP InternalMonitor - 1",
                    "OPEN AND NOT CLOSE GROUP InternalMonitor - 2",
                    "Talk: OPEN AND NOT CLOSE GROUP InternalMonitor - 2"
                } ).Should().BeTrue();
            m.CloseGroup().Should().BeFalse( "Back to root (both groups left opened have been closed)." );
        }

        [Test]
        public void InternalMonitor_logs_time_is_preserved()
        {
            TimeSpan beforeLogs = TimeSpan.FromMilliseconds( 300 );
            TimeSpan beforeLine2 = TimeSpan.FromMilliseconds( 100 );
            GetTextAndTimes( beforeLogs, beforeLine2, out string[] texts, out DateTime[] times );
            TimeSpan[] diffs = DiffTimes( times );
            diffs.All( d => d >= TimeSpan.Zero ).Should().BeTrue();

            // Group -> Talk: OnOpenGroup
            diffs[0].Should().BeGreaterOrEqualTo( beforeLine2 ).And.BeLessThan( beforeLogs );
            // Talk: OnOpenGroup -> SleepTime: 00:00:00.1000000.
            diffs[1].Should().BeCloseTo( TimeSpan.Zero, TimeSpan.FromMilliseconds( 5 ) );
            // SleepTime: 00:00:00.1000000. -> Line
            diffs[2].Should().BeGreaterThan( beforeLogs );
            // Line1 -> Talk: OnUnfilteredLog
            diffs[3].Should().BeGreaterOrEqualTo( beforeLine2 ).And.BeLessThan( beforeLogs );
            // Talk: OnUnfilteredLog -> SleepTime: 00:00:00.1000000.
            diffs[4].Should().BeCloseTo( TimeSpan.Zero, TimeSpan.FromMilliseconds( 5 ) );
        }

        static void GetTextAndTimes( TimeSpan beforeLogs, TimeSpan beforeTalk, out string[] texts, out DateTime[] times )
        {
            var m = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );
            var c = m.Output.RegisterClient( new TalkingClient() { SleepTime = beforeTalk } );
            var logs = m.Output.RegisterClient( new StupidStringClient() );
            using( m.OpenInfo( "Group" ) )
            {
                Thread.Sleep( beforeLogs );
                m.Info( "Line1" );
                Thread.Sleep( beforeLogs );
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
