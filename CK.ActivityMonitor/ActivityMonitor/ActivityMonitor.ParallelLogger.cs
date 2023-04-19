using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    public sealed partial class ActivityMonitor
    {
        sealed class Logger : IParallelLogger, ActivityMonitorLogData.IFactory
        {
            sealed class ForMonitor : ActivityMonitorLogData.IFactory
            {
                readonly Logger _logger;

                public ForMonitor( Logger logger )
                {
                    _logger = logger;
                }

                public ActivityMonitorLogData CreateLogData( LogLevel level, CKTrait finalTags, string? text, object? exception, string? fileName, int lineNumber )
                {
                    return _logger.CreateLogData( false, level, finalTags, text, exception, fileName, lineNumber );
                }

                public DateTimeStamp GetLogTime() => _logger.GetLogTime();
            }

            readonly ActivityMonitor _monitor;
            readonly ForMonitor _forMonitor;
            readonly object _lock;

            internal Logger( ActivityMonitor monitor )
            {
                _monitor = monitor;
                _lock = new object();
                _forMonitor = new ForMonitor( this );
            }

            public string UniqueId => _monitor._uniqueId;

            public CKTrait AutoTags => _monitor._autoTags;

            // Don't use the ActualFiler property here because it updates the level
            // if it has been signaled (and calls ReentrantAndConcurrentCheck).
            public LogLevelFilter ActualFilter => _monitor._actualFilter.Line;

            public ActivityMonitorLogData.IFactory DataFactory => this;

            public ActivityMonitorLogData.IFactory FactoryForMonitor => _forMonitor;

            ActivityMonitorLogData ActivityMonitorLogData.IFactory.CreateLogData( LogLevel level,
                                                                                  CKTrait finalTags,
                                                                                  string? text,
                                                                                  object? exception,
                                                                                  string? fileName,
                                                                                  int lineNumber )
            {
                return CreateLogData( true, level, finalTags, text, exception, fileName, lineNumber );
            }

            public DateTimeStamp GetLogTime()
            {
                lock( _lock )
                {
                    return _monitor._lastLogTime = new DateTimeStamp( _monitor._lastLogTime, DateTime.UtcNow );
                }
            }

            internal ActivityMonitorLogData CreateLogData( bool isParallel,
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
                lock( _lock )
                {
                    _monitor._lastLogTime = logTime = new DateTimeStamp( _monitor._lastLogTime, DateTime.UtcNow );
                    depth = _monitor._currentDepth;
                }
                return new ActivityMonitorLogData( _monitor._uniqueId, logTime, depth, isParallel, level, finalTags, text, exception, fileName, lineNumber );
            }

            public DependentToken CreateDependentToken( string? message,
                                                        string? dependentTopic,
                                                        string? fileName,
                                                        int lineNumber )
            {
                if( string.IsNullOrWhiteSpace( message ) ) message = null;
                var data = CreateLogData( true, LogLevel.Info | LogLevel.IsFiltered, _monitor.AutoTags | Tags.CreateDependentToken, message, null, fileName, lineNumber );
                DependentToken t = _monitor.CreateDependentToken( ref data, message, dependentTopic );
                OnStaticLog?.Invoke( ref data ); ;
                return t;
            }

            public void UnfilteredLog( ref ActivityMonitorLogData data )
            {
                OnStaticLog?.Invoke( ref data );
            }
        }
    }
}
