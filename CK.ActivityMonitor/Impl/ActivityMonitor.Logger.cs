namespace CK.Core
{
    public partial class ActivityMonitor
    {
        /// <summary>
        /// Associated <see cref="IActivityMonitor.Logger"/> available when a
        /// <see cref="DateTimeStampProvider"/> has been provided to a <see cref="IActivityMonitor"/>.
        /// </summary>
        public sealed class ThreadSafeLogger : IActivityLogger
        {
            readonly ActivityMonitor _monitor;
            readonly DateTimeStampProvider _stamp;

            internal ThreadSafeLogger( ActivityMonitor monitor, DateTimeStampProvider stamp )
            {
                _monitor = monitor;
                _stamp = stamp;
            }

            /// <inheritdoc />
            public string UniqueId => _monitor.UniqueId;

            /// <inheritdoc />
            public CKTrait AutoTags => _monitor.AutoTags;

            /// <inheritdoc />
            public LogLevelFilter ActualFilter => _monitor.ActualFilter.Line;

            /// <inheritdoc />
            public DateTimeStamp GetAndUpdateNextLogTime() => _stamp.GetNextNow();

            /// <inheritdoc />
            public void UnfilteredLog( ref ActivityMonitorLogData data )
            {
                if( !data.LogTime.IsKnown ) data.SetLogTime( _stamp.GetNextNow() );
                OnStaticLog?.Invoke( ref data );
            }
        }

    }
}
