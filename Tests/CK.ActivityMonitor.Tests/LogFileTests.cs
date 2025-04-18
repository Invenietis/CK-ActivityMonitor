using System;
using NUnit.Framework;
using Shouldly;

namespace CK.Core.Tests.Monitoring;

public class LogFileTests
{
    public LogFileTests()
    {
        TestHelper.InitializePaths();
    }

    [Test]
    public void RootLogPath_can_not_be_changed()
    {
        string? current = LogFile.RootLogPath;
        Throw.DebugAssert( current != null, "We have initialize the paths." );
        LogFile.RootLogPath = current;
        Action fail = () => LogFile.RootLogPath = current + "sub";
        fail.ShouldThrow<InvalidOperationException>();
    }

}
