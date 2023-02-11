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

            public void OnStartReplay() => _replaying = true;

            public void OnStopReplay()
            {
                _replaying = false;
                History.Clear();
            }

            void IActivityMonitorClient.OnUnfilteredLog( ref ActivityMonitorLogData data )
            {
                if( !_replaying ) History.Add( data );
            }

            void IActivityMonitorClient.OnOpenGroup( IActivityLogGroup group )
            {
                if( !_replaying ) History.Add( Tuple.Create( group.Data ) );
            }

            void IActivityMonitorClient.OnGroupClosing( IActivityLogGroup group, ref List<ActivityLogGroupConclusion>? conclusions )
            {
            }

            void IActivityMonitorClient.OnGroupClosed( IActivityLogGroup group, IReadOnlyList<ActivityLogGroupConclusion>? conclusions )
            {
                if( !_replaying ) History.Add( Tuple.Create( group.CloseLogTime, conclusions ) );
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
                if( source != _owner ) ActivityMonitorClient.ThrowMultipleRegisterOnBoundClientException( this );
            }
        }

        sealed class InternalMonitor : ActivityMonitor
        {
            public readonly LogsRecorder Recorder;

            public InternalMonitor( ActivityMonitor main )
                : base( main._lastLogTime, _generatorId.GetNextString(), Tags.Empty, false )
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
            var d = new ActivityMonitorLogData( LogLevel.Fatal, Tags.Empty, $"Unhandled error in IActivityMonitorClient '{culprit.GetType().FullName}'.", ex );
            _internalMonitor.UnfilteredLog( ref d );
            return true;
        }

        void HandleBuggyClients( List<IActivityMonitorClient> buggyClients )
        {
            foreach( var l in buggyClients )
            {
                var ex = _output.ForceRemoveCondemnedClient( l );
                if( ex != null )
                {
                    var d = new ActivityMonitorLogData( LogLevel.Fatal, Tags.Empty, text: $"IActivityMonitorBoundClient.SetMonitor '{l.GetType().FullName}' failure.", ex );
                    if( _internalMonitor == null ) _internalMonitor = new InternalMonitor( this );
                    _internalMonitor.UnfilteredLog( ref d );
                }
            }
            _clientFilter = HandleBoundClientsSignal();
            UpdateActualFilter();
        }

        void DoReplayInternalLogs()
        {
            Debug.Assert( _internalMonitor != null && _internalMonitor.Recorder.History.Count > 0 );
            Debug.Assert( Environment.CurrentManagedThreadId == _enteredThreadId );
            CKTrait savedTags = _autoTags;
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
                foreach( var o in _internalMonitor.Recorder.History )
                {
                    switch( o )
                    {
                        case Tuple<ActivityMonitorLogData> group:
                            var d = group.Item1;
                            d.SetExplicitTags( d.Tags | Tags.InternalMonitor );
                            DoOpenGroup( ref d );
                            ++balancedGroup;
                            break;
                        case ActivityMonitorLogData line:
                            if( line.Tags.AtomicTraits.Contains( Tags.MonitorTopicChanged ) )
                            {
                                changedTopic = line.Text.Substring( SetTopicPrefix.Length );
                            }
                            line.SetExplicitTags( line.Tags | Tags.InternalMonitor );
                            DoUnfilteredLog( ref line );
                            break;
                        case Tuple<DateTimeStamp, IReadOnlyList<ActivityLogGroupConclusion>?> close:
                            DoCloseGroup( close.Item2, close.Item1 );
                            --balancedGroup;
                            break;
                    }
                }
                if( changedTopic != _topic )
                {
                    var d = new ActivityMonitorLogData( LogLevel.Info, Tags.InternalMonitor, ActivityMonitorResources.ReplayRestoreTopic, null );
                    DoUnfilteredLog( ref d );
                    SendTopicLogLine();
                }
                _internalMonitor.Recorder.OnStopReplay();
            }
            catch( Exception ex )
            {
                // We first stop the replay: the history is cleared and the Recorder records.
                _internalMonitor.Recorder.OnStopReplay();
                // We close groups opened. If errors occur here they will be recorded.
                while( balancedGroup > 0 ) DoCloseGroup( ActivityMonitorResources.ErrorWhileReplayingInternalLogs );
                // This will be recorded and replayed by the next call to ReentrantAndConcurrentRelease().
                var d = new ActivityMonitorLogData( LogLevel.Fatal, Tags.InternalMonitor, ActivityMonitorResources.ErrorWhileReplayingInternalLogs, ex );
                UnfilteredLog( ref d );
            }
            _autoTags = savedTags;
            _trackStackTrace = savedTrackStackTrace;
        }


    }
}
