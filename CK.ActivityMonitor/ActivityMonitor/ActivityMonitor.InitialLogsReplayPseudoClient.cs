using CK.Core.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace CK.Core
{
    public sealed partial class ActivityMonitor
    {

        internal void DoStopInitialReplay()
        {
            var r = Interlocked.Exchange( ref _initialReplay, null );
            if( r != null ) r.Release();
        }

        sealed class InitialLogsReplayPseudoClient
        {
            readonly List<object> _logs = new List<object>();
            readonly ActivityMonitor _monitor;
            internal int _maxCount;
            internal int _count;

            public InitialLogsReplayPseudoClient( ActivityMonitor monitor )
            {
                _monitor = monitor;
                _maxCount = 1000;
            }

            public void OnUnfilteredLog( ref ActivityMonitorLogData data )
            {
                _logs.Add( data.AcquireExternalData() );
                CheckAutoStop();
            }

            public void OnOpenGroup( ref ActivityMonitorLogData data )
            {
                _logs.Add( Tuple.Create( data.AcquireExternalData() ) );
                CheckAutoStop();
            }

            void CheckAutoStop()
            {
                if( ++_count > _maxCount )
                {
                    _monitor.DoStopInitialReplay();
                    ((IActivityMonitorImpl)_monitor).InternalMonitor.UnfilteredLog( LogLevel.Warn | LogLevel.IsFiltered,
                                                                                    null,
                                                                                    $"Replay logs reached its maximal count ({_maxCount}). It is stopped.",
                                                                                    null );
                }
            }

            public void OnGroupClosed( DateTimeStamp closeTime, IReadOnlyList<ActivityLogGroupConclusion> conclusions )
            {
                _logs.Add( Tuple.Create( closeTime, conclusions ) );
            }

            public void Replay( IActivityMonitorClient client )
            {
                foreach( var log in _logs )
                {
                    switch( log )
                    {
                        case ActivityMonitorExternalLogData line:
                            var dLine = new ActivityMonitorLogData( line );
                            _monitor.ReplayUnfilteredLog( ref dLine, client );
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
