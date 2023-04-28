using CK.Core.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CK.Core
{
    public sealed partial class ActivityMonitor
    {
        sealed class LoggerStatic : IStaticLogger
        {
            static DateTimeStamp _lastLogTime = DateTimeStamp.MinValue;
            static readonly object _lock = new object();

            public CKTrait AutoTags => Tags.Empty;

            public LogLevelFilter ActualFilter => DefaultFilter.Line;

            public ActivityMonitorLogData CreateActivityMonitorLogData( LogLevel level,
                                                                        CKTrait finalTags,
                                                                        string? text,
                                                                        object? exception,
                                                                        string? fileName,
                                                                        int lineNumber,
                                                                        bool isOpenGroup )
            {
                DateTimeStamp logTime;
                var now = DateTime.UtcNow;
                lock( _lock )
                {
                    _lastLogTime = logTime = new DateTimeStamp( _lastLogTime, now );
                }
                return new ActivityMonitorLogData( StaticLogMonitorUniqueId, logTime, 0, isParallel: true, isOpenGroup: false, level, finalTags, text, exception, fileName, lineNumber );
            }

            public void UnfilteredLog( ref ActivityMonitorLogData data )
            {
                OnStaticLog?.Invoke( ref data );
            }
        }

        static readonly LoggerStatic _staticLogger;

        /// <summary>
        /// The handler signature of static logs.
        /// <para>
        /// The "by ref" argument here is to avoid any copy of the data.
        /// </para>
        /// </summary>
        /// <param name="data">The log data payload.</param>
        public delegate void StaticLogHandler( ref ActivityMonitorLogData data );

        /// <summary>
        /// Raised by <see cref="StaticLogger"/>.
        /// <para>
        /// Such events should be handled very quickly (typically by queuing a <see cref="ActivityMonitorExternalLogData"/> or
        /// a projection of the data in a concurrent queue or a channel).
        /// </para>
        /// <para>
        /// This is a static event: the callbacks registered here will be referenced until removed: care should be
        /// taken to unregister callbacks otherwise referenced objects will never be garbage collected. This is by design,
        /// please don't ask for weak references here.
        /// </para>
        /// </summary>
        static public event StaticLogHandler? OnStaticLog;

        /// <summary>
        /// Gets a static logger that immediately relay log data to <see cref="OnStaticLog"/> event.
        /// <para>
        /// Nothing is done with these logs at this level: this is to be used by client code of this library, typically CK.Monitoring.
        /// </para>
        /// <para>
        /// This is to be used rarely: only if there's really no way to bind the calling context to a <see cref="IActivityMonitor"/>
        /// or a <see cref="IParallelLogger"/>.
        /// </para>
        /// </summary>
        public static IStaticLogger StaticLogger => _staticLogger;

    }
}
