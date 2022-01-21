using System;
using System.Collections.Generic;

namespace CK.Core
{
    /// <summary>
    /// Offers <see cref="IActivityMonitorClient"/> registration/unregistration and exposes a <see cref="BridgeTarget"/> 
    /// (an <see cref="ActivityMonitorBridgeTarget"/>) that can be used to accept logs from other monitors.
    /// </summary>
    public interface IActivityMonitorOutput
    {
        /// <summary>
        /// Registers an <see cref="IActivityMonitorClient"/> to the <see cref="Clients"/> list.
        /// Duplicate IActivityMonitorClient instances are ignored.
        /// </summary>
        /// <param name="client">An <see cref="IActivityMonitorClient"/> implementation.</param>
        /// <param name="added">True if the client has been added, false if it was already registered.</param>
        /// <returns>The registered client.</returns>
        IActivityMonitorClient RegisterClient( IActivityMonitorClient client, out bool added );

        /// <summary>
        /// Registers a typed <see cref="IActivityMonitorClient"/>.
        /// Duplicate IActivityMonitorClient instances are ignored.
        /// </summary>
        /// <typeparam name="T">Any type that specializes <see cref="IActivityMonitorClient"/>.</typeparam>
        /// <param name="client">Client to register.</param>
        /// <param name="added">True if the client has been added, false if it was already registered.</param>
        /// <returns>The registered client.</returns>
        T RegisterClient<T>( T client, out bool added ) where T : IActivityMonitorClient;

        /// <summary>
        /// Unregisters the given <see cref="IActivityMonitorClient"/> from the <see cref="Clients"/> list.
        /// Silently ignores an unregistered client.
        /// </summary>
        /// <param name="client">An <see cref="IActivityMonitorClient"/> implementation.</param>
        /// <returns>The unregistered client or null if it has not been found.</returns>
        IActivityMonitorClient? UnregisterClient( IActivityMonitorClient client );

        /// <summary>
        /// Registers a <see cref="IActivityMonitorClient"/> that must be unique in a sense.
        /// </summary>
        /// <param name="tester">Predicate that checks for an already registered client.</param>
        /// <param name="factory">Factory that will be called if no existing client satisfies <paramref name="tester"/>.</param>
        /// <returns>The existing or newly created client or null if the factory returned null.</returns>
        /// <remarks>
        /// The factory function MUST return null OR a client that satisfies the tester function otherwise a <see cref="InvalidOperationException"/> is thrown.
        /// When null is returned by the factory function, nothing is added and null is returned. 
        /// The factory is called only when no client satisfy the tester function: this makes the 'added' out parameter useless.
        /// </remarks>
        T? RegisterUniqueClient<T>( Func<T, bool> tester, Func<T?> factory ) where T : IActivityMonitorClient;

        /// <summary>
        /// Gets the list of registered <see cref="IActivityMonitorClient"/>.
        /// </summary>
        IReadOnlyList<IActivityMonitorClient> Clients { get; }

    }

}
