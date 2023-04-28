using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.Core
{
    /// <summary>
    /// Offers <see cref="IActivityMonitorClient"/> registration/unregistration.
    /// </summary>
    public interface IActivityMonitorOutput
    {
        /// <summary>
        /// Registers an <see cref="IActivityMonitorClient"/> to the <see cref="Clients"/> list.
        /// Duplicate IActivityMonitorClient instances are ignored.
        /// </summary>
        /// <param name="client">An <see cref="IActivityMonitorClient"/> implementation.</param>
        /// <param name="added">True if the client has been added, false if it was already registered.</param>
        /// <param name="replayInitialLogs">True to immediately replay initial logs if any (see <see cref="ActivityMonitorOptions.WithInitialReplay"/>)</param>
        /// <returns>The registered client.</returns>
        IActivityMonitorClient RegisterClient( IActivityMonitorClient client, out bool added, bool replayInitialLogs = false );

        /// <summary>
        /// Unregisters the given <see cref="IActivityMonitorClient"/> from the <see cref="Clients"/> list.
        /// Silently ignores an unregistered client.
        /// </summary>
        /// <param name="client">An <see cref="IActivityMonitorClient"/> implementation.</param>
        /// <returns>The unregistered client or null if it has not been found.</returns>
        IActivityMonitorClient? UnregisterClient( IActivityMonitorClient client );

        /// <summary>
        /// Unregisters the first <see cref="IActivityMonitorClient"/> from the <see cref="IActivityMonitorOutput.Clients"/> list
        /// that satisfies the predicate.
        /// </summary>
        /// <param name="predicate">A predicate that will be used to determine the first client to unregister.</param>
        /// <returns>The unregistered client, or null if no client has been found.</returns>
        T? UnregisterClient<T>( Func<T, bool> predicate ) where T : IActivityMonitorClient;

        /// <summary>
        /// Registers a <see cref="IActivityMonitorClient"/> that must be unique in a sense.
        /// </summary>
        /// <param name="tester">Predicate that checks for an already registered client.</param>
        /// <param name="factory">Factory that will be called if no existing client satisfies <paramref name="tester"/>.</param>
        /// <param name="replayInitialLogs">True to immediately replay initial logs if any (see <see cref="ActivityMonitorOptions.WithInitialReplay"/>)</param>
        /// <returns>The existing or newly created client or null if the factory returned null.</returns>
        /// <remarks>
        /// The factory function MUST return null OR a client that satisfies the tester function otherwise a <see cref="InvalidOperationException"/> is thrown.
        /// When null is returned by the factory function, nothing is added and null is returned. 
        /// The factory is called only when no client satisfy the tester function: this makes the 'added' out parameter useless.
        /// </remarks>
        T? RegisterUniqueClient<T>( Func<T, bool> tester, Func<T?> factory, bool replayInitialLogs = false ) where T : IActivityMonitorClient;

        /// <summary>
        /// Gets a snapshot of the registered <see cref="IActivityMonitorClient"/>.
        /// </summary>
        IActivityMonitorClient[] Clients { get; }

        /// <summary>
        /// Gets or sets the current maximal logs that are initially replayed.
        /// This is null when the monitor has not been instantiated <see cref="ActivityMonitorOptions.WithInitialReplay"/> or
        /// if the initial replay has stopped. Default to 1000.
        /// <para>
        /// To stop the initial replay (when all clients are registered) sets this to null or 0.
        /// </para>
        /// </summary>
        int? MaxInitialReplayCount { get; set; }
    }

}
