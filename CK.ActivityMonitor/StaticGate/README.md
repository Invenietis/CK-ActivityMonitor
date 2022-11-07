# StaticGates

A [StaticGate](StaticGate.cs) is a simple object that encapsulates a boolean: it can be opened or closed.

This is, we think, the most possible efficient implementation to enable/disable logging since it relies on the
[null-conditional operator](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/member-access-operators#null-conditional-operators--and-)
to return a `IActivityMonitor` or null (actually any type of object can be used through a gate).

> This is a trick... But optimal because every log creation code is skipped (there is absolutely no method calls when a 
> gate is closed) and thanks to the Nullable Reference warnings mostly safe to use.

## A StaticGate lives forever, it should be a static object

To create a gate, simply instantiate it and stores the reference in a static readonly field.
This field can be safely exposed publicly, here is the gate definition of the [AsyncLock](../AsyncLock.md):

```csharp
public sealed class AsyncLock
{
    /// <summary>
    /// The gate that controls logging for AsyncLock. Can be reused by other async related
    /// features. It is closed by default.
    /// </summary>
    public static readonly StaticGate Gate = new StaticGate( nameof(AsyncLock), false );

    ...
}
```

The name (here "AsyncLock") is a DisplayName, it doesn't have to be unique. The constructor captures the file name and line number
of the instantiation however even this is not used as an identity. The true identity of a StaticGate is provided
by the [CoreApplicationIdentity.InstanceId](https://github.com/Invenietis/CK-Core/blob/master/CK.Core/CoreApplicationIdentity/README.md)
of the running application and the `int Key { get; }` (an incremented index for each gate created). 

## StaticGate opening and closing

Open/Close status is exposed by a simple writable property `bool IsOpen {get; set;}`.
For remote control of gates, the static `Open` method should be used:
```csharp
/// <summary>
/// Sets the <see cref="IsOpen"/> property of a gate by its key, ensuring that
/// <see cref="CoreApplicationIdentity.InstanceId"/> is known by the caller.
/// </summary>
/// <param name="key"></param>
/// <param name="instanceId">Must be <see cref="CoreApplicationIdentity.InstanceId"/>, otherwise nothing is done.</param>
/// <param name="open">True to open, false to close.</param>
/// <returns>True if the operation has been applied, false otherwise.</returns>
public static bool Open( int key, string instanceId, bool open )
```
This guaranties that the caller knows what he's doing: he must have obtained the instance identifier of the application and
have called `static IEnumerable<StaticGate> GetStaticGates()` to choose the relevant gate thanks to its properties
(`DisplayName`, `FilePath` and `LineNumber`).

## Usage: the `O` method and StaticLogger property

Using a gate is rather easy:
```csharp
Gate.O(monitor)?.Info( "I'll be emitted only if the Gate is opened." );
```

> This `T? O<T>( T instance )` method can be called **with any reference type**, not necessarily
a `IActivityMonitor`.

Another capability of a gate is to handle access to the [StaticLogger](../../README.md#emitting-logs-the-ilogger-static-contextless-way):
```csharp
Gate.StaticLogger?.Error( $"I'll be emitted only if the Gate is opened." );
```

## Where should StaticGate be used?
StaticGate should be used in low level code, in hot paths and when the feature is tied to
a well identified object or part of code (the AsyncLock is good example).

StaticGate should not be used in applicative layer, where activities flow across multiple
layers of code and the context of the callee is highly relevant: [Tags](../Impl/TagFiltering.md) are much more powerful.



