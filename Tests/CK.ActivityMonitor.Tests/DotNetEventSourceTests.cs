using FluentAssertions;
using Microsoft.IO;
using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;

namespace CK.Core.Tests.Monitoring
{
    [TestFixture]
    public class DotNetEventSourceTests
    {
        [Test]
        public void listing_enabling_and_disabling_sources_work()
        {
            var logs = new List<(LogLevel Level, string Text, CKTrait Tags)>();
            TestHelper.LogsToConsole = true;
            try
            {
                IReadOnlyList<(string Name, EventLevel? Level)> all = DotNetEventSourceCollector.GetSources();
                TestHelper.Monitor.Info( all.Select( s => s.ToString() ).Concatenate() );

                ActivityMonitor.StaticLogger.OnStaticLog += StaticLogger_OnStaticLog;

                RecyclableMemoryStreamManager.Events.Writer.IsEnabled( EventLevel.Verbose, EventKeywords.All ).Should().BeFalse();

                Assume.That( all.Any( s => s.Name == "Microsoft-IO-RecyclableMemoryStream" ) );
                DotNetEventSourceCollector.Enable( "Microsoft-IO-RecyclableMemoryStream", EventLevel.Verbose ).Should().BeTrue();

                RecyclableMemoryStreamManager.Events.Writer.IsEnabled( EventLevel.Verbose, EventKeywords.All ).Should().BeTrue();

                using( var m = new RecyclableMemoryStream( Util.RecyclableStreamManager ) )
                {
                    m.Write( Encoding.UTF8.GetBytes( "Hello World!" ) );
                }
                logs.Should().HaveCount( 3 );

                using( var m = new RecyclableMemoryStream( Util.RecyclableStreamManager ) )
                {
                    m.Write( Encoding.UTF8.GetBytes( "Hello World!" ) );
                }
                logs.Should().HaveCount( 5 );

                logs.All( l => (l.Level & LogLevel.IsFiltered) != 0 ).Should().BeTrue();
                logs.All( l => l.Tags == DotNetEventSourceCollector.EventSourceTag ).Should().BeTrue();

                DotNetEventSourceCollector.Disable( "Microsoft-IO-RecyclableMemoryStream" ).Should().BeTrue();
                using( var m = new RecyclableMemoryStream( Util.RecyclableStreamManager ) )
                {
                    m.Write( Encoding.UTF8.GetBytes( "Hello World!" ) );
                }
                logs.Should().HaveCount( 5, "No more logs." );
            }
            finally
            {
                DotNetEventSourceCollector.Disable( "Microsoft-IO-RecyclableMemoryStream" );
                TestHelper.LogsToConsole = false;
            }


            void StaticLogger_OnStaticLog( ref ActivityMonitorLogData data )
            {
                // No concurrency issue here. Keep it simple.
                TestHelper.Monitor.UnfilteredLog( ref data );
                logs.Add( (data.Level, data.Text, data.Tags) );
            }
        }

    }
}
