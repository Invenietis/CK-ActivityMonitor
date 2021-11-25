using System;
using System.Collections.Generic;
using System.Threading;
using CK.Core.Impl;
using FluentAssertions;
#nullable enable

namespace CK.Core.Tests.Monitoring
{
    class ActivityMonitorClientTester : IActivityMonitorBoundClient
    {
        IActivityMonitorImpl? _source;
        LogFilter _minimalFilter;
        int _depth;
        string[] _text;

        public ActivityMonitorClientTester()
        {
            _text = Array.Empty<string>();
        }

        public LogFilter MinimalFilter
        {
            get { return _minimalFilter; }
            set
            {
                var prev = _minimalFilter;
                if( prev != value )
                {
                    _minimalFilter = value;
                    if( _source != null ) _source.SignalChange();
                }
            }
        }

        public bool IsDead { get; set; }

        public string[]? ReceivedTexts => _text;


        class Flag { public bool Set; }

        public void AsyncSetMinimalFilterAndWait( LogFilter filter, int delayMilliSeconds = 0 )
        {
            var state = Tuple.Create( TimeSpan.FromMilliseconds( delayMilliSeconds ), (LogFilter?)filter, new Flag() );
            ThreadPool.QueueUserWorkItem( DoAsyncDieOrSetMinimalFilterAndBlock, state );
            lock( state )
                while( !state.Item3.Set )
                    Monitor.Wait( state );
        }

        public void AsyncDieAndWait( int delayMilliSeconds = 0 )
        {
            var state = Tuple.Create( TimeSpan.FromMilliseconds( delayMilliSeconds ), (LogFilter?)null, new Flag() );
            ThreadPool.QueueUserWorkItem( DoAsyncDieOrSetMinimalFilterAndBlock, state );
            lock( state )
                while( !state.Item3.Set )
                    Monitor.Wait( state );
        }

        void DoAsyncDieOrSetMinimalFilterAndBlock( object? state )
        {
            var o = (Tuple<TimeSpan, LogFilter?, Flag>)state!;
            if( o.Item1 != TimeSpan.Zero ) Thread.Sleep( o.Item1 );
            if( o.Item2.HasValue ) MinimalFilter = o.Item2.Value;
            else
            {
                IsDead = true;
                if( _source == null ) throw new InvalidOperationException( nameof( IActivityMonitorBoundClient.SetMonitor ) + " was not called." );
                _source.SignalChange();
            }
            lock( o )
            {
                o.Item3.Set = true;
                Monitor.Pulse( o );
            }
        }

        void IActivityMonitorBoundClient.SetMonitor( IActivityMonitorImpl? source, bool forceBuggyRemove )
        {
            if( source != null && _source != null ) throw ActivityMonitorClient.CreateMultipleRegisterOnBoundClientException( this );
            if( source != null )
            {
                Interlocked.Exchange( ref _text, Array.Empty<string>() );
                _source = source;
            }
            else _source = null;
        }

        void IActivityMonitorClient.OnUnfilteredLog( ref ActivityMonitorLogData data )
        {
            data.FileName.Should().NotBeNullOrEmpty();
            Util.InterlockedAdd( ref _text, String.Format( "{0} {1} - {2} -[{3}]", new String( '>', _depth ), data.Level, data.Text, data.Tags ) );
        }

        void IActivityMonitorClient.OnOpenGroup( IActivityLogGroup group )
        {
            group.Data.FileName.Should().NotBeNullOrEmpty();
            int d = Interlocked.Increment( ref _depth );
            Util.InterlockedAdd( ref _text, String.Format( "{0} {1} - {2} -[{3}]", new String( '>', d ), group.Data.Level, group.Data.Text, group.Data.Tags ) );
        }

        void IActivityMonitorClient.OnGroupClosing( IActivityLogGroup group, ref List<ActivityLogGroupConclusion>? conclusions )
        {
        }

        void IActivityMonitorClient.OnGroupClosed( IActivityLogGroup group, IReadOnlyList<ActivityLogGroupConclusion>? conclusions )
        {
        }

        void IActivityMonitorClient.OnTopicChanged( string newTopic, string? fileName, int lineNumber )
        {
            fileName.Should().NotBeNullOrEmpty();
        }

        void IActivityMonitorClient.OnAutoTagsChanged( CKTrait newTrait )
        {
        }
    }
}
