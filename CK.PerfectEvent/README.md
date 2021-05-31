# Perfect Event

These events mimics standard .Net events but offer the support of asynchronous handlers.

> Be sure to understand the [standard .Net event pattern](https://docs.microsoft.com/en-us/dotnet/csharp/event-pattern) 
> before reading this.

Perfect events come in two flavors:
 - Events with a single event argument `PerfectEvent<TEvent>` that accepts the following callback signature:
 ```csharp
public delegate void SequentialEventHandler<TEvent>( IActivityMonitor monitor, TEvent e );
```
 - Events with the sender and event argument (like the .Net standard one) `PerfectEvent<TSender, TArg>`:
```csharp
public delegate void SequentialEventHandler<TSender, TArg>( IActivityMonitor monitor, TSender sender, TArg e );
```

As it appears in the signatures above, a monitor is provided: the callee uses it so that its own actions naturally belong to
the calling activity.

## Subscribing and unsubscribing to a Perfect Event

### Synchronous handlers
Perfect events looks like regular events and support `+=` and `-=` operators. Given the fact that things can talk:
```csharp
public interface IThing
{
  string Name { get; }
  PerfectEvent<string> Talk { get; }
}
```
This is a typical listener code:
```csharp
void ListenTo( IThing o )
{
    o.Talk.Sync += ThingTalked;
}

void StopListeningTo( IThing o )
{
    o.Talk.Sync -= ThingTalked;
}

void ThingTalked( IActivityMonitor monitor, string e )
{
    monitor.Info( $"A thing said: '{e}'." );
}
```
### Asynchronous handlers

Let's say that this very first version is not enough:
 - We now want to know who is talking: we'll use the `PerfectEvent<TSender, TArg>` event that includes the sender.
The `IThing` definition becomes:
```csharp
public interface IThing
{
  string Name { get; }
  PerfectEvent<IThing,string> Talk { get; }
}
```
 - We want to persist the talk in a database: it's better to use an asynchronous API to interact with the database.
The listener becomes:
```csharp
void ListenTo( IThing o )
{
    o.Talk.Async += ThingTalkedAsync;
}

void StopListeningTo( IThing o )
{
    o.Talk.Async -= ThingTalkedAsync;
}

async Task ThingTalkedAsync( IActivityMonitor monitor, IThing thing, string e )
{
    monitor.Info( $"Thing {thing.Name} said: '{e}'." );
    await _database.RecordAsync( monitor, thing.Name, e );
}
```

### Parallel handlers

Parallel handlers are a little bit more complex to implement and also more dangerous: concurrency must be handled carefully.
The parallel handlers is not called with the origin monitor but with a `ActivityMonitor.DependentToken` that is a correlation
identifier (actually a string that identifies its creation instant):

```csharp
void ListenTo( IThing o )
{
    o.Talk.ParallelAsync += ThingTalkedAsync;
}

void StopListeningTo( IThing o )
{
    o.Talk.ParallelAsync -= ThingTalkedAsync;
}

async Task ThingTalkedAsync( ActivityMonitor.DependentToken token, IThing thing, string e )
{
    var monitor = new ActivityMonitor();
    monitor.DependentActivity().Launch( token );
    //...
    monitor.MonitorEnd();
}
```

## Implementing and raising a Perfect Event

A Perfect Event is implemented thanks to a [PerfectEventSender](PerfectEventSender.cs). 

```csharp
class Thing : IThing
{
    // The sender must NOT be exposed: its PerfectEvent property is the external API. 
    readonly PerfectEventSender<IThing, string> _talk;

    public Thing( string name )
    {
        Name = name;
        _talk = new PerfectEventSender<IThing, string>();
    }

    public string Name { get; }

    public PerfectEvent<IThing, string> Talk => _talk.PerfectEvent;

    internal Task SaySomething( IActivityMonitor monitor, string something ) => _talk.RaiseAsync( monitor, this, something );
}
```

Calling `RaiseAsync` calls all the subscribed handlers and if any of them throws an exception, it is propagated to the caller. Sometimes,
we want to isolate the caller from any error in the handlers (handlers are "client code", they can be buggy). `SafeRaiseAsync` protects th calls:

```csharp
/// <summary>
/// Same as <see cref="RaiseAsync"/> except that if exceptions occurred they are caught and logged
/// and a gentle false is returned.
/// <para>
/// The returned task is resolved once the parallels, the synchronous and the asynchronous event handlers have finished their jobs.
/// </para>
/// <para>
/// If exceptions occurred, they are logged and false is returned.
/// </para>
/// </summary>
/// <param name="monitor">The monitor to use.</param>
/// <param name="sender">The sender of the event.</param>
/// <param name="e">The argument of the event.</param>
/// <param name="fileName">The source filename where this event is raised.</param>
/// <param name="lineNumber">The source line number in the filename where this event is raised.</param>
/// <returns>True on success, false if an exception occurred.</returns>
public async Task<bool> SafeRaiseAsync( IActivityMonitor monitor, TSender sender, TArg e, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0 )
```

That's pretty much everything that needs to be said about Perfect Events.



