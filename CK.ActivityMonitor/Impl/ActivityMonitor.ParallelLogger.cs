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

                public ActivityMonitorLogData CreateLogData( LogLevel level, CKTrait finalTags, string? text, Exception? exception, string? fileName, int lineNumber )
                {
                    return _logger.CreateLogData( false, level, finalTags, text, exception, fileName, lineNumber );
                }
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

            public string UniqueId => _monitor.UniqueId;

            public CKTrait AutoTags => _monitor.AutoTags;

            public LogLevelFilter ActualFilter => _monitor.ActualFilter.Line;

            public ActivityMonitorLogData.IFactory DataFactory => this;

            public ActivityMonitorLogData.IFactory FactoryForMonitor => _forMonitor;

            ActivityMonitorLogData ActivityMonitorLogData.IFactory.CreateLogData( LogLevel level,
                                                                                  CKTrait finalTags,
                                                                                  string? text,
                                                                                  Exception? exception,
                                                                                  string? fileName,
                                                                                  int lineNumber )
            {
                return CreateLogData( true, level, finalTags, text, exception, fileName, lineNumber );
            }

            internal ActivityMonitorLogData CreateLogData( bool isParallel,
                                                           LogLevel level,
                                                           CKTrait finalTags,
                                                           string? text,
                                                           Exception? exception,
                                                           string? fileName,
                                                           int lineNumber )
            {
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
