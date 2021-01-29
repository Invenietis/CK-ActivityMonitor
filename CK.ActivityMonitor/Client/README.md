# ActivityMonitor clients

The `IActivityMonitor` is the collector of the logs that
are routed to any number of clients that can be registered onto its [Output](../IActivityMonitorOutput.cs).

The design of the Output and its Clients allows very different kind of
clients to coexist, that can support funny patterns like counting errors that may
occur anywhere in subordinated calls:

```csharp
int errorCount = 0;
using( monitor.OnError( () => ++errorCount ) )
{
    monitor.Info( "This is not an error." );
    await SafeCodeAsync( monitor );
    monitor.Error( "Ouch! (I'm the only error)." );
}
errorCount.Should().Be( 1 );

```

This one is the [ActivityMonitorErrorCounter](ActivityMonitorErrorCounter.cs) client.

Other clients like the [ColoredActivityMonitorConsoleClient](ColoredActivityMonitorConsoleClient.cs) routes
and/or displays the received stream of logs.

