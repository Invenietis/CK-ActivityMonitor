namespace CK.Core
{
    public sealed partial class ActivityMonitor
    {
        sealed class Logger : IActivityLogger
        {
            readonly ActivityMonitor _monitor;
            readonly DateTimeStampProvider _stamp;

            internal Logger( ActivityMonitor monitor, DateTimeStampProvider stamp )
            {
                _monitor = monitor;
                _stamp = stamp;
            }

            public string UniqueId => _monitor.UniqueId;

            public CKTrait AutoTags => _monitor.AutoTags;

            public LogLevelFilter ActualFilter => _monitor.ActualFilter.Line;

            public DateTimeStamp GetAndUpdateNextLogTime() => _stamp.GetNextNow();

            public void UnfilteredLog( ref ActivityMonitorLogData data )
            {
                if( !data.LogTime.IsKnown ) data.SetLogTime( _stamp.GetNextNow() );
                OnStaticLog?.Invoke( ref data );
            }
        }
    }
}
