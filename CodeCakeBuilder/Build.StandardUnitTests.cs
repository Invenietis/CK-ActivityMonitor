using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Solution;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.NUnit;
using System.Collections.Generic;
using System.Linq;

namespace CodeCake
{
    public partial class Build
    {
        void StandardUnitTests( string configuration, IEnumerable<SolutionProject> testProjects )
        {
            var testDlls = testProjects.Select( p =>
                         new
                         {
                             ProjectPath = p.Path.GetDirectory(),
                             NetCoreAppDll = p.Path.GetDirectory().CombineWithFilePath( "bin/" + configuration + "/netcoreapp2.0/" + p.Name + ".dll" ),
                             Net461Dll = p.Path.GetDirectory().CombineWithFilePath( "bin/" + configuration + "/net461/" + p.Name + ".dll" ),
                             Net461Exe = p.Path.GetDirectory().CombineWithFilePath( "bin/" + configuration + "/net461/" + p.Name + ".exe" ),
                         } );

            foreach( var test in testDlls )
            {
                var net461 = Cake.FileExists( test.Net461Dll )
                                ? test.Net461Dll
                                : Cake.FileExists( test.Net461Exe )
                                    ? test.Net461Exe
                                    : null;
                if( net461 != null )
                {
                    Cake.Information( "Testing: {0}", net461 );
                    Cake.NUnit( new[] { net461 }, new NUnitSettings()
                    {
                        Framework = "v4.5",
                        ResultsFile = test.ProjectPath.CombineWithFilePath( "TestResult.Net461.xml" )
                    } );
                }
                var nUnitLite = Cake.FileExists( test.NetCoreAppDll )
                                    ? test.NetCoreAppDll
                                    : null;

                if( nUnitLite != null )
                {
                    Cake.Information( "Testing: {0}", nUnitLite );
                    Cake.DotNetCoreExecute( nUnitLite );
                }
            }

        }

    }
}
