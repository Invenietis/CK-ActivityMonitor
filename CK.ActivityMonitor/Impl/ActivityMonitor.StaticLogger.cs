using CK.Core.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CK.Core
{
    public partial class ActivityMonitor
    {
        sealed class LoggerStatic : IActivityLogger
        {
            static readonly DateTimeStampProvider _stamp = new DateTimeStampProvider();

            public CKTrait AutoTags => Tags.Empty;

            public LogLevelFilter ActualFilter => DefaultFilter.Line;

            public void UnfilteredLog( ref ActivityMonitorLogData data )
            {
                if( !data.LogTime.IsKnown )
                {
                    // There should be very few contentions here (the operation is fast),
                    // so we keep it simple (lock is efficient when there is no contention).
                    lock( _stamp )
                    {
                        _stamp.Value = data.SetLogTime( new DateTimeStamp( _stamp.Value, DateTime.UtcNow ) );
                    }
                }
                OnStaticLog?.Invoke( ref data );
            }
        }

        static readonly LoggerStatic _staticLogger;

        /// <summary>
        /// The handler signature of static logs.
        /// <para>
        /// The "by ref" argument here is to avoid any copy of the data. The data should not be altered by calling one of
        /// the 2 mutating methods <see cref="ActivityMonitorLogData.SetExplicitLogTime(DateTimeStamp)"/> or <see cref="ActivityMonitorLogData.SetExplicitTags(CKTrait)"/>
        /// unless you absolutely know what you are doing.
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
        /// A static <see cref="IActivityLogger"/> that immediately relay log data to <see cref="OnStaticLog"/> event.
        /// <para>
        /// Nothing is done with these logs at this level: this is to be used by client code of this library, typically CK.Monitoring.
        /// </para>
        /// <para>
        /// This is to be used rarely: only if there's really no way to bind the calling context to a real <see cref="IActivityMonitor"/>.
        /// Handlers of the OnStaticLog event should use the <see cref="ExternalLogMonitorUniqueId"/> as the monitor identifier.
        /// </para>
        /// </summary>
        public static IActivityLogger StaticLogger => _staticLogger;

    }
}
