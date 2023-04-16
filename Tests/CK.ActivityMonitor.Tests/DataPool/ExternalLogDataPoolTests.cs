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
        public void ActivityMonitorExternalLogData_cannot_be_obtained_until_LogTime_is_set()
        {
            var d = new ActivityMonitorLogData( "external", LogLevel.Info, ActivityMonitor.Tags.Empty, "nop", null );
            FluentActions.Invoking( () => d.AcquireExternalData() ).Should().Throw<InvalidOperationException>();
            d.SetExplicitLogTime( DateTimeStamp.UtcNow );
            ActivityMonitorExternalLogData e = d.AcquireExternalData();
            ActivityMonitorExternalLogData.AliveCount.Should().Be( 1 );

            FluentActions.Invoking( () => d.SetExplicitLogTime( DateTimeStamp.UtcNow ) ).Should().Throw<InvalidOperationException>();
            FluentActions.Invoking( () => d.SetExplicitTags( ActivityMonitor.Tags.SecurityCritical ) ).Should().Throw<InvalidOperationException>();

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
                    var d = new ActivityMonitorLogData( "external", LogLevel.Info, ActivityMonitor.Tags.Empty, "nop", null );
                    d.SetExplicitLogTime( DateTimeStamp.UtcNow );
                    externalLogData.Add( d.AcquireExternalData() );
                }
                staticLogs.Should().BeEmpty();

                // First warning.
                var dInExcess1 = new ActivityMonitorLogData( "external", LogLevel.Info, ActivityMonitor.Tags.Empty, "in excess", null );
                dInExcess1.SetExplicitLogTime( DateTimeStamp.UtcNow );
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
                    var d = new ActivityMonitorLogData( "external", LogLevel.Info, ActivityMonitor.Tags.Empty, "nop", null );
                    d.SetExplicitLogTime( DateTimeStamp.UtcNow );
                    externalLogData.Add( d.AcquireExternalData() );
                }
                staticLogs.Should().BeEmpty();

                var dInExcess2 = new ActivityMonitorLogData( "external", LogLevel.Info, ActivityMonitor.Tags.Empty, "in excess", null );
                dInExcess2.SetExplicitLogTime( DateTimeStamp.UtcNow );
                externalLogData.Add( dInExcess2.AcquireExternalData() );

                foreach( var e in externalLogData ) e.Release();
                externalLogData.Clear();

                capacity += ActivityMonitorExternalLogData.PoolCapacityIncrement;
                staticLogs.Should().HaveCount( 1 );
                staticLogs[0].Should().Be( $"The log data pool has been increased to {capacity}." );

                // Fill the pool up to the max.
                for( int i = 0; i < ActivityMonitorExternalLogData.MaximalCapacity; ++i )
                {
                    var d = new ActivityMonitorLogData( "monitorId", LogLevel.Info, ActivityMonitor.Tags.Empty, "nop", null );
                    d.SetExplicitLogTime( DateTimeStamp.UtcNow );
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
                    var d = new ActivityMonitorLogData( "monitorId", LogLevel.Info, ActivityMonitor.Tags.Empty, "nop", null );
                    d.SetExplicitLogTime( DateTimeStamp.UtcNow );
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
                    var d = new ActivityMonitorLogData( "monitorId", LogLevel.Info, ActivityMonitor.Tags.Empty, "nop", null );
                    d.SetExplicitLogTime( DateTimeStamp.UtcNow );
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

        [Test]
        public async Task ThreadSafeLogger_and_LogRetainer_Async()
        {
            var logger = new AlternateThreadSafeLogger( "Test" );
            var retainer = new LogRetainerClient( 100 );
            logger.Monitor.Output.RegisterClient( retainer );

            var sender1 = Task.Run( () => Send( logger, "Sender1" ) );
            var sender2 = Task.Run( () => Send( logger, "Sender2" ) );

            await sender1;
            await sender2;
            logger.Stop();
            await logger.Stopped;

            retainer.Retained.Should().HaveCount( 100 );
            ActivityMonitorExternalLogData.AliveCount.Should().Be( 100 );
            int s1 = retainer.Retained.Count( e => e.Text.StartsWith( "Sender1" ) );
            int s2 = retainer.Retained.Count( e => e.Text.StartsWith( "Sender2" ) );
            s1.Should().BeGreaterThan( 0 );
            s2.Should().BeGreaterThan( 0 );
            (s1 + s2).Should().Be( 100 );

            for( int i = 0; i < 50; i++ ) retainer.Retained[i].Release();
            ActivityMonitorExternalLogData.AliveCount.Should().Be( 50 );
            for( int i = 50; i < 100; i++ ) retainer.Retained[i].Release();
            ActivityMonitorExternalLogData.AliveCount.Should().Be( 0 );

            static void Send( IActivityLogger logger, string message )
            {
                for( int i = 0; i < 10000; ++i )
                {
                    Thread.Sleep( 0 );
                    logger.Info( $"{message} n°{i}." );
                }
            }
        }
    }
}
