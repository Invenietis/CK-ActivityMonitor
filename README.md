# CK-ActivityMonitor

[![AppVeyor](https://img.shields.io/appveyor/ci/olivier-spinelli/ck-activitymonitor.svg)](https://ci.appveyor.com/project/olivier-spinelli/ck-activitymonitor)
[![Nuget](https://img.shields.io/nuget/vpre/CK.ActivityMonitor.svg)](https://www.nuget.org/packages/CK.ActivityMonitor/)
[![Licence](https://img.shields.io/github/license/Invenietis/CK-ActivityMonitor.svg)](https://github.com/Invenietis/CK-ActivityMonitor/blob/develop/LICENSE)

This repository contains `IActivityMonitor` definition and its primary implementation along with helpers.  
See [CK-Monitoring](https://github.com/Invenietis/CK-Monitoring) for integration and use as a logging solution in .Net projects.

---
**Note**

The ActivityMonitor is a different logger. The original motivation (back in... 2004) for that "logger" was to support structured logs ("structured" not in the sense of [SeriLog](https://serilog.net/), but structured as a book can be, with Sections, Parts, Chapters, Paragraphs etc.).
This is an opinionated framework: one strong belief is that **logging is NOT a "cross-cutting concern"** (I know how much this could hurt a lot of architects). Logging is the developer's voice, designing logs is an important mission of the developer: logs
must describe the program *by* its execution, they tell the story of the running code, they play a crucial role
in the maintenance, exploitation and evolution phase of any serious project.

The ActivityMonitor is more a **Storyteller**, than a regular Logger.

We believe that more and more architectures, tools, programs will take this path because it's one of the mean to handle high complexity.
MSBuild has this https://msbuildlog.com/, CI/CD interfaces starts to display toggled section around the execution steps, etc.

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

#### Emitting logs the `ILogger` (static, contextless) way

When no `IActivityMonitor` exists in a given context, there are 2 possibilities:
- Create a `var monitor = new ActivityMonitor();` and use it. There is nothing to dispose (but if your code can know where a monitor should not be 
used anymore, calling `monitor.MonitorEnd()` is welcome).
- If there is only one (or very few) things to log, then you can use the [`ActivityMonitor.StaticLogger`](CK.ActivityMonitor/Impl/ActivityMonitor.StaticLogger.cs) 
simple static API. Such log events are not tied to a monitor, their monitor identifier will be "§ext" and they are
collectible by any external components: the CK.Monitoring.GrandOuput will catch and collect them.

The `StaticLogger` should be used in very specific cases, in low level zone of code that are not
yet "monitored" such as callbacks from timers for instance:

```csharp
  void OnTimer( object? _ )
  {
      ActivityMonitor.StaticLogger.Debug( IDeviceHost.DeviceModel, $"Timer fired for '{FullName}'." );
      Volatile.Write( ref _timerFired, true );
      _commandQueue.Writer.TryWrite( _commandAwaker );
  }
```

Of course, there is no `OpenGroup` on this API since open/close would interleave without any clue of which Close
relates to which Open.

### Consuming logs

Logs received by the `IActivityMonitor` façade are routed to its clients (see [Clients](CK.ActivityMonitor/Client) for a basic console output sample).

In practice, more powerful logs management than this simple direct clients is required and we use the packages from
[CK-Monitoring](https://github.com/Invenietis/CK-Monitoring) repository (that implements the `GrandOutput` central collector) and, for tests,
the [CK.Testing.Monitoring](https://github.com/Invenietis/CK-Testing/tree/master/CK.Testing.Monitoring) package that adds a Monitor property on the **TestHelper**
mix-in: it's easy to use `TestHelper.Monitor` from any tests.

### Using monitor to ease tests writing
The local client architecture of this logger enables an interesting pattern for tests.
The following test uses events from CK.PerfectEvent, it creates an independent monitor but may also
use the `TestHelper.Monitor` from CK.Testing.Monitoring).

```csharp
[Test]
public async Task demo_using_CollectTexts_Async()
{
    var monitor = new ActivityMonitor();
            
    var sender = new PerfectEventSender<Action<IActivityMonitor,int>?>();

    sender.PerfectEvent.Async += OnActionAsync;

    using( monitor.CollectTexts( out var texts ) )
    {
        await sender.RaiseAsync( monitor, (monitor,i) => monitor.Info( $"Action {i}" ) );
        await sender.RaiseAsync( monitor, null );
        texts.Should().BeEquivalentTo( new[]
        {
            "Received Action and executing it after a 100 ms delay.",
            "Action 3712",
            "Received a null Action. Ignoring it."
        } );
    }

    static async Task OnActionAsync( IActivityMonitor monitor, Action<IActivityMonitor,int>? a )
    {
        if( a == null ) monitor.Warn( "Received a null Action. Ignoring it." );
        else
        {
          monitor.Info( "Received Action and executing it after a 100 ms delay." );
          await Task.Delay( 100 );
          a( monitor, 3712 );
        }
    }
}
```

## Content projects

### CK.ActivityMonitor [![Nuget](https://img.shields.io/nuget/vpre/CK.ActivityMonitor.svg)](https://www.nuget.org/packages/CK.ActivityMonitor/)

The core abstractions, and default implementation of `ActivityMonitor`. Also contains:
- Standard but basic [Clients](CK.ActivityMonitor/Client). 
- The LogFile static class that exposes the `RootLogPath` property.
- The [EventMonitoredArgs](CK.ActivityMonitor/EventMonitoredArgs.cs) that is an EventArgs with a monitor.
- The [AsyncLock](CK.ActivityMonitor/AsyncLock.md) that can detect, handles or reject asynchronous lock reentrancy 
without any awful [AsyncLocal](https://docs.microsoft.com/en-us/dotnet/api/system.threading.asynclocal-1) 
thanks to the `IActivityMonitor` ubiquitous parameter. 
- The [LogGate](CK.ActivityMonitor/LogGates/README.md) that can optimally control log emission.

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
two-steps approach. This package is now totally deprecated since thanks to the C# 10 [interpolated handlers](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-10.0/improved-interpolated-strings#the-handler-pattern),
the .NET 6 simple sender can now skip the evaluation of the interpolated message based on the log Tags.
This is described here: [CK.ActivityMonitor/Impl/TagFiltering](CK.ActivityMonitor/Impl/TagFiltering.md).
 
## Bug tracker

If you find any bug, don't hesitate to report it on [https://github.com/Invenietis/CK-ActivityMonitor/issues/](https://github.com/Invenietis/CK-ActivityMonitor/issues/)

## Copyright and license

This solution is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.
<http://www.gnu.org/licenses/>.

Copyright © 2007-2021 Signature-Code <http://www.signature-code.com> All rights reserved.
