using FluentAssertions;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace CK.Core.Tests.Monitoring
{
    [TestFixture]
    public class TagFilteringTests
    {
        [Test]
        public void single_tag_configuration()
        {
            var m = new ActivityMonitor( applyAutoConfigurations: false );
            var c = m.Output.RegisterClient( new StupidStringClient() );

            ActivityMonitor.DefaultFilter.Should().Be( LogFilter.Trace );

            m.ActualFilter.Should().Be( LogFilter.Undefined );
            m.Trace( "Trace1" );
            m.OpenTrace( "OTrace1" );

            // For logs with Tag1, Release {Error, Error} is applied.
            ActivityMonitor.Tags.SetFilters( new[] { (TestHelper.Tag1, LogClamper.Parse( "Release!" )) } );

            m.Trace( TestHelper.Tag1, "NOSHOW" );
            m.OpenTrace( TestHelper.Tag1, "NOSHOW" );
            m.Trace( "Trace2" );
            m.Warn( TestHelper.Tag1 | TestHelper.Tag2, "NOSHOW" );
            m.OpenWarn( TestHelper.Tag1 | TestHelper.Tag2, "NOSHOW" );
            m.Warn( TestHelper.Tag1 | TestHelper.Tag2, $"NOSHOW{Environment.TickCount}" );
            m.OpenWarn( TestHelper.Tag1 | TestHelper.Tag2, $"NOSHOW{Environment.TickCount}" );
            m.OpenTrace( "OTrace2" );

            ActivityMonitor.Tags.ClearAll();

            c.Entries.Select( e => e.Data.Text ).ToArray().Should().BeEquivalentTo( new[] { "Trace1", "OTrace1", "Trace2", "OTrace2" }, o => o.WithStrictOrdering() );
        }

        [Test]
        public void tags_configuration_orders_matters_since_first_match_wins()
        {
            var m = new ActivityMonitor( applyAutoConfigurations: false );
            var c = m.Output.RegisterClient( new StupidStringClient() );

            int hole = Environment.TickCount;

            ActivityMonitor.DefaultFilter.Should().Be( LogFilter.Trace );

            // ActualFilter is Terse, 
            m.MinimalFilter = LogFilter.Terse;
            m.ActualFilter.Should().Be( LogFilter.Terse );

            m.Trace( "NOSHOW" );
            m.OpenTrace( "NOSHOW" );

            ActivityMonitor.Tags.SetFilters( new[]
            {
                    // Tag2 => Debug is the top priority here (Clamping or not a Debug filter doesn't matter).
                    (TestHelper.Tag2, LogClamper.Parse( "Debug" )),
                    // Tag1 => Cuts to Trace: Tag1|Tag2 in Debug will appear because of the above,
                    //         but Tag1 alone in Debug will not appear.
                    (TestHelper.Tag1, LogClamper.Parse( "Trace!" )),
                    // Tag3 (alone) will not log Error.
                    (TestHelper.Tag3, LogClamper.Parse( "{Trace,Fatal}!" )),
                } );

            m.Trace( TestHelper.Tag1, "Trace1" );
            m.Debug( TestHelper.Tag1, "NOSHOW" );
            m.Debug( TestHelper.Tag1, $"NOSHOW{hole}" );
            m.Log( LogLevel.Debug, TestHelper.Tag1, "NOSHOW" );
            m.Log( LogLevel.Debug, TestHelper.Tag1, $"NOSHOW{hole}" );
            m.Trace( TestHelper.Tag1, $"Trace1{hole}" );

            m.Debug( TestHelper.Tag2, "Trace2" );
            m.Debug( TestHelper.Tag2, $"Trace{hole}" );
            m.Log( LogLevel.Debug, TestHelper.Tag2, "Log" );
            m.Log( LogLevel.Debug, TestHelper.Tag2, $"Log{hole}" );

            m.OpenTrace( TestHelper.Tag3, "OTrace1" );
            m.Error( TestHelper.Tag3, "NOSHOW" );
            m.Error( TestHelper.Tag3, $"NOSHOW{hole}" );

            m.Debug( TestHelper.Tag1 | TestHelper.Tag2, "Combined" );
            m.Debug( TestHelper.Tag1 | TestHelper.Tag2, $"Combined{hole}" );

            ActivityMonitor.Tags.ClearAll();
            m.Trace( "NOSHOW" );
            m.OpenTrace( "NOSHOW" );

            c.Entries.Select( e => e.Data.Text ).Concatenate()
                .Should().Be( $"Trace1, Trace1{hole}, Trace2, Trace{hole}, Log, Log{hole}, OTrace1, Combined, Combined{hole}" );
        }

    }
}
