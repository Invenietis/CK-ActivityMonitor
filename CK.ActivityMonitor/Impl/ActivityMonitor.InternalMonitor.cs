using CK.Core.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace CK.Core
{
    public partial class ActivityMonitor
    {
        IActivityMonitor Impl.IActivityMonitorImpl.InternalMonitor
        {
            get
            {
                RentrantOnlyCheck();
                if( _internalMonitor == null ) _internalMonitor = new InternalMonitor( this );
                return _internalMonitor;
            }
        }

        sealed class LogsRecorder : IActivityMonitorBoundClient
        {
            readonly InternalMonitor _owner;
            public readonly List<object> History = new List<object>();
            bool _replaying;

            public LogsRecorder( InternalMonitor owner )
            {
                _owner = owner;
            }

            public class CloseGroupData
            {
                public readonly DateTimeStamp ClosingDate;
                public readonly IReadOnlyList<ActivityLogGroupConclusion> Conclusions;

                public CloseGroupData( DateTimeStamp d, IReadOnlyList<ActivityLogGroupConclusion> c )
                {
                    ClosingDate = d;
                    Conclusions = c;
                }
            }

            public void OnStartReplay() => _replaying = true;

            public void OnStopReplay()
            {
                _replaying = false;
                History.Clear();
            }

            void IActivityMonitorClient.OnUnfilteredLog( ActivityMonitorLogData data )
            {
                if( !_replaying ) History.Add( data );
            }

            void IActivityMonitorClient.OnOpenGroup( IActivityLogGroup group )
            {
                if( !_replaying ) History.Add( group.InnerData );
            }

            void IActivityMonitorClient.OnGroupClosing( IActivityLogGroup group, ref List<ActivityLogGroupConclusion> conclusions )
            {
            }

            void IActivityMonitorClient.OnGroupClosed( IActivityLogGroup group, IReadOnlyList<ActivityLogGroupConclusion> conclusions )
            {
                if( !_replaying ) History.Add( new CloseGroupData( group.CloseLogTime, conclusions ) );
            }

            void IActivityMonitorClient.OnTopicChanged( string newTopic, string fileName, int lineNumber )
            {
            }

            void IActivityMonitorClient.OnAutoTagsChanged( CKTag newTag )
            {
            }
            LogFilter IActivityMonitorBoundClient.MinimalFilter => LogFilter.Undefined;

            bool IActivityMonitorBoundClient.IsDead => false;

            void IActivityMonitorBoundClient.SetMonitor( IActivityMonitorImpl source, bool forceBuggyRemove )
            {
                if( source != _owner ) throw ActivityMonitorClient.CreateBoundClientIsLockedException( this );
            }
        }

        class InternalMonitor : ActivityMonitor
        {
            public readonly LogsRecorder Recorder;

            public InternalMonitor( ActivityMonitor main )
                : base( main._lastLogTime, Guid.NewGuid(), Tags.Empty, false )
            {
                Recorder = Output.RegisterClient( new LogsRecorder( this ) );
            }
        }

        void DoReplayInternalLogs()
        {
            Debug.Assert( _internalMonitor != null && _internalMonitor.Recorder.History.Count > 0 );
            Debug.Assert( Thread.CurrentThread.ManagedThreadId == _enteredThreadId );
            CKTag savedTags = _currentTag;
            string changedTopic = _topic;
            // Secure any unclosed groups.
            while( _internalMonitor.CloseGroup() ) ;
            // Replay the history, trusting the exisiting data time.
            _internalMonitor.Recorder.OnStartReplay();
            try
            {
                _currentTag = _currentTag.Union( Tags.InternalMonitor );
                foreach( var o in _internalMonitor.Recorder.History )
                {
                    switch( o )
                    {
                        case ActivityMonitorGroupData group:
                            DoOpenGroup( group, true );
                            break;
                        case ActivityMonitorLogData line:
                            if( line.Tags.AtomicTags.Contains( Tags.MonitorTopicChanged ) )
                            {
                                changedTopic = line.Text.Substring( SetTopicPrefix.Length );
                            }
                            DoUnfilteredLog( line, true );
                            break;
                        case LogsRecorder.CloseGroupData close:
                            DoCloseGroup( close.ClosingDate, close.Conclusions, true );
                            break;
                    }
                }
                if( changedTopic != _topic )
                {
                    var d = new ActivityMonitorLogData( LogLevel.Info, null, Tags.Empty, ActivityMonitorResources.ReplayRestoreTopic, this.NextLogTime(), null );
                    DoUnfilteredLog( d );
                    SendTopicLogLine();
                }
            }
            catch( Exception ex )
            {
                CriticalErrorCollector.Add( ex, ActivityMonitorResources.ErrorWhileReplayingInternalLogs );
            }
            _internalMonitor.Recorder.OnStopReplay();
            _currentTag = savedTags;
        }


    }
}
