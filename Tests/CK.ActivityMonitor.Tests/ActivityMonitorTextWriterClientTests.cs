using System;
using System.Text;
using NUnit.Framework;
using FluentAssertions;

namespace CK.Core.Tests.Monitoring
{
    public class ActivityMonitorTextWriterClientTests
    {
        [Test]
        public void logging_multiple_lines()
        {
            var m = new ActivityMonitor( false );
            m.MinimalFilter = LogFilter.Trace;
            StringBuilder b = new StringBuilder();
            var client = new ActivityMonitorTextWriterClient( s => b.Append( s ), LogFilter.Undefined, '|' );
            m.Output.RegisterClient( client );
            using( TestHelper.Monitor.TemporarilySetMinimalFilter( LogFilter.Trace ) )
            using( m.Output.CreateBridgeTo( TestHelper.Monitor.Output.BridgeTarget ) )
            {
                using( m.OpenInfo( "IL1" + Environment.NewLine + "IL2" + Environment.NewLine + "IL3" ) )
                {
                    using( m.OpenTrace( "TL1" + Environment.NewLine + "TL2" + Environment.NewLine + "TL3" ) )
                    {
                        m.Warn( "WL1" + Environment.NewLine + "WL2" + Environment.NewLine + "WL3" );
                        m.CloseGroup( new[]
                        {
                            new ActivityLogGroupConclusion("c1"),
                            new ActivityLogGroupConclusion("c2"),
                            new ActivityLogGroupConclusion("Multi"+Environment.NewLine+"Line"+Environment.NewLine),
                            new ActivityLogGroupConclusion("Another"+Environment.NewLine+"Multi"+Environment.NewLine+"Line"+Environment.NewLine)
                        } );
                    }
                }
            }
            string result = b.ToString();
            result.Should().Be(
@"> Info: IL1
|       IL2
|       IL3
|  > Trace: TL1
|  |        TL2
|  |        TL3
|  |  - Warn: WL1
|  |          WL2
|  |          WL3
|  < c1 - c2
|  < Multi
|    Line
|  < Another
|    Multi
|    Line
".NormalizeEOL() );
        }
    }
}
