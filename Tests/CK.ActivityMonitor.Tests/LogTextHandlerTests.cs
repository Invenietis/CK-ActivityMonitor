using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core.Tests.Monitoring
{
    [TestFixture]
    public class LogTextHandlerTests
    {
        [Test]
        public void LogTextHandler_skips_text_building()
        {
            int i = 0;
            var m = new ActivityMonitor( false );
            m.MinimalFilter = LogFilter.Verbose;
            m.Log( LogLevel.Info, "constant" );
            m.Log( LogLevel.Info, $"I'm computed {i++}." );
            i.Should().Be( 1 );
            m.Log( LogLevel.Trace, $"I'm NOT computed {i++}." );
            i.Should().Be( 1 );
        }
    }
}
