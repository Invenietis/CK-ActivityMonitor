using System;

namespace CK.Core;

public sealed partial class ActivityMonitor
{
    sealed class Logger : IParallelLogger
    {
        readonly ActivityMonitor _monitor;
        readonly object _lock;

        internal Logger( ActivityMonitor monitor )
        {
            _monitor = monitor;
            _lock = new object();
        }

        public string UniqueId => _monitor._uniqueId;

        public CKTrait AutoTags => _monitor._autoTags;

        // Don't use the ActualFiler property here because it updates the level
        // if it has been signaled (and calls ReentrantAndConcurrentCheck).
        public LogLevelFilter ActualFilter => _monitor._actualFilter.Line;

        ActivityMonitorLogData IActivityLineEmitter.CreateActivityMonitorLogData( LogLevel level,
                                                                                  CKTrait finalTags,
                                                                                  string? text,
                                                                                  object? exception,
                                                                                  string? fileName,
                                                                                  int lineNumber,
                                                                                  bool isOpenGroup )
        {
            return CreateLogLineData( true, level, finalTags, text, exception, fileName, lineNumber );
        }

        internal DateTimeStamp GetLogTimeForClosingGroup()
        {
            var now = DateTime.UtcNow;
            lock( _lock )
            {
                --_monitor._currentDepth;
                return _monitor._lastLogTime = new DateTimeStamp( _monitor._lastLogTime, now );
            }
        }

        internal ActivityMonitorLogData CreateLogLineData( bool isParallel,
                                                           LogLevel level,
                                                           CKTrait finalTags,
                                                           string? text,
                                                           object? exception,
                                                           string? fileName,
                                                           int lineNumber )
        {
            // Taking the depth here in the critical section guaranties that the data's depth
            // of parallel data is coherent with the regular activity.
            // That doesn't mean that the final ordering is guaranteed: it's up to the log sink
            // to reorder the log entries if it wants. The time window is rather narrow: unordered
            // entries can appear between CreateLogData -> Output -> Client -> Sink vs.
            // CreateLogData -> OnStaticLog -> Sink.
            DateTimeStamp logTime;
            int depth;
            var now = DateTime.UtcNow;
            lock( _lock )
            {
                _monitor._lastLogTime = logTime = new DateTimeStamp( _monitor._lastLogTime, now );
                depth = _monitor._currentDepth;
            }
            return new ActivityMonitorLogData( _monitor._uniqueId, logTime, depth, isParallel, false, level, finalTags, text, exception, fileName, lineNumber );
        }

        internal ActivityMonitorLogData CreateOpenGroupData( bool isParallel,
                                                             LogLevel level,
                                                             CKTrait finalTags,
                                                             string? text,
                                                             object? exception,
                                                             string? fileName,
                                                             int lineNumber )
        {
            // Same as above except that data.IsOpenGroup is true and the depth
            // is incremented in the critical section.
            DateTimeStamp logTime;
            int depth;
            var now = DateTime.UtcNow;
            lock( _lock )
            {
                _monitor._lastLogTime = logTime = new DateTimeStamp( _monitor._lastLogTime, now );
                // Gets the current depth and then increments it.
                // It is decremented in GetLogTimeForClosingGroup().
                depth = _monitor._currentDepth++;
            }
            return new ActivityMonitorLogData( _monitor._uniqueId, logTime, depth, isParallel, true, level, finalTags, text, exception, fileName, lineNumber );
        }

        public Token CreateToken( string? message,
                                  string? dependentTopic,
                                  CKTrait? createTags,
                                  string? fileName,
                                  int lineNumber )
        {
            if( string.IsNullOrWhiteSpace( message ) ) message = null;
            createTags |= _monitor._autoTags | Tags.CreateToken;
            var data = CreateLogLineData( true, LogLevel.Info | LogLevel.IsFiltered, createTags, message, null, fileName, lineNumber );
            Token t = _monitor.CreateToken( ref data, message, dependentTopic );
            OnStaticLog?.Invoke( ref data ); ;
            return t;
        }

        public void UnfilteredLog( ref ActivityMonitorLogData data )
        {
            OnStaticLog?.Invoke( ref data );
        }
    }
}
