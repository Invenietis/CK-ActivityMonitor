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
    [Ignore("Behavior of the EventSource is unstable.")]
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

                ActivityMonitor.OnStaticLog += StaticLogger_OnStaticLog;

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
                TestHelper.Monitor.UnfilteredLog( data.Level, data.Tags, data.Text, data.Exception, data.FileName, data.LineNumber );
                logs.Add( (data.Level, data.Text, data.Tags) );
            }
        }

        [Test]
        public void DotNetEventSourceConfigurator_tests()
        {
            TestHelper.LogsToConsole = true;
            try
            {
                IReadOnlyList<(string Name, EventLevel? Level)> all = DotNetEventSourceCollector.GetSources();

                var initalConfig = DotNetEventSourceConfigurator.GetConfiguration();
                initalConfig.Split( ';' ).All( s => s.EndsWith( ":!" ) ).Should().BeTrue();

                using( TestHelper.Monitor.CollectTexts( out var logs ) )
                {
                    var c = "Microsoft-Windows-DotNETRuntime : C ; Microsoft-IO-RecyclableMemoryStream:V;; System.Runtime!";
                    DotNetEventSourceConfigurator.ApplyConfiguration( TestHelper.Monitor, c );
                    logs.Should().HaveCount( 1 );
                    logs[0].Should().Match( "Applying .Net EventSource configuration: '*" );
                }
                DotNetEventSourceCollector.GetLevel( "Microsoft-Windows-DotNETRuntime", out bool found1 ).Should().Be( EventLevel.Critical );
                DotNetEventSourceCollector.GetLevel( "Microsoft-IO-RecyclableMemoryStream", out bool found2 ).Should().Be( EventLevel.Verbose );
                DotNetEventSourceCollector.GetLevel( "System.Runtime", out bool found3 ).Should().Be( null );
                (found1 & found2 & found3).Should().BeTrue();

                using( TestHelper.Monitor.CollectTexts( out var logs ) )
                {
                    var c = "Microsoft-Windows-DotNETRuntime";
                    DotNetEventSourceConfigurator.ApplyConfiguration( TestHelper.Monitor, c );
                    logs.Should().HaveCount( 2 );
                    logs[1].Should().Match( "Missing level specification for EventSource 'Microsoft-Windows-DotNETRuntime', using Informational by default.*" );
                }

                using( TestHelper.Monitor.CollectTexts( out var logs ) )
                {
                    var c = "Microsoft-Windows-DotNETRuntime:KO";
                    DotNetEventSourceConfigurator.ApplyConfiguration( TestHelper.Monitor, c );
                    logs.Should().HaveCount( 2 );
                    logs[1].Should().Match( "Unrecognized level specification for EventSource 'Microsoft-Windows-DotNETRuntime:KO', using Informational by default.*" );
                }
                DotNetEventSourceConfigurator.ApplyConfiguration( TestHelper.Monitor, initalConfig );

                DotNetEventSourceCollector.GetLevel( "Microsoft-Windows-DotNETRuntime", out found1 ).Should().Be( null );
                DotNetEventSourceCollector.GetLevel( "Microsoft-IO-RecyclableMemoryStream", out found2 ).Should().Be( null );
                DotNetEventSourceCollector.GetLevel( "System.Runtime", out found3 ).Should().Be( null );
                (found1 & found2 & found3).Should().BeTrue();
            }
            finally
            {
                DotNetEventSourceCollector.DisableAll();
                TestHelper.LogsToConsole = false;
            }
        }
    }
}
