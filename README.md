# CK-ActivityMonitor

[![AppVeyor](https://img.shields.io/appveyor/ci/olivier-spinelli/ck-activitymonitor.svg)](https://ci.appveyor.com/project/olivier-spinelli/ck-activitymonitor)
[![Nuget](https://img.shields.io/nuget/vpre/CK.ActivityMonitor.svg)](https://www.nuget.org/packages/CK.ActivityMonitor/)
[![Licence](https://img.shields.io/github/license/Invenietis/CK-ActivityMonitor.svg)](https://github.com/Invenietis/CK-ActivityMonitor/blob/develop/LICENSE)

This repository contains `IActivityMonitor` defifinition and its primary implementation along with helpers.  
See [CK-Monitoring](https://github.com/Invenietis/CK-Monitoring) for integration and use as a logging solution in .Net projects.

## Documentation

The [CK-ActivityMonitor GitHub wiki](https://github.com/Invenietis/CK-ActivityMonitor/wiki) contains documentation on how to [get started](https://github.com/Invenietis/CK-ActivityMonitor/wiki/Getting-started) with CK-ActivityMonitor.

## Content projects

### CK.ActivityMonitor [![Nuget](https://img.shields.io/nuget/vpre/CK.ActivityMonitor.svg)](https://www.nuget.org/packages/CK.ActivityMonitor/)

The core abstractions, and default implementation of `ActivityMonitor`. Also contains:
- Standard [Clients](CK.ActivityMonitor/Clients). 
- The LogFile static class that exposes the `RootLogPath` property.
- The [EventMonitoredArgs](CK.ActivityMonitor/EventMonitoredArgs.cs) that is an EventArgs with a monitor.
- The [AsyncLock](CK.ActivityMonitor/AsyncLock.cs) that can detect, handles or reject asynchronous lock reentrancy. 

### CK.ActivityMonitor.SimpleSender [![Nuget](https://img.shields.io/nuget/vpre/CK.ActivityMonitor.SimpleSender.svg)](https://www.nuget.org/packages/CK.ActivityMonitor.SimpleSender/)

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


### CK.ActivityMonitor.StandardSender [![Nuget](https://img.shields.io/nuget/vpre/CK.ActivityMonitor.StandardSender.svg)](https://www.nuget.org/packages/CK.ActivityMonitor.StandardSender/)

Contains the two-steps logging extension methods on `IActivityMonitor`:

```csharp
using CK.Core;
public class MyClass
{
  public void MyMethod()
  {
      IActivityMonitor m = new ActivityMonitor();

      using( m.OpenInfo().Send("My group") )
      {
          m.Debug().Send("My Debug log line");
          m.Trace().Send("My Trace log line");
          m.Info().Send("My Info log line");
          m.Warn().Send("My Warn log line");
          m.Error().Send("My Error log line");
          m.Fatal().Send("My Fatal log line");
      }
  }
}
```

### CK.PerfectEvent [![Nuget](https://img.shields.io/nuget/vpre/CK.PerfectEvent.svg)](https://www.nuget.org/packages/CK.PerfectEvent/)

A simple implementation of an asynchronous .Net events that handles synchronous, sequential asynchronous and parrallel asynchronous callbacks.

## Bug tracker

If you find any bug, don't hesitate to report it on [https://github.com/Invenietis/CK-ActivityMonitor/issues/](https://github.com/Invenietis/CK-ActivityMonitor/issues/)

## Copyright and license

This solution is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.
<http://www.gnu.org/licenses/>.

Copyright © 2007-2021 Invenietis <http://www.invenietis.com> All rights reserved.
