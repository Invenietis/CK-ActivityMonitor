using CK.Core.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace CK.Core
{
    public partial class ActivityMonitor
    {
        IActivityMonitor IActivityMonitorImpl.InternalMonitor
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

            public bool Replaying => _replaying;

            public class CloseGroupData
            {
                public readonly DateTimeStamp ClosingDate;
                public readonly IReadOnlyList<ActivityLogGroupConclusion>? Conclusions;

                public CloseGroupData( DateTimeStamp d, IReadOnlyList<ActivityLogGroupConclusion>? c )
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

            void IActivityMonitorClient.OnGroupClosing( IActivityLogGroup group, ref List<ActivityLogGroupConclusion>? conclusions )
            {
            }

            void IActivityMonitorClient.OnGroupClosed( IActivityLogGroup group, IReadOnlyList<ActivityLogGroupConclusion>? conclusions )
            {
                if( !_replaying ) History.Add( new CloseGroupData( group.CloseLogTime, conclusions ) );
            }

            void IActivityMonitorClient.OnTopicChanged( string newTopic, string? fileName, int lineNumber )
            {
            }

            void IActivityMonitorClient.OnAutoTagsChanged( CKTrait newTrait )
            {
            }
            LogFilter IActivityMonitorBoundClient.MinimalFilter => LogFilter.Undefined;

            bool IActivityMonitorBoundClient.IsDead => false;

            void IActivityMonitorBoundClient.SetMonitor( IActivityMonitorImpl? source, bool forceBuggyRemove )
            {
                if( source != _owner ) throw ActivityMonitorClient.CreateBoundClientIsLockedException( this );
            }
        }

        sealed class InternalMonitor : ActivityMonitor
        {
            public readonly LogsRecorder Recorder;

            public InternalMonitor( ActivityMonitor main )
                : base( main._lastLogTime, Guid.NewGuid(), Tags.Empty, false )
            {
                Recorder = Output.RegisterClient( new LogsRecorder( this ) );
            }
        }

        /// <summary>
        /// Called when a client fails. When this returns false, it means that we are
        /// currently replaying such errors: in such case the exception must be thrown, it will
        /// be caught by the <see cref="DoReplayInternalLogs"/> that will stop the replay and
        /// add the error to the recorder.
        /// When true is returned, it means that the client error has been recorded and will
        /// be replayed next time <see cref="ReentrantAndConcurrentCheck()"/> is called.
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <param name="culprit">The faulty client.</param>
        /// <param name="buggyClients">The culprit list of clients to remove.</param>
        /// <returns>True to continue, false to throw the error.</returns>
        bool InternalLogUnhandledClientError( Exception ex, IActivityMonitorClient culprit, ref List<IActivityMonitorClient>? buggyClients )
        {
            if( buggyClients == null ) buggyClients = new List<IActivityMonitorClient>();
            buggyClients.Add( culprit );

            if( _internalMonitor != null )
            {
                if( _internalMonitor.Recorder.Replaying )
                {
                    // Since we will throw, we must remove the culprit right now
                    // (and we can because the loop on the clients will break).
                    // We forget the exception that may be returned by ForceRemoveCondemnedClient here
                    // since the client is already on error.
                    _output.ForceRemoveCondemnedClient( culprit );
                    return false;
                }
            }
            else
            {
                _internalMonitor = new InternalMonitor( this );
            }
            var d = new ActivityMonitorLogData( LogLevel.Fatal, ex, null, $"Unhandled error in IActivityMonitorClient '{culprit.GetType().FullName}'.", DateTimeStamp.UtcNow );
            _internalMonitor.UnfilteredLog( d );
            return true;
        }

        void HandleBuggyClients( List<IActivityMonitorClient> buggyClients )
        {
            foreach( var l in buggyClients )
            {
                var ex = _output.ForceRemoveCondemnedClient( l );
                if( ex != null )
                {
                    var d = new ActivityMonitorLogData( LogLevel.Fatal, ex, null, $"IActivityMonitorBoundClient.SetMonitor '{l.GetType().FullName}' failure.", DateTimeStamp.UtcNow );
                    if( _internalMonitor == null ) _internalMonitor = new InternalMonitor( this );
                    _internalMonitor.UnfilteredLog( d );
                }
            }
            _clientFilter = HandleBoundClientsSignal();
            UpdateActualFilter();
        }

        void DoReplayInternalLogs()
        {
            Debug.Assert( _internalMonitor != null && _internalMonitor.Recorder.History.Count > 0 );
            Debug.Assert( Thread.CurrentThread.ManagedThreadId == _enteredThreadId );
            CKTrait savedTags = _currentTag;
            string changedTopic = _topic;
            bool savedTrackStackTrace = _trackStackTrace;
            _trackStackTrace = false;
            int balancedGroup = 0;
            try
            {
                // Secure any unclosed groups.
                while( _internalMonitor.CloseGroup() ) ;
                // Replay the history, trusting the existing data time.
                _internalMonitor.Recorder.OnStartReplay();
                _currentTag = _currentTag.Union( Tags.InternalMonitor );
                foreach( var o in _internalMonitor.Recorder.History )
                {
                    switch( o )
                    {
                        case ActivityMonitorGroupData group:
                            DoOpenGroup( group, true );
                            ++balancedGroup;
                            break;
                        case ActivityMonitorLogData line:
                            if( line.Tags.AtomicTraits.Contains( Tags.MonitorTopicChanged ) )
                            {
                                changedTopic = line.Text.Substring( SetTopicPrefix.Length );
                            }
                            DoUnfilteredLog( line, true );
                            break;
                        case LogsRecorder.CloseGroupData close:
                            DoCloseGroup( close.ClosingDate, close.Conclusions, true );
                            --balancedGroup;
                            break;
                    }
                }
                if( changedTopic != _topic )
                {
                    var d = new ActivityMonitorLogData( LogLevel.Info, null, Tags.Empty, ActivityMonitorResources.ReplayRestoreTopic, this.NextLogTime() );
                    DoUnfilteredLog( d );
                    SendTopicLogLine();
                }
                _internalMonitor.Recorder.OnStopReplay();
            }
            catch( Exception ex )
            {
                // We first stop the replay: the history is cleared and the Recorder records.
                _internalMonitor.Recorder.OnStopReplay();
                // We close groups opened. If errors occur here they will be recorded.
                while( balancedGroup > 0 ) DoCloseGroup( DateTimeStamp.UtcNow, ActivityMonitorResources.ErrorWhileReplayingInternalLogs );
                // This will be recorded and replayed by the next call to ReentrantAndConcurrentRelease().
                var d = new ActivityMonitorLogData( LogLevel.Fatal, ex, null, ActivityMonitorResources.ErrorWhileReplayingInternalLogs, DateTimeStamp.UtcNow );
                UnfilteredLog( d );
            }
            _currentTag = savedTags;
            _trackStackTrace = savedTrackStackTrace;
        }


    }
}
