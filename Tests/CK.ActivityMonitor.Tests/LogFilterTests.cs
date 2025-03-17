using Shouldly;
using System;
using NUnit.Framework;

namespace CK.Core.Tests.Monitoring;

public class LogFilterTests
{
    [Test]
    public void CombineLevelTests()
    {
        LogFilter.Combine( LogLevelFilter.Error, LogLevelFilter.Fatal ).ShouldBe( LogLevelFilter.Error );
        LogFilter.Combine( LogLevelFilter.None, LogLevelFilter.Fatal ).ShouldBe( LogLevelFilter.Fatal );
        LogFilter.Combine( LogLevelFilter.Error, LogLevelFilter.None ).ShouldBe( LogLevelFilter.Error );
        LogFilter.Combine( LogLevelFilter.None, LogLevelFilter.None ).ShouldBe( LogLevelFilter.None );
        LogFilter.Combine( LogLevelFilter.Info, LogLevelFilter.Error ).ShouldBe( LogLevelFilter.Info );
    }

    [Test]
    public void CombineLogTests()
    {
        LogFilter f = new LogFilter( LogLevelFilter.None, LogLevelFilter.Error );
        LogFilter f2 = f.SetGroup( LogLevelFilter.Info );
        f2.Line.ShouldBe( LogLevelFilter.Error );
        f2.Group.ShouldBe( LogLevelFilter.Info );
        LogFilter f3 = new LogFilter( LogLevelFilter.Trace, LogLevelFilter.Info );
        LogFilter f4 = f2.Combine( f3 );
        f4.ShouldBe( f3 );
        (f4 == f3).ShouldBeTrue();
    }

    [Test]
    public void ToStringTests()
    {
        LogFilter.Undefined.ToString().ShouldBe( "Undefined" );
        LogFilter.Terse.ToString().ShouldBe( "Terse" );
        LogFilter.Fatal.ToString().ShouldBe( "Fatal" );
        LogFilter.Trace.ToString().ShouldBe( "Trace" );
        LogFilter.Debug.ToString().ShouldBe( "Debug" );
        LogFilter.Invalid.ToString().ShouldBe( "Invalid" );
        new LogFilter( LogLevelFilter.Warn, LogLevelFilter.Error ).ToString().ShouldBe( "{Warn,Error}" );
    }

    [Test]
    public void ParseTests()
    {
        LogFilter.Parse( "Undefined" ).ShouldBe( LogFilter.Undefined );
        LogFilter.Parse( "Debug" ).ShouldBe( LogFilter.Debug );
        LogFilter.Parse( "Trace" ).ShouldBe( LogFilter.Trace );
        LogFilter.Parse( "Verbose" ).ShouldBe( LogFilter.Verbose );
        LogFilter.Parse( "Monitor" ).ShouldBe( LogFilter.Monitor );
        LogFilter.Parse( "Terse" ).ShouldBe( LogFilter.Terse );
        LogFilter.Parse( "Release" ).ShouldBe( LogFilter.Release );
        LogFilter.Parse( "Fatal" ).ShouldBe( LogFilter.Fatal );

        LogFilter.Parse( "Quiet" ).ShouldBe( LogFilter.Quiet );
        LogFilter.Parse( "Minimal" ).ShouldBe( LogFilter.Minimal );
        LogFilter.Parse( "Normal" ).ShouldBe( LogFilter.Normal );
        LogFilter.Parse( "Detailed" ).ShouldBe( LogFilter.Detailed );
        LogFilter.Parse( "Diagnostic" ).ShouldBe( LogFilter.Diagnostic );


        LogFilter.Parse( "{None,None}" ).ShouldBe( LogFilter.Undefined );
        LogFilter.Parse( "{Warn,None}" ).ShouldBe( new LogFilter( LogLevelFilter.Warn, LogLevelFilter.None ) );
        LogFilter.Parse( "{Error,Warn}" ).ShouldBe( new LogFilter( LogLevelFilter.Error, LogLevelFilter.Warn ) );
        LogFilter.Parse( "{Fatal,None}" ).ShouldBe( new LogFilter( LogLevelFilter.Fatal, LogLevelFilter.None ) );
        LogFilter.Parse( "{Error,Error}" ).ShouldBe( LogFilter.Release );
        LogFilter.Parse( "{Info,Warn}" ).ShouldBe( LogFilter.Terse );
        LogFilter.Parse( "{Fatal,Invalid}" ).ShouldBe( new LogFilter( LogLevelFilter.Fatal, LogLevelFilter.Invalid ) );

        LogFilter.Parse( "{ Error , Error }" ).ShouldBe( LogFilter.Release );
        LogFilter.Parse( "{   Trace    ,    Info   }" ).ShouldBe( LogFilter.Verbose );

        Action fail = () => LogFilter.Parse( " {Error,Error}" );
        fail.ShouldThrow<Exception>();
        fail = () => LogFilter.Parse( "{Error,Error} " );
        fail.ShouldThrow<Exception>();
        fail = () => LogFilter.Parse( "Error,Error}" );
        fail.ShouldThrow<Exception>();
        fail = () => LogFilter.Parse( "{Error,Error" );
        fail.ShouldThrow<Exception>();
        fail = () => LogFilter.Parse( "{Error,,Error}" );
        fail.ShouldThrow<Exception>();
        fail = () => LogFilter.Parse( "{Error,Warn,Trace}" );
        fail.ShouldThrow<Exception>();
        fail = () => LogFilter.Parse( "{}" );
        fail.ShouldThrow<Exception>();
    }

}
