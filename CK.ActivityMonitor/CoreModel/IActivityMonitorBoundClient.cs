using CK.Core.Impl;

namespace CK.Core
{
    /// <summary>
    /// Specialized <see cref="IActivityMonitorClient"/> that is bound to one <see cref="IActivityMonitor"/>.
    /// Clients that can not be registered into multiple outputs (and receive logs from multiple monitors at
    /// the same time) should implement this interface in order to control their registration/un-registration.
    /// </summary>
    public interface IActivityMonitorBoundClient : IActivityMonitorClient
    {
        /// <summary>
        /// Gets the minimal log level that this Client expects. 
        /// Should default to <see cref="LogFilter.Undefined"/> if this client has no
        /// filtering requirements.
        /// </summary>
        LogFilter MinimalFilter { get; }

        /// <summary>
        /// Gets whether this client is dead: it should be removed from the source activity monitor's clients.
        /// It should obviously defaults to false (and once true should remain true).
        /// Implementations should call <see cref="IActivityMonitorImpl.SignalChange"/> on its current source
        /// to trigger the removal.
        /// </summary>
        bool IsDead { get; }

        /// <summary>
        /// Called by <see cref="IActivityMonitorOutput"/> when registering or unregistering
        /// this client.
        /// </summary>
        /// <param name="source">The monitor that will send log.</param>
        /// <param name="forceBuggyRemove">
        /// True if this client must be removed because one of its method thrown an exception.
        /// The <paramref name="source"/> is necessarily null and a client has no way to
        /// prevent the removal.
        /// </param>
        void SetMonitor( IActivityMonitorImpl? source, bool forceBuggyRemove );
    }
}
