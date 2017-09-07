#region LGPL License
/*----------------------------------------------------------------------------
* This file (CK.Core\ActivityMonitor\Client\ActivityMonitorSimpleCollector.cs) is part of CiviKey. 
*  
* CiviKey is free software: you can redistribute it and/or modify 
* it under the terms of the GNU Lesser General Public License as published 
* by the Free Software Foundation, either version 3 of the License, or 
* (at your option) any later version. 
*  
* CiviKey is distributed in the hope that it will be useful, 
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
* GNU Lesser General Public License for more details. 
* You should have received a copy of the GNU Lesser General Public License 
* along with CiviKey.  If not, see <http://www.gnu.org/licenses/>. 
*  
* Copyright © 2007-2015, 
*     Invenietis <http://www.invenietis.com>,
*     In’Tech INFO <http://www.intechinfo.fr>,
* All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Simple monitor client that memorizes actions and can <see cref="Replay"/> them in other monitors.
    /// </summary>
    public sealed class ActivityMonitorRecorder : IActivityMonitorClient
    {
        List<object> _history;

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitorRecorder"/> client.
        /// </summary>
        public ActivityMonitorRecorder()
        {
            _history = new List<object>();
        }

        /// <summary>
        /// Clears the history.
        /// </summary>
        public void Clear() => _history.Clear();

        /// <summary>
        /// Replays what has been recorded so far to another monitor.
        /// The original <see cref="IActivityMonitor.Topic"/> is automatically restored by default.
        /// Note that replayed logs are timed at the time of their replay, the original emission time is lost.
        /// </summary>
        /// <param name="monitor">The target monitor.</param>
        /// <param name="autoRestoreTopic">False to let potential last topic change impact the target monitor.</param>
        public void Replay( IActivityMonitor monitor, bool autoRestoreTopic = true )
        {
            if( monitor == null ) throw new ArgumentNullException( nameof( monitor ) );
            string topic = monitor.Topic;
            foreach( var o in _history )
            {
                switch( o )
                {
                    case ActivityMonitorGroupData group:
                        monitor.UnfilteredOpenGroup( group );
                        break;
                    case ActivityMonitorLogData line:
                        if( line.Tags.AtomicTraits.Contains( ActivityMonitor.Tags.MonitorTopicChanged ) )
                        {
                            string t = line.Text.Substring( ActivityMonitor.SetTopicPrefix.Length );
                            monitor.SetTopic( t, line.FileName, line.LineNumber );
                        }
                        else monitor.UnfilteredLog( line );
                        break;
                    case IReadOnlyList<ActivityLogGroupConclusion> conclusions:
                        monitor.CloseGroup( conclusions );
                        break;
                }
            }
            if( autoRestoreTopic && topic != monitor.Topic )
            {
                monitor.UnfilteredLog( ActivityMonitor.Tags.Empty, LogLevel.Info, "Restoring changed Topic.", monitor.NextLogTime(), null );
                monitor.SetTopic( topic );
            }
        }

        void IActivityMonitorClient.OnUnfilteredLog( ActivityMonitorLogData data )
        {
            _history.Add( data );
        }

        void IActivityMonitorClient.OnOpenGroup( IActivityLogGroup group )
        {
            _history.Add( group.InnerData );
        }

        void IActivityMonitorClient.OnGroupClosing( IActivityLogGroup group, ref List<ActivityLogGroupConclusion> conclusions )
        {
        }

        void IActivityMonitorClient.OnGroupClosed( IActivityLogGroup group, IReadOnlyList<ActivityLogGroupConclusion> conclusions )
        {
            _history.Add( conclusions );
        }

        void IActivityMonitorClient.OnTopicChanged( string newTopic, string fileName, int lineNumber )
        {
            // SetTopic is tracked thanks to its Topic: log line tagged with ActivityMonitor.Tags.MonitorTopicChanged.
        }

        void IActivityMonitorClient.OnAutoTagsChanged( CKTrait newTrait )
        {
            // We do not memorize AutoTags changes: their impacts appear on the log entry tag and
            // that is memorized.
        }
    }
}
