using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using CK.Core;

namespace WeakNameConsole
{
    class Program
    {
        /// <summary>
        /// This is compiled with the current version of CK.ActivityMonitor
        /// and CK.ActivityMonitor.SimpleSender.
        /// CodeCakeBuilder sets an old CK.Core (v9.0.0) and an old CK.Text (v6.0.0) in the bin folder
        /// and runs this program.
        /// This works and the 2 conflicts are reported in netcoreapp2.1 (after a publish of course)
        /// and in Net461.
        /// </summary>
        /// <param name="args">Unused.</param>
        /// <returns>The number of conflicts (actually 2).</returns>
        static int Main(string[] args)
        {
            IReadOnlyList<AssemblyLoadConflict> conflicts = null;
            using( var w = WeakAssemblyNameResolver.TemporaryInstall( c => conflicts = c ) )
            {
                ActualProgram.Run();
            }
            foreach( var c in conflicts )
            {
                Console.WriteLine( c );
            }
            return conflicts.Count;
        }

    }
}
