# CK-ActivityMonitor

[![AppVeyor](https://img.shields.io/appveyor/ci/olivier-spinelli/ck-activitymonitor.svg)](https://ci.appveyor.com/project/olivier-spinelli/ck-activitymonitor)
[![Nuget](https://img.shields.io/nuget/vpre/CK.ActivityMonitor.svg)](https://www.nuget.org/packages/CK.ActivityMonitor/)
[![Licence](https://img.shields.io/github/license/Invenietis/CK-ActivityMonitor.svg)](https://github.com/Invenietis/CK-ActivityMonitor/blob/develop/LICENSE)

This repository contains `IActivityMonitor` defifinition and its primary implementation along with helpers.  
See [CK-Monitoring](https://github.com/Invenietis/CK-Monitoring) for integration and use as a logging solution in .Net projects.

---
**Note**

The ActivityMonitor is a different logger. The original motivation (back in... 2004) for that "logger" was to support structured logs ("structured" not in the sense of [SeriLog](https://serilog.net/), but structured as a book can be, with Sections, Parts, Chapters, Paragraphs etc.).
This is an opinionated framework: one strong belief is that **logging is NOT a "cross-cutting concern"** (I know how much this could hurt a lot of architects). Logging is the developer's voice, designing logs is an important mission of the developer: logs
must describe the program *by* its execution, they tell the story of the running code, they play a crucial role
in the maintenance, exploitation and evolution phase of any serious project.

The ActivityMonitor is more a **Storyteller**, than a regular Logger.

We believe that more and more architectures, tools, programs will take this path because it's one of the mean to handle high complexity.
MSBuild has this https://msbuildlog.com/, CI/CD interfaces starts to dislay toggled section around the execution steps, etc.

---

## Quick start

### Emitting logs

The `IActivityMonitor` is not a singleton, each ActivityMonitor instance must follow the execution path. It can be a Scoped 
dependency for root objects, and for the vast majority of interactions, it appears as an explicit method parameter.
Cumbersome? Not that much actually but clear, explicit and bug-free.

Install the [CK.ActivityMonitor.SimpleSender](#SimpleSender) NuGet package, create a new `CK.Core.ActivityMonitor` and starts sending logs
thanks to all the extension methods that help to:
- Send a line with a given [LogLevel](CK.ActivityMonitor/LogLevel.cs): Debug, Trace, Info, Warn, Error, Fatal.
- Opens a group of logs (see all the available overloads [here](CK.ActivityMonitor.SimpleSender/ActivityMonitorSimpleSenderExtension.Group.cs) and [here](CK.ActivityMonitor.SimpleSender/ActivityMonitorSimpleSenderExtension.Group-Gen.cs)).

```csharp
using System.IO;
using CK.Core;

public class Program
{
    public static void Main()
    {
        // An ActivityMonitor is a lightweight object that is tied to non concurrent
        // (sequential) set of calls (this perfectly complies with async/await calls).
        var m = new ActivityMonitor();
        int onError = 0, onSuccess = 0;
        foreach( var f in Directory.GetFiles( Environment.CurrentDirectory ) )
        {
            using( m.OpenTrace( $"Processing file '{f}'." ) )
            {
                try
                {
                    if( ProcessFile( m, f ) )
                    {
                        ++onSuccess;
                    }
                    else
                    {
                        ++onError;
                    }
                }
                catch( Exception ex )
                {
                    m.Error( $"Unhandled error while processing file '{f}'. Continuing.", ex );
                    ++onError;
                }
            }
        }
        m.Info( $"Done: {onSuccess} files succeed and {onError} failed." );
    }

    /// When consuming a monitor, we always use the IActivityMonitor interface.
    static bool ProcessFile( IActivityMonitor m, string f )
    {
        int ticks = Environment.TickCount;
        m.Debug( $"Ticks: {ticks} for '{f}'." );
        /// Quick and dirty way to return a (not really) random boolean.
        return ticks % 2 == 0;
    }
}
```

A monitor has a Topic that aims to describes what it is OR what it is currently doing. The constructor can initialize it `m = new ActivityMonitor("My topic");`
and it can be changed by calling the `SetTopic( message )` method at any time.

The topic is merely a log line with a special tag, sent when constructing the monitor or changing it.

### Consuming logs

Logs received by the `IActivityMonitor` façade are routed to its clients (see [Clients](CK.ActivityMonitor/Client) for a basic console output sample).

In practice, more powerful logs management that this simple direct clients is required and we use the packages from
[CK-Monitoring](https://github.com/Invenietis/CK-Monitoring) repository (that implements the `GrandOutput` central collector) and, for tests,
the [CK.Testing.Monitoring](https://github.com/Invenietis/CK-Testing/tree/master/CK.Testing.Monitoring) package that adds a Monitor property on the **TestHelper**
mix-in: it's easy to use `TestHelper.Monitor` from any tests.

## Content projects

### CK.ActivityMonitor [![Nuget](https://img.shields.io/nuget/vpre/CK.ActivityMonitor.svg)](https://www.nuget.org/packages/CK.ActivityMonitor/)

The core abstractions, and default implementation of `ActivityMonitor`. Also contains:
- Standard but basic [Clients](CK.ActivityMonitor/Client). 
- The LogFile static class that exposes the `RootLogPath` property.
- The [EventMonitoredArgs](CK.ActivityMonitor/EventMonitoredArgs.cs) that is an EventArgs with a monitor.
- The [AsyncLock](CK.ActivityMonitor/AsyncLock.md) that can detect, handles or reject asynchronous lock reentrancy without any awful [AsyncLocal](https://docs.microsoft.com/en-us/dotnet/api/system.threading.asynclocal-1) 
thanks to the `IActivityMonitor` ubiquitous parameter. 

### <a name="SimpleSender"></a>CK.ActivityMonitor.SimpleSender [![Nuget](https://img.shields.io/nuget/vpre/CK.ActivityMonitor.SimpleSender.svg)](https://www.nuget.org/packages/CK.ActivityMonitor.SimpleSender/)

Contains the simple logging extension methods on `IActivityMonitor`:

```csharp
using CK.Core;
public class MyClass
{
    public void MyMethod()
    {
        IActivityMonitor m = new ActivityMonitor();

        using( m.OpenInfo("My group") )
        {
            m.Debug( "My Debug log line" );
            m.Trace( "My Trace log line" );
            m.Info( "My Info log line" );
            m.Warn( "My Warn log line" );
            m.Error( "My Error log line" );
            m.Fatal( "My Fatal log line" );
        }
    }
}
```
Before this simple sender, a less intuitive set of extension methods exist: the "standard" ones that rely on a
two-steps approach. Even if we maintain this package, we recommend to use the Simple one rather that the Standard one.

> **CK.ActivityMonitor.StandardSender** [![Nuget](https://img.shields.io/nuget/vpre/CK.ActivityMonitor.StandardSender.svg)](https://www.nuget.org/packages/CK.ActivityMonitor.StandardSender/)
> 
> Contains the two-steps logging extension methods on `IActivityMonitor`:
> ```csharp
> using CK.Core;
> public class MyClass
> { 
>   public void MyMethod()
>   {
>       IActivityMonitor m = new ActivityMonitor();
> 
>       using( m.OpenInfo().Send("My group") )
>       {
>           m.Debug().Send("My Debug log line");
>           m.Trace().Send("My Trace log line");
>           m.Info().Send("My Info log line");
>           m.Warn().Send("My Warn log line");
>           m.Error().Send("My Error log line");
>           m.Fatal().Send("My Fatal log line");
>       }
>   }
> }
> ```

 
### CK.PerfectEvent [![Nuget](https://img.shields.io/nuget/vpre/CK.PerfectEvent.svg)](https://www.nuget.org/packages/CK.PerfectEvent/)

A simple implementation of an asynchronous .Net events that handles synchronous, sequential asynchronous and parallel asynchronous callbacks.

## Bug tracker

If you find any bug, don't hesitate to report it on [https://github.com/Invenietis/CK-ActivityMonitor/issues/](https://github.com/Invenietis/CK-ActivityMonitor/issues/)

## Copyright and license

This solution is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.
<http://www.gnu.org/licenses/>.

Copyright © 2007-2021 Signature-Code <http://www.signature-code.com> All rights reserved.
