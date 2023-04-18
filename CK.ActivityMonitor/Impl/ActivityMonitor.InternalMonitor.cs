using CK.Core.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;
using static CK.Core.ActivityMonitor;
using static System.Net.Mime.MediaTypeNames;

namespace CK.Core
{
    public sealed partial class ActivityMonitor
    {
        IActivityMonitor IActivityMonitorImpl.InternalMonitor
        {
            get
            {
                RentrantOnlyCheck();
                if( _recorder == null )
                {
                    _recorder = new LogsRecorder( this );
                }
                return _recorder.InternalMonitor;
            }
        }

        /// <summary>
        /// History here retains objects that are Tuples of boxed struct ActivityMonitorLogData:
        /// this allocates objects but it's easier to inject the struct back and we don't care here
        /// since we are in an edge case.
        /// </summary>
        sealed class LogsRecorder : IActivityMonitorBoundClient
        {
            readonly ActivityMonitor _primary;
            public readonly List<object> History;
            public readonly ActivityMonitor InternalMonitor;
            bool _replaying;

            public LogsRecorder( ActivityMonitor primary )
            {
                _primary = primary;
                History = new List<object>();
                InternalMonitor = new ActivityMonitor( _generatorId.GetNextString(), Tags.Empty, false, primary._stampProvider, primary._logger );
                InternalMonitor.Output.RegisterClient( this );
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
                if( !_replaying )
                {
                    // This ensures that an InternalMonitor's log won't clash with the primary
                    // monitor stamps.
                    var copy = _primary.DataFactory.CreateLogData( data.Level, data.Tags, data.Text, data.Exception, data.FileName, data.LineNumber );
                    History.Add( copy );
                }
            }

            void IActivityMonitorClient.OnOpenGroup( IActivityLogGroup group )
            {
                if( !_replaying )
                {
                    var data = group.Data;
                    var copy = _primary.DataFactory.CreateLogData( data.Level, data.Tags, data.Text, data.Exception, data.FileName, data.LineNumber );
                    History.Add( Tuple.Create( copy ) );
                }
            }

            void IActivityMonitorClient.OnGroupClosing( IActivityLogGroup group, ref List<ActivityLogGroupConclusion>? conclusions )
            {
            }

            void IActivityMonitorClient.OnGroupClosed( IActivityLogGroup group, IReadOnlyList<ActivityLogGroupConclusion> conclusions )
            {
                if( !_replaying )
                {
                    throw new NotImplementedException();
                    var closeTime = _primary._stampProvider != null
                                    ? _primary._stampProvider.GetNext( group.CloseLogTime.TimeUtc )
                                    : (_primary._lastLogTime = new DateTimeStamp( _primary._lastLogTime, group.CloseLogTime ));
                    History.Add( Tuple.Create( closeTime, conclusions ) );
                }
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
                // Do not check here that the source is _primary._internalMonitor because this is called from the
                // InternalMonitor's constructor: _internalMonitor is still null.
                // This is not an issue since all of this is totally internal.
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
            buggyClients ??= new List<IActivityMonitorClient>();
            buggyClients.Add( culprit );

            if( _recorder != null )
            {
                if( _recorder.Replaying )
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
                _recorder = new LogsRecorder( this );
            }
            _recorder.InternalMonitor.UnfilteredLog( LogLevel.Fatal, Tags.Empty, $"Unhandled error in IActivityMonitorClient '{culprit.GetType().FullName}'.", ex );
            return true;
        }

        void HandleBuggyClients( List<IActivityMonitorClient> buggyClients )
        {
            foreach( var l in buggyClients )
            {
                var ex = _output.ForceRemoveCondemnedClient( l );
                if( ex != null )
                {
                    _recorder ??= new LogsRecorder( this );
                    _recorder.InternalMonitor.UnfilteredLog( LogLevel.Fatal, Tags.Empty, text: $"IActivityMonitorBoundClient.SetMonitor '{l.GetType().FullName}' failure.", ex );
                }
            }
            _clientFilter = HandleBoundClientsSignal();
            UpdateActualFilter();
        }

        void DoReplayInternalLogs()
        {
            Debug.Assert( _recorder != null && _recorder.History.Count > 0 );
            Debug.Assert( Environment.CurrentManagedThreadId == _enteredThreadId );
            CKTrait savedTags = _autoTags;
            string changedTopic = _topic;
            bool savedTrackStackTrace = _trackStackTrace;
            _trackStackTrace = false;
            int balancedGroup = 0;
            try
            {
                // Secure any unclosed groups.
                while( _recorder.InternalMonitor.CloseGroup() ) ;
                // Replay the history, trusting the existing data time but
                // changing the monitor identifier from the internal one to the
                // actual monitor's identifier.
                _recorder.OnStartReplay();
                foreach( var o in _recorder.History )
                {
                    switch( o )
                    {
                        case Tuple<ActivityMonitorLogData> group:
                            var d = group.Item1;
                            d.MutateForReplay( _uniqueId, _currentDepth );
                            DoOpenGroup( ref d );
                            ++balancedGroup;
                            break;
                        case ActivityMonitorLogData line:
                            if( line.Tags.AtomicTraits.Contains( Tags.MonitorTopicChanged ) )
                            {
                                changedTopic = line.Text.Substring( SetTopicPrefix.Length );
                            }
                            line.MutateForReplay( _uniqueId, _currentDepth );
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
                    SendTopicLogLine();
                }
                _recorder.OnStopReplay();
            }
            catch( Exception ex )
            {
                // We first stop the replay: the history is cleared and the Recorder records.
                _recorder.OnStopReplay();
                // We close groups opened. If errors occur here they will be recorded.
                while( balancedGroup > 0 ) DoCloseGroup( ActivityMonitorResources.ErrorWhileReplayingInternalLogs );
                // This will be recorded and replayed by the next call to ReentrantAndConcurrentRelease().
                this.UnfilteredLog( LogLevel.Fatal, Tags.InternalMonitor, ActivityMonitorResources.ErrorWhileReplayingInternalLogs, ex );
            }
            _autoTags = savedTags;
            _trackStackTrace = savedTrackStackTrace;
        }


    }
}
