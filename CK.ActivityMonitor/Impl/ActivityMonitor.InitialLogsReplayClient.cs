using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CK.Core
{
    public sealed partial class ActivityMonitor
    {
        sealed class InitialLogsReplayClient : IActivityMonitorClient
        {
            readonly List<object> _logs = new List<object>();
            readonly ActivityMonitor _monitor;

            public InitialLogsReplayClient( ActivityMonitor monitor )
            {
                _monitor = monitor;
            }

            public void OnAutoTagsChanged( CKTrait newTrait ) { }
            public void OnGroupClosing( IActivityLogGroup group, ref List<ActivityLogGroupConclusion>? conclusions ) { }
            public void OnTopicChanged( string newTopic, string? fileName, int lineNumber ) { }

            public void OnUnfilteredLog( ref ActivityMonitorLogData data )
            {
                _logs.Add( data.AcquireExternalData() );
            }

            public void OnOpenGroup( IActivityLogGroup group )
            {
                _logs.Add( Tuple.Create( group.Data.AcquireExternalData() ) );
            }

            public void OnGroupClosed( IActivityLogGroup group, IReadOnlyList<ActivityLogGroupConclusion> conclusions )
            {
                _logs.Add( Tuple.Create( group.CloseLogTime, conclusions ) );
            }

            public void Replay( IActivityMonitorClient client )
            {
                foreach( var log in _logs )
                {
                    switch( log )
                    {
                        case ActivityMonitorExternalLogData line:
                            var dLine = new ActivityMonitorLogData( line );
                            _monitor.ReplayUnfilteredLog( ref dLine );
                            break;
                        case Tuple<ActivityMonitorExternalLogData> group:
                            var dGroup = new ActivityMonitorLogData( group.Item1 );
                            _monitor.ReplayOpenGroup( ref dGroup, client );
                            break;
                        case Tuple<DateTimeStamp, IReadOnlyList<ActivityLogGroupConclusion>> close:
                            _monitor.ReplayClosedGroup( close.Item1, close.Item2, client );
                            break;
                    }
                }
            }

            public void Release()
            {
                foreach( var log in _logs )
                {
                    switch( log )
                    {
                        case ActivityMonitorExternalLogData line:
                            line.Release();
                            break;
                        case Tuple<ActivityMonitorExternalLogData> group:
                            group.Item1.Release();
                            break;
                    }
                }
            }
        }
    }
}
