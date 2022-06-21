using FluentAssertions;
using System.Collections.Generic;
using NUnit.Framework;

namespace CK.Core.Tests.Monitoring
{
    public class ActivityMonitorTimestamp
    {
        class DateTimeStampCollision : IActivityMonitorClient
        {
            DateTimeStamp _lastOne;
            
            public int NbClash;

            public void OnUnfilteredLog( ActivityMonitorLogData data )
            {
                if( data.LogTime <= _lastOne ) ++NbClash;
                _lastOne = data.LogTime;
            }

            public void OnUnfilteredLog( ref ActivityMonitorLogData data )
            {
                if( data.LogTime <= _lastOne ) ++NbClash;
                _lastOne = data.LogTime;
            }

            public void OnOpenGroup( IActivityLogGroup group )
            {
                if( group.Data.LogTime <= _lastOne ) ++NbClash;
                _lastOne = group.Data.LogTime;
            }

            public void OnGroupClosing( IActivityLogGroup group, ref List<ActivityLogGroupConclusion>? conclusions )
            {
            }

            public void OnGroupClosed( IActivityLogGroup group, IReadOnlyList<ActivityLogGroupConclusion>? conclusions )
            {
                if( group.CloseLogTime <= _lastOne ) ++NbClash;
                _lastOne = group.CloseLogTime;
            }

            public void OnTopicChanged( string newTopic, string? fileName, int lineNumber )
            {
            }

            public void OnAutoTagsChanged( CKTrait newTrait )
            {
            }
        }

        [Test]
        public void DateTimeStamp_collision_can_not_happen()
        {
            ActivityMonitor m = new ActivityMonitor( applyAutoConfigurations: false );
            var detect = new DateTimeStampCollision();
            m.Output.RegisterClient( detect );
            for( int i = 0; i < 10; ++i )
            {
                m.UnfilteredLog( LogLevel.Info, null, "This should clash!", null );
            }
            for( int i = 0; i < 10; ++i )
            {
                m.Trace( "This should clash!" );
            }
            for( int i = 0; i < 10; ++i )
            {
                using( m.OpenTrace( "This should clash!" ) )
                {
                }
            }
             detect.NbClash.Should().Be( 0 );
        }
    }
}
