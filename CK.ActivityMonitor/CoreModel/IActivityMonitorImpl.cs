using System;
using System.Runtime.CompilerServices;

namespace CK.Core.Impl
{
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
}
