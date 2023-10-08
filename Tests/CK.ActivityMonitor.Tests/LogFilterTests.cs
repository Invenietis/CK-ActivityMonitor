using FluentAssertions;
using System;
using NUnit.Framework;

namespace CK.Core.Tests.Monitoring
{
    public class LogFilterTests
    {
        [Test]
        public void CombineLevelTests()
        {
             LogFilter.Combine( LogLevelFilter.Error, LogLevelFilter.Fatal ).Should().Be( LogLevelFilter.Error );
             LogFilter.Combine( LogLevelFilter.None, LogLevelFilter.Fatal ).Should().Be( LogLevelFilter.Fatal  );
             LogFilter.Combine( LogLevelFilter.Error, LogLevelFilter.None ).Should().Be( LogLevelFilter.Error  );
             LogFilter.Combine( LogLevelFilter.None, LogLevelFilter.None ).Should().Be( LogLevelFilter.None  );
             LogFilter.Combine( LogLevelFilter.Info, LogLevelFilter.Error ).Should().Be( LogLevelFilter.Info  );
        }

        [Test]
        public void CombineLogTests()
        {
            LogFilter f = new LogFilter( LogLevelFilter.None, LogLevelFilter.Error );
            LogFilter f2 = f.SetGroup( LogLevelFilter.Info );
            f2.Line.Should().Be(LogLevelFilter.Error);
            f2.Group.Should().Be(LogLevelFilter.Info );
            LogFilter f3 = new LogFilter( LogLevelFilter.Trace, LogLevelFilter.Info );
            LogFilter f4 = f2.Combine( f3 );
             f4.Should().Be(f3 );
             (f4 == f3).Should().BeTrue( );
        }

        [Test]
        public void ToStringTests()
        {
            LogFilter.Undefined.ToString().Should().Be("Undefined");
            LogFilter.Terse.ToString().Should().Be("Terse");
            LogFilter.Fatal.ToString().Should().Be("Fatal");
            LogFilter.Trace.ToString().Should().Be("Trace");
            LogFilter.Debug.ToString().Should().Be("Debug");
            LogFilter.Invalid.ToString().Should().Be("Invalid");
            new LogFilter(LogLevelFilter.Warn, LogLevelFilter.Error).ToString().Should().Be("{Warn,Error}");
        }

        [Test]
        public void ParseTests()
        {
            LogFilter.Parse( "Undefined" ).Should().Be( LogFilter.Undefined );
            LogFilter.Parse( "Debug" ).Should().Be( LogFilter.Debug );
            LogFilter.Parse( "Trace" ).Should().Be( LogFilter.Trace );
            LogFilter.Parse( "Verbose" ).Should().Be( LogFilter.Verbose );
            LogFilter.Parse( "Monitor" ).Should().Be( LogFilter.Monitor );
            LogFilter.Parse( "Terse" ).Should().Be( LogFilter.Terse );
            LogFilter.Parse( "Release" ).Should().Be( LogFilter.Release );
            LogFilter.Parse( "Fatal" ).Should().Be( LogFilter.Fatal );

            LogFilter.Parse( "Quiet" ).Should().Be( LogFilter.Quiet );
            LogFilter.Parse( "Minimal" ).Should().Be( LogFilter.Minimal );
            LogFilter.Parse( "Normal" ).Should().Be( LogFilter.Normal );
            LogFilter.Parse( "Detailed" ).Should().Be( LogFilter.Detailed );
            LogFilter.Parse( "Diagnostic" ).Should().Be( LogFilter.Diagnostic );


            LogFilter.Parse( "{None,None}" ).Should().Be( LogFilter.Undefined );
            LogFilter.Parse( "{Warn,None}" ).Should().Be( new LogFilter( LogLevelFilter.Warn, LogLevelFilter.None ) );
            LogFilter.Parse( "{Error,Warn}" ).Should().Be( new LogFilter( LogLevelFilter.Error, LogLevelFilter.Warn ) );
            LogFilter.Parse( "{Fatal,None}" ).Should().Be( new LogFilter( LogLevelFilter.Fatal, LogLevelFilter.None ) );
            LogFilter.Parse( "{Error,Error}" ).Should().Be( LogFilter.Release );
            LogFilter.Parse( "{Info,Warn}" ).Should().Be( LogFilter.Terse );
            LogFilter.Parse( "{Fatal,Invalid}" ).Should().Be( new LogFilter( LogLevelFilter.Fatal, LogLevelFilter.Invalid ) );

            LogFilter.Parse( "{ Error , Error }" ).Should().Be( LogFilter.Release );
            LogFilter.Parse( "{   Trace    ,    Info   }" ).Should().Be( LogFilter.Verbose );

            Action fail = () => LogFilter.Parse( " {Error,Error}" );
            fail.Should().Throw<Exception>();
            fail = () => LogFilter.Parse( "{Error,Error} " );
            fail.Should().Throw<Exception>();
            fail = () => LogFilter.Parse( "Error,Error}" );
            fail.Should().Throw<Exception>();
            fail = () => LogFilter.Parse( "{Error,Error" );
            fail.Should().Throw<Exception>();
            fail = () => LogFilter.Parse( "{Error,,Error}" );
            fail.Should().Throw<Exception>();
            fail = () => LogFilter.Parse( "{Error,Warn,Trace}" );
            fail.Should().Throw<Exception>();
            fail = () => LogFilter.Parse( "{}" );
            fail.Should().Throw<Exception>();
        }

    }
}
