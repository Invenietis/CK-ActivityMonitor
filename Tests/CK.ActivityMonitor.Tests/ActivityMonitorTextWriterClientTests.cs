using System;
using System.Text;
using NUnit.Framework;
using FluentAssertions;

namespace CK.Core.Tests.Monitoring
{
    public class ActivityMonitorTextWriterClientTests
    {
        [Test]
        public void logging_groups()
        {
            var m = new ActivityMonitor( false );
            m.MinimalFilter = LogFilter.Debug;
            StringBuilder b = new StringBuilder();
            var client = new ActivityMonitorTextWriterClient( s => b.Append( s ), LogClamper.Undefined, '|' );
            m.Output.RegisterClient( client );
            using( m.OpenInfo( "G1." ) )
            {
                m.Info( "I1." );
                using( m.OpenTrace( "G2." ) )
                {
                    m.Error( "E1." );
                    m.Warn( "W1." );
                    m.CloseGroup( new[]
                    {
                        new ActivityLogGroupConclusion("c1"),
                        new ActivityLogGroupConclusion("c2"),
                        new ActivityLogGroupConclusion("Multi"+Environment.NewLine+"Line"+Environment.NewLine),
                        new ActivityLogGroupConclusion("Another"+Environment.NewLine+"Multi"+Environment.NewLine+"Line"+Environment.NewLine)
                    } );
                }
            }
            using( m.OpenInfo( "G1-1." + Environment.NewLine + "G1-2." + Environment.NewLine + "G1-3" ) )
            {
                using( m.OpenTrace( "G2-1" + Environment.NewLine + "G2-2" + Environment.NewLine + "G2-3" ) )
                {
                    m.Trace( "T1-1." + Environment.NewLine + "T1-2" + Environment.NewLine + "T1-3" );
                    m.CloseGroup( new[]
                    {
                        new ActivityLogGroupConclusion("c1"),
                        new ActivityLogGroupConclusion("c2"),
                        new ActivityLogGroupConclusion("Multi"+Environment.NewLine+"Line"+Environment.NewLine),
                        new ActivityLogGroupConclusion("Another"+Environment.NewLine+"Multi"+Environment.NewLine+"Line"+Environment.NewLine)
                    } );
                }
            }
            string result = b.ToString();
            result.Trim().Should().Be( @"
> i [] G1.
| i [] I1.
| > t [] G2.
| | E [] E1.
| | W [] W1.
| < c1
|   - c2
|   - Multi
|     Line
|   - Another
|     Multi
|     Line
> i [] G1-1.
|   G1-2.
|   G1-3
| > t [] G2-1
| |   G2-2
| |   G2-3
| | t [] T1-1.
| |   T1-2
| |   T1-3
| < c1
|   - c2
|   - Multi
|     Line
|   - Another
|     Multi
|     Line
".Trim().ReplaceLineEndings() );
        }

        [Test]
        public void logging_lines()
        {
            var m = new ActivityMonitor( false );
            m.MinimalFilter = LogFilter.Debug;
            StringBuilder b = new StringBuilder();
            var client = new ActivityMonitorTextWriterClient( s => b.Append( s ), LogClamper.Undefined, '|' );
            m.Output.RegisterClient( client );
            m.Debug( "One." );
            m.Debug( "Two." );
            m.Fatal( "Three." );
            m.Error( "Four." );

            m.Debug( $"One1.{Environment.NewLine}One2.{Environment.NewLine}One3." );
            m.Debug( $"Two1.{Environment.NewLine}Two2.{Environment.NewLine}Two3." );
            m.Fatal( $"Three1.{Environment.NewLine}Three2.{Environment.NewLine}Three3." );
            m.Error( $"Four1.{Environment.NewLine}Four2.{Environment.NewLine}Four3." );
            string result = b.ToString();
            result.Trim().Should().Be( @"
d [] One.
  [] Two.
F [] Three.
E [] Four.
d [] One1.
  One2.
  One3.
  [] Two1.
  Two2.
  Two3.
F [] Three1.
  Three2.
  Three3.
E [] Four1.
  Four2.
  Four3.
".Trim().ReplaceLineEndings() );
        }


        static readonly CKTrait Sql = ActivityMonitor.Tags.Register( "Sql" );
        static readonly CKTrait Perf = ActivityMonitor.Tags.Register( "Perf" );
        static readonly CKTrait Google = ActivityMonitor.Tags.Register( "Google" );
        static readonly CKTrait Test = ActivityMonitor.Tags.Register( "Test" );

        [Test]
        public void logging_with_tags()
        {
            var m = new ActivityMonitor( false );
            m.MinimalFilter = LogFilter.Debug;
            StringBuilder b = new StringBuilder();
            var client = new ActivityMonitorTextWriterClient( s => b.Append( s ), LogClamper.Undefined, '|' );
            m.Output.RegisterClient( client );
            using( m.OpenInfo( Sql|Google, "One." ) )
            {
                using( m.OpenTrace( Perf, "Two." ) )
                {
                    m.Debug( Perf, "Same tags appear." );
                    m.Warn( Test | Google | Perf | Sql, "Three." );
                    m.Debug( Test | Google | Perf | Sql, "Same tags appear." );
                }
            }
            string result = b.ToString();
            result.Trim().Should().Be( @"
> i [Google|Sql] One.
| > t [Perf] Two.
| | d [Perf] Same tags appear.
| | W [Google|Perf|Sql|Test] Three.
| | d [Google|Perf|Sql|Test] Same tags appear.
".Trim().ReplaceLineEndings() );
        }

    }
}
