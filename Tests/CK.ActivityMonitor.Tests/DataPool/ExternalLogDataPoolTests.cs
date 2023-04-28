using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CK.Core.Tests.Monitoring
{
    [TestFixture]
    public class ExternalLogDataPoolTests
    {
        [TearDown]
        public void CheckNoMoreAliveExternalLogData()
        {
            ActivityMonitorExternalLogData.AliveCount.Should().Be( 0 );
        }

        [Test]
        public void ActivityMonitorExternalLogData_LogTime_Text_and_Tags_cannot_be_changed_after_AcquireExternalData_call()
        {
            var d = ActivityMonitor.StaticLogger.CreateActivityMonitorLogData( LogLevel.Info, ActivityMonitor.Tags.Empty, "text", null, "filename", 1, false );

            d.SetLogTime( DateTimeStamp.UtcNow );
            d.SetTags( ActivityMonitor.Tags.SecurityCritical );
            d.SetText( "Another..." );

            ActivityMonitorExternalLogData e = d.AcquireExternalData();
            ActivityMonitorExternalLogData.AliveCount.Should().Be( 1 );

            FluentActions.Invoking( () => d.SetLogTime( DateTimeStamp.UtcNow ) ).Should().Throw<InvalidOperationException>();
            FluentActions.Invoking( () => d.SetTags( ActivityMonitor.Tags.SecurityCritical ) ).Should().Throw<InvalidOperationException>();
            FluentActions.Invoking( () => d.SetText( "Another..." ) ).Should().Throw<InvalidOperationException>();

            ActivityMonitorExternalLogData e2 = d.AcquireExternalData();
            e2.Should().BeSameAs( e );
            ActivityMonitorExternalLogData.AliveCount.Should().Be( 1 );
            e.Release();
            ActivityMonitorExternalLogData.AliveCount.Should().Be( 1 );
            e.Release();
        }

        [Test]
        public void ActivityMonitorExternalLogData_pool_overflow()
        {
            var staticLogs = new List<string>();
            ActivityMonitor.StaticLogHandler h = delegate ( ref ActivityMonitorLogData d ) { staticLogs.Add( d.Text ); };
            ActivityMonitor.OnStaticLog += h;

            try
            {
                int capacity = ActivityMonitorExternalLogData.CurrentPoolCapacity;
                var externalLogData = new List<ActivityMonitorExternalLogData>();
                for( int i = 0; i < ActivityMonitorExternalLogData.CurrentPoolCapacity; ++i )
                {
                    var d = ActivityMonitor.StaticLogger.CreateActivityMonitorLogData( LogLevel.Info, ActivityMonitor.Tags.Empty, "text", null, "fileName", 0, false );
                    externalLogData.Add( d.AcquireExternalData() );
                }
                staticLogs.Should().BeEmpty();

                // First warning.
                var dInExcess1 = ActivityMonitor.StaticLogger.CreateActivityMonitorLogData( LogLevel.Info, ActivityMonitor.Tags.Empty, "in excess", null, "fileName", 0, false );
                externalLogData.Add( dInExcess1.AcquireExternalData() );

                // Return them to the pool: the pool hits its CurrentPoolCapacity.
                foreach( var e in externalLogData ) e.Release();
                externalLogData.Clear();

                staticLogs.Should().HaveCount( 1 );
                capacity += ActivityMonitorExternalLogData.PoolCapacityIncrement;
                staticLogs[0].Should().Be( $"The log data pool has been increased to {capacity}." );
                staticLogs.Clear();

                // One more warning.
                for( int i = 0; i < capacity; ++i )
                {
                    var d = ActivityMonitor.StaticLogger.CreateActivityMonitorLogData( LogLevel.Info, ActivityMonitor.Tags.Empty, "text", null, "fileName", 0, false );
                    externalLogData.Add( d.AcquireExternalData() );
                }
                staticLogs.Should().BeEmpty();

                var dInExcess2 = ActivityMonitor.StaticLogger.CreateActivityMonitorLogData( LogLevel.Info, ActivityMonitor.Tags.Empty, "in excess", null, "fileName", 0, false );
                externalLogData.Add( dInExcess2.AcquireExternalData() );

                foreach( var e in externalLogData ) e.Release();
                externalLogData.Clear();

                capacity += ActivityMonitorExternalLogData.PoolCapacityIncrement;
                staticLogs.Should().HaveCount( 1 );
                staticLogs[0].Should().Be( $"The log data pool has been increased to {capacity}." );

                // Fill the pool up to the max.
                for( int i = 0; i < ActivityMonitorExternalLogData.MaximalCapacity; ++i )
                {
                    var d = ActivityMonitor.StaticLogger.CreateActivityMonitorLogData( LogLevel.Info, ActivityMonitor.Tags.Empty, "text", null, "fileName", 0, false );
                    externalLogData.Add( d.AcquireExternalData() );
                }

                // Release them: the pool is now full.
                foreach( var e in externalLogData ) e.Release();
                externalLogData.Clear();

                ActivityMonitorExternalLogData.CurrentPoolCapacity.Should().Be( ActivityMonitorExternalLogData.MaximalCapacity );
                // We have received n new warnings per increment until the MaximalCapacity.
                staticLogs.Should().HaveCount( (ActivityMonitorExternalLogData.MaximalCapacity - capacity ) / ActivityMonitorExternalLogData.PoolCapacityIncrement + 2 );
                staticLogs[^1].Should().StartWith( $"The log data pool reached its maximal capacity of {ActivityMonitorExternalLogData.MaximalCapacity}." );
                staticLogs.Clear();

                // Error logs are emitted only once per second.
                // Fill the pool up to the max again.
                for( int i = 0; i < ActivityMonitorExternalLogData.MaximalCapacity; ++i )
                {
                    var d = ActivityMonitor.StaticLogger.CreateActivityMonitorLogData( LogLevel.Info, ActivityMonitor.Tags.Empty, "text", null, "fileName", 0, false );
                    externalLogData.Add( d.AcquireExternalData() );
                }

                // Release them: the pool is now full.
                foreach( var e in externalLogData ) e.Release();
                externalLogData.Clear();

                staticLogs.Should().BeEmpty();

                Thread.Sleep( 1000 );

                // Error logs are emitted only once per second.
                // Fill the pool up to the max again.
                for( int i = 0; i < ActivityMonitorExternalLogData.MaximalCapacity; ++i )
                {
                    var d = ActivityMonitor.StaticLogger.CreateActivityMonitorLogData( LogLevel.Info, ActivityMonitor.Tags.Empty, "text", null, "fileName", 0, false );
                    externalLogData.Add( d.AcquireExternalData() );
                }

                // Release them: the pool is now full.
                foreach( var e in externalLogData ) e.Release();
                externalLogData.Clear();

                staticLogs.Should().HaveCount( 1 );
                staticLogs[0].Should().StartWith( $"The log data pool reached its maximal capacity of {ActivityMonitorExternalLogData.MaximalCapacity}." );

                // Free all.
                foreach( var e in externalLogData ) e.Release();
                externalLogData.Clear();
            }
            finally
            {
                ActivityMonitor.OnStaticLog -= h;
            }
        }
    }
}
