using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using CK.Core;
using FluentAssertions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CK.Core.Tests
{
    public static class Should
    {
        public static void Throw<T>(Action a) where T : Exception => a.ShouldThrow<T>();
        public static void Throw<T>(Action a, string because) where T : Exception => a.ShouldThrow<T>(because);
    }

#if !NET452
    class ExcludeFromCodeCoverageAttribute : Attribute
    {
    }
#endif

    static class TestHelper
    {
        static string _testFolder;
        static string _solutionFolder;
        
        static IActivityMonitor _monitor;
        static ActivityMonitorConsoleClient _console;

        static TestHelper()
        {
            _monitor = new ActivityMonitor();
            // Do not pollute the console by default... LogsToConsole does the job.
            _console = new ActivityMonitorConsoleClient();
        }

        public static IActivityMonitor ConsoleMonitor => _monitor; 

        public static bool LogsToConsole
        {
            get { return _monitor.Output.Clients.Contains( _console ); }
            set
            {
                if( value ) _monitor.Output.RegisterUniqueClient( c => c == _console, () => _console );
                else _monitor.Output.UnregisterClient( _console );
            }
        }

        public static string TestFolder
        {
            get
            {
                if( _testFolder == null ) InitializePaths();
                return _testFolder;
            }
        }

        public static string SolutionFolder
        {
            get
            {
                if( _solutionFolder == null ) InitializePaths();
                return _solutionFolder;
            }
        }

        public static void CleanupTestFolder()
        {
            CleanupFolder( TestFolder );
        }

        public static void CleanupFolder( string folder )
        {
            int tryCount = 0;
            for( ; ; )
            {
                try
                {
                    if( Directory.Exists( folder ) ) Directory.Delete( folder, true );
                    Directory.CreateDirectory( folder );
                    File.WriteAllText( Path.Combine( folder, "TestWrite.txt" ), "Test write works." );
                    File.Delete( Path.Combine( folder, "TestWrite.txt" ) );
                    return;
                }
                catch( Exception ex )
                {
                    if( ++tryCount == 20 ) throw;
                    ConsoleMonitor.Info().Send( ex, "While cleaning up test directory. Retrying." );
                    System.Threading.Thread.Sleep( 100 );
                }
            }
        }

        static void InitializePaths()
        {
            if( _solutionFolder != null ) return;
            _solutionFolder = Path.GetDirectoryName(Path.GetDirectoryName(GetTestProjectPath()));
            _testFolder = Path.Combine( _solutionFolder, "Tests", "CK.ActivityMonitor.Tests", "TestFolder" );
            Console.WriteLine($"SolutionFolder is: {_solutionFolder}.");
            Console.WriteLine($"TestFolder is: {_testFolder}.");
            Console.WriteLine($"Core path: {typeof(string).GetTypeInfo().Assembly.CodeBase}.");
            CleanupTestFolder();
        }

        static string GetTestProjectPath([CallerFilePath]string path = null) => Path.GetDirectoryName(path);

    }
}
