using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using NUnit.Framework;
using FluentAssertions;

namespace CK.Core.Tests.Monitoring
{
    public class LogFileTests 
    {
        public LogFileTests()
        {
            TestHelper.InitializePaths();
        }

        [Test]
        public void testing_file_write()
        {
            var exMsg = "The-Test-Exception-Message " + Guid.NewGuid().ToString();
            var comment = "Produced by testing_file_write " + Guid.NewGuid().ToString();
            ActivityMonitor.CriticalErrorCollector.Add( new Exception( exMsg ), comment );
            ActivityMonitor.CriticalErrorCollector.WaitOnErrorFromBackgroundThreadsPending();
            string lastWrite = Directory.EnumerateFiles( LogFile.CriticalErrorsPath )
                                        .OrderByDescending( n => File.GetCreationTimeUtc( n ) )
                                        .First();

            File.ReadAllText( lastWrite ).Should().Contain( exMsg ).And.Contain( comment );
        }

        [Test]
        public void RootLogPath_can_not_be_changed()
        {
            string current = LogFile.RootLogPath;
            LogFile.RootLogPath = current;
            Should.Throw<InvalidOperationException>( () => LogFile.RootLogPath = current + "sub" );
        }

    }
}
