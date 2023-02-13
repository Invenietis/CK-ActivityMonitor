using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;

namespace CK.Core.Tests.Monitoring
{
    [TestFixture]
    public class ExternalLogDataPoolTests
    {
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
                    var d = new ActivityMonitorLogData( LogLevel.Info, ActivityMonitor.Tags.Empty, "nop", null );
                    externalLogData.Add( d.AcquireExternalData() );
                }
                staticLogs.Should().BeEmpty();

                // First warning.
                var dInExcess1 = new ActivityMonitorLogData( LogLevel.Info, ActivityMonitor.Tags.Empty, "in excess", null );
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
                    var d = new ActivityMonitorLogData( LogLevel.Info, ActivityMonitor.Tags.Empty, "nop", null );
                    externalLogData.Add( d.AcquireExternalData() );
                }
                staticLogs.Should().BeEmpty();

                var dInExcess2 = new ActivityMonitorLogData( LogLevel.Info, ActivityMonitor.Tags.Empty, "in excess", null );
                externalLogData.Add( dInExcess2.AcquireExternalData() );

                foreach( var e in externalLogData ) e.Release();
                externalLogData.Clear();

                capacity += ActivityMonitorExternalLogData.PoolCapacityIncrement;
                staticLogs.Should().HaveCount( 1 );
                staticLogs[0].Should().Be( $"The log data pool has been increased to {capacity}." );

                // Fill the pool up to the max.
                for( int i = 0; i < ActivityMonitorExternalLogData.MaximalCapacity; ++i )
                {
                    var d = new ActivityMonitorLogData( LogLevel.Info, ActivityMonitor.Tags.Empty, "nop", null );
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
                    var d = new ActivityMonitorLogData( LogLevel.Info, ActivityMonitor.Tags.Empty, "nop", null );
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
                    var d = new ActivityMonitorLogData( LogLevel.Info, ActivityMonitor.Tags.Empty, "nop", null );
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
