#region LGPL License
/*----------------------------------------------------------------------------
* This file (CK.Core\ActivityMonitor\Impl\ActivityMonitorOutput.cs) is part of CiviKey. 
*  
* CiviKey is free software: you can redistribute it and/or modify 
* it under the terms of the GNU Lesser General Public License as published 
* by the Free Software Foundation, either version 3 of the License, or 
* (at your option) any later version. 
*  
* CiviKey is distributed in the hope that it will be useful, 
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
* GNU Lesser General Public License for more details. 
* You should have received a copy of the GNU Lesser General Public License 
* along with CiviKey.  If not, see <http://www.gnu.org/licenses/>. 
*  
* Copyright © 2007-2015, 
*     Invenietis <http://www.invenietis.com>,
*     In’Tech INFO <http://www.intechinfo.fr>,
* All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CK.Core.Impl
{
    /// <summary>
    /// Implementation of <see cref="IActivityMonitorOutput"/> for <see cref="IActivityMonitor.Output"/>.
    /// </summary>
    public sealed class ActivityMonitorOutput : IActivityMonitorOutput
    {
        readonly List<IActivityMonitorClient> _clients;
        readonly IActivityMonitorImpl _monitor;
        readonly ActivityMonitorBridgeTarget _externalInput;

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitorOutput"/> bound to a <see cref="IActivityMonitor"/>.
        /// </summary>
        /// <param name="monitor"></param>
        public ActivityMonitorOutput( IActivityMonitorImpl monitor )
        {
            if( monitor == null ) throw new ArgumentNullException();
            _monitor = monitor;
            _clients = new List<IActivityMonitorClient>();
            _externalInput = new ActivityMonitorBridgeTarget( monitor, true );
        }

        /// <summary>
        /// Gets an entry point for other monitors: by registering <see cref="ActivityMonitorBridge"/> in other <see cref="IActivityMonitor.Output"/>
        /// bound to this <see cref="ActivityMonitorBridgeTarget"/>, log streams can easily be merged.
        /// </summary>
        public ActivityMonitorBridgeTarget BridgeTarget => _externalInput; 

        /// <summary>
        /// Registers an <see cref="IActivityMonitorClient"/> to the <see cref="Clients"/> list.
        /// Duplicate IActivityMonitorClient are silently ignored.
        /// </summary>
        /// <param name="client">An <see cref="IActivityMonitorClient"/> implementation.</param>
        /// <param name="added">True if the client has been added, false if it was already registered.</param>
        /// <returns>The registered client.</returns>
        public IActivityMonitorClient RegisterClient( IActivityMonitorClient client, out bool added )
        {
            if( client == null ) throw new ArgumentNullException( "client" );
            using( _monitor.ReentrancyAndConcurrencyLock() )
            {
                added = false;
                return DoRegisterClient( client, ref added );
            }
        }

        IActivityMonitorClient DoRegisterClient( IActivityMonitorClient client, ref bool forceAdded )
        {
            if( (forceAdded |= (_clients.IndexOf( client ) < 0)) )
            {
                IActivityMonitorBoundClient? bound = client as IActivityMonitorBoundClient;
                if( bound != null )
                {
                    // Calling SetMonitor before adding it to the client first:
                    // - Enables the monitor to initialize itself before being sollicited.
                    // - If SetMonitor method calls InitializeTopicAndAutoTags, it does not
                    //   receive a "stupid" OnTopic/AutoTagsChanged.
                    // - Any exceptions like the ones created by CreateMultipleRegisterOnBoundClientException or
                    //   CreateBoundClientIsLockedException flow to the caller and have no impacts.
                    bound.SetMonitor( _monitor, false );
                }
                _clients.Add( client );
                if( bound != null ) _monitor.OnClientMinimalFilterChanged( LogFilter.Undefined, bound.MinimalFilter );
            }
            return client;
        }

        /// <summary>
        /// Registers a typed <see cref="IActivityMonitorClient"/>.
        /// </summary>
        /// <typeparam name="T">Any type that specializes <see cref="IActivityMonitorClient"/>.</typeparam>
        /// <param name="client">Clients to register.</param>
        /// <param name="added">True if the client has been added, false if it was already registered.</param>
        /// <returns>The registered client.</returns>
        public T RegisterClient<T>( T client, out bool added ) where T : IActivityMonitorClient
        {
            return (T)RegisterClient( (IActivityMonitorClient)client, out added );
        }

        /// <summary>
        /// Registers a <see cref="IActivityMonitorClient"/> that must be unique in a sense.
        /// </summary>
        /// <param name="tester">Predicate that must be satisfied for at least one registered client.</param>
        /// <param name="factory">Factory that will be called if no existing client satisfies <paramref name="tester"/>.</param>
        /// <returns>The existing or newly created client or null if the factory returned null.</returns>
        /// <remarks>
        /// The factory function MUST return null OR a client that satisfies the tester function otherwise a <see cref="InvalidOperationException"/> is thrown.
        /// When null is returned by the factory function, nothing is added and null is returned. 
        /// The factory is called only when no client satisfy the tester function: this makes the 'added' out parameter useless.
        /// </remarks>
        public T? RegisterUniqueClient<T>( Func<T, bool> tester, Func<T?> factory ) where T : IActivityMonitorClient
        {
            if( tester == null ) throw new ArgumentNullException( "tester" );
            if( factory == null ) throw new ArgumentNullException( "factory" );
            using( _monitor.ReentrancyAndConcurrencyLock() )
            {
                T e = _clients.OfType<T>().FirstOrDefault( tester );
                if( e == null )
                {
                    bool forceAdded = true;
                    if( (e = factory()) != null )
                    {
                        e = (T)DoRegisterClient( e, ref forceAdded );
                        if( !tester( e ) ) throw new InvalidOperationException( Impl.CoreResources.FactoryTesterMismatch );
                    }
                }
                return e;
            }
        }

        /// <summary>
        /// Unregisters the given <see cref="IActivityMonitorClient"/> from the <see cref="Clients"/> list.
        /// Silently ignores unregistered client.
        /// </summary>
        /// <param name="client">An <see cref="IActivityMonitorClient"/> implementation.</param>
        /// <returns>The unregistered client or null if it has not been found.</returns>
        public IActivityMonitorClient? UnregisterClient( IActivityMonitorClient client )
        {
            if( client == null ) throw new ArgumentNullException( "client" );
            using( _monitor.ReentrancyAndConcurrencyLock() )
            {
                int idx;
                if( (idx = _clients.IndexOf( client )) >= 0 )
                {
                    LogFilter filter = LogFilter.Undefined;
                    IActivityMonitorBoundClient? bound = client as IActivityMonitorBoundClient;
                    if( bound != null )
                    {
                        filter = bound.MinimalFilter;
                        bound.SetMonitor( null, false );
                    }
                    _clients.RemoveAt( idx );
                    if( filter != LogFilter.Undefined ) _monitor.OnClientMinimalFilterChanged( filter, LogFilter.Undefined );
                    return client;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the list of registered <see cref="IActivityMonitorClient"/>.
        /// </summary>
        public IReadOnlyList<IActivityMonitorClient> Clients => _clients;

        internal void ForceRemoveCondemnedClient( IActivityMonitorClient client )
        {
            Debug.Assert( client != null && _clients.Contains( client ) );
            IActivityMonitorBoundClient? bound = client as IActivityMonitorBoundClient;
            if( bound != null )
            {
                try
                {
                    bound.SetMonitor( null, true );
                }
                catch( Exception ex )
                {
                    ActivityMonitor.CriticalErrorCollector.Add( ex, $"While removing condemned client." );
                }
            }
            _clients.Remove( client );
        }

    }
}
