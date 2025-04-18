namespace CK.Core.Impl;

/// <summary>
/// Defines required aspects that an actual monitor implementation must support.
/// This interface is available to any <see cref="IActivityMonitorBoundClient"/>.
/// </summary>
public interface IActivityMonitorImpl
{
    /// <summary>
    /// Gets a monitor that can be used
    /// from inside clients methods (like <see cref="IActivityMonitorClient.OnUnfilteredLog"/>)
    /// logs will be replayed automatically after the initial log.
    /// <para>
    /// This monitor has the same <see cref="IActivityMonitor.UniqueId"/> as the main one but it's
    /// a different instance that shares the same <see cref="IActivityMonitor.ParallelLogger"/>.
    /// It should be used only to log errors or warnings (including groups) if needed but no client should be
    /// added to its <see cref="IActivityMonitor.Output"/>.
    /// </para>
    /// </summary>
    IActivityMonitor InternalMonitor { get; }

    /// <summary>
    /// Signals the monitor that one <see cref="IActivityMonitorBoundClient.IsDead"/> is true or 
    /// a <see cref="IActivityMonitorBoundClient.MinimalFilter"/> has changed: the <see cref="IActivityMonitor.ActualFilter"/> is 
    /// marked as needing a re computation in a thread-safe manner.
    /// This can be called by bound clients on any thread at any time.
    /// </summary>
    void SignalChange();
}
