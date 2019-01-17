using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Solution;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Build;
using Cake.Common.Tools.DotNetCore.Pack;
using Cake.Common.Tools.DotNetCore.Publish;
using Cake.Common.Tools.DotNetCore.Restore;
using Cake.Common.Tools.DotNetCore.Test;
using Cake.Common.Tools.NuGet;
using Cake.Common.Tools.NuGet.Push;
using Cake.Common.Tools.NUnit;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using CK.Text;
using SimpleGitVersion;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Code.Cake;

namespace CodeCake
{
    /// <summary>
    /// Standard build "script".
    /// </summary>
    [AddPath( "%UserProfile%/.nuget/packages/**/tools*" )]
    public partial class Build : CodeCakeHost
    {
        public Build()
        {
            Cake.Log.Verbosity = Verbosity.Diagnostic;

            const string solutionName = "CK-ActivityMonitor";
            const string solutionFileName = solutionName + ".sln";

            var releasesDir = Cake.Directory( "CodeCakeBuilder/Releases" );

            var projects = Cake.ParseSolution( solutionFileName )
                           .Projects
                           .Where( p => !(p is SolutionFolder)
                                        && p.Name != "CodeCakeBuilder" );

            // We do not publish .Tests projects for this solution.
            var projectsToPublish = projects
                                        .Where( p => !p.Path.Segments.Contains( "Tests" ) );

            // The SimpleRepositoryInfo should be computed once and only once.
            SimpleRepositoryInfo gitInfo = Cake.GetSimpleRepositoryInfo();
            // This default global info will be replaced by Check-Repository task.
            // It is allocated here to ease debugging and/or manual work on complex build script.
            CheckRepositoryInfo globalInfo = new CheckRepositoryInfo( gitInfo, projectsToPublish );

            Task( "Check-Repository" )
                .Does( () =>
                {
                    globalInfo = StandardCheckRepository( projectsToPublish, gitInfo );
                    if( globalInfo.ShouldStop )
                    {
                        Cake.TerminateWithSuccess( "All packages from this commit are already available. Build skipped." );
                    }
                } );

            Task( "Clean" )
                .Does( () =>
                {
                    Cake.CleanDirectories( projects.Select( p => p.Path.GetDirectory().Combine( "bin" ) ) );
                    Cake.CleanDirectories( releasesDir );
                    Cake.DeleteFiles( "Tests/**/TestResult*.xml" );
                } );

            Task( "Build" )
                .IsDependentOn( "Check-Repository" )
                .IsDependentOn( "Clean" )
                .Does( () =>
                {
                    StandardSolutionBuild( solutionFileName, gitInfo, globalInfo.BuildConfiguration );
                } );

            Task( "Unit-Testing" )
                .IsDependentOn( "Build" )
                .WithCriteria( () => Cake.InteractiveMode() == InteractiveMode.NoInteraction
                                     || Cake.ReadInteractiveOption( "RunUnitTests", "Run Unit Tests?", 'Y', 'N' ) == 'Y' )
                .Does( () =>
                {
                    StandardUnitTests( globalInfo, projects.Where( p => p.Name.EndsWith( ".Tests" ) ) );
                } );

            Task( "WeakAssemblyBinding-Test-Net461" )
               .IsDependentOn( "Build" )
               .Does( () =>
               {
                   string binPath = $"Tests/WeakNameConsole/bin/{globalInfo.BuildConfiguration}/net461/";
                   // Replaces CK.Text with its old version v6.0.0 in Net451.
                   System.IO.File.Copy( binPath + "CK.Text.dll", binPath + "CK.Text.dll.backup", true );
                   System.IO.File.Copy( "CodeCakeBuilder/WeakBindingTestSupport/CK.Text.dll.v6.0.0.Net451.bin", binPath + "CK.Text.dll", true );
                   // Replaces CK.Core.dll with its old version v9.0.0 in Net461.
                   System.IO.File.Copy( binPath + "CK.Core.dll", binPath + "CK.Core.dll.backup", true );
                   System.IO.File.Copy( "CodeCakeBuilder/WeakBindingTestSupport/CK.Core.dll.v9.0.0.Net461.bin", binPath + "CK.Core.dll", true );
                   try
                   {
                       var fName = System.IO.Path.GetFullPath( binPath + "WeakNameConsole.exe" );
                       bool foundMonitorTrace = false;
                       int conflictCount = Cake.RunCmd( fName, output =>
                       {
                           foundMonitorTrace |= output.Contains( "From inside WeakNameConsole." );
                       } );
                       if( !foundMonitorTrace ) Cake.TerminateWithError( "'From inside WeakNameConsole.' logged not found in output." );
                       if( conflictCount != 2 ) Cake.TerminateWithError( "Assembly binding failed (Expected CK.Text and CK.Core weak bindings)." );
                   }
                   finally
                   {
                       System.IO.File.Copy( binPath + "CK.Core.dll.backup", binPath + "CK.Core.dll", true );
                       System.IO.File.Copy( binPath + "CK.Text.dll.backup", binPath + "CK.Text.dll", true );
                   }
               } );

            Task( "WeakAssemblyBinding-Test-NetCore" )
               .IsDependentOn( "Build" )
               .Does( () =>
               {
                   var config = new DotNetCorePublishSettings().AddVersionArguments( gitInfo, c =>
                   {
                       c.Configuration = globalInfo.BuildConfiguration;
                       c.Framework = "netcoreapp2.1";
                   } );
                   Cake.DotNetCorePublish( "Tests/WeakNameConsole/WeakNameConsole.csproj", config );

                   string binPath = $"Tests/WeakNameConsole/bin/{globalInfo.BuildConfiguration}/netcoreapp2.1/publish/";
                   // Replaces CK.Text with its old version v6.0.0 in Net451.
                   System.IO.File.Copy( binPath + "CK.Text.dll", binPath + "CK.Text.dll.backup", true );
                   System.IO.File.Copy( "CodeCakeBuilder/WeakBindingTestSupport/CK.Text.dll.v6.0.0.netstandard1.3.bin", binPath + "CK.Text.dll", true );
                   // Replaces CK.Core.dll with its old version v9.0.0 in Net461.
                   System.IO.File.Copy( binPath + "CK.Core.dll", binPath + "CK.Core.dll.backup", true );
                   System.IO.File.Copy( "CodeCakeBuilder/WeakBindingTestSupport/CK.Core.dll.v9.0.0.netstandard2.0.bin", binPath + "CK.Core.dll", true );
                   try
                   {
                       var fName = System.IO.Path.GetFullPath( binPath + "WeakNameConsole.dll" );
                       bool foundMonitorTrace = false;
                       int conflictCount = Cake.RunCmd( $"dotnet \"{fName}\"", output =>
                       {
                           foundMonitorTrace |= output.Contains( "From inside WeakNameConsole." );
                       } );
                       if( !foundMonitorTrace ) Cake.TerminateWithError( "'From inside WeakNameConsole.' logged not found in output." );
                       if( conflictCount == 0 ) Cake.Warning( "At least one Assembly binding conflict should have been detected (Expected CK.Text or CK.Core weak bindings)." );
                       if( conflictCount == 1 ) Cake.Warning( "Only one Assembly binding conflict detected (should be CK.Text and CK.Core)." );
                   }
                   finally
                   {
                       System.IO.File.Copy( binPath + "CK.Core.dll.backup", binPath + "CK.Core.dll", true );
                       System.IO.File.Copy( binPath + "CK.Text.dll.backup", binPath + "CK.Text.dll", true );
                   }
               } );

            Task( "WeakAssemblyBinding-Test" )
               .IsDependentOn( "WeakAssemblyBinding-Test-Net461" )
               .IsDependentOn( "WeakAssemblyBinding-Test-NetCore" );

            Task( "Create-NuGet-Packages" )
                .WithCriteria( () => gitInfo.IsValid )
                .IsDependentOn( "WeakAssemblyBinding-Test" )
                .IsDependentOn( "Unit-Testing" )
                .Does( () =>
                {
                    StandardCreateNuGetPackages( releasesDir, projectsToPublish, gitInfo, globalInfo.BuildConfiguration );
                } );

            Task( "Push-NuGet-Packages" )
                .WithCriteria( () => gitInfo.IsValid )
                .IsDependentOn( "Create-NuGet-Packages" )
                .Does( () =>
                {
                    StandardPushNuGetPackages( globalInfo, releasesDir );
                } );

            // The Default task for this script can be set here.
            Task( "Default" )
                .IsDependentOn( "Push-NuGet-Packages" );

        }
    }
}
