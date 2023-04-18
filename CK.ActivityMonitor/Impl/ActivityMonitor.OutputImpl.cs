using CK.Core.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;

namespace CK.Core
{
    public sealed partial class ActivityMonitor
    {
        /// <summary>
        /// Implementation of <see cref="IActivityMonitorOutput"/> for <see cref="IActivityMonitor.Output"/>.
        /// </summary>
        sealed class OutputImpl : IActivityMonitorOutput, ActivityMonitorLogData.IFactory
        {
            readonly List<IActivityMonitorClient> _clients;
            readonly ActivityMonitor _monitor;

            public OutputImpl( ActivityMonitor monitor )
            {
                Throw.CheckNotNullArgument( monitor );
                _monitor = monitor;
                _clients = new List<IActivityMonitorClient>();
            }

            ActivityMonitorLogData ActivityMonitorLogData.IFactory.CreateLogData( LogLevel level,
                                                                                  CKTrait finalTags,
                                                                                  string? text,
                                                                                  Exception? exception,
                                                                                  string? fileName,
                                                                                  int lineNumber )
            {
                return new ActivityMonitorLogData( _monitor._uniqueId,
                                                   _monitor._lastLogTime = new DateTimeStamp( _monitor._lastLogTime, DateTime.UtcNow ),
                                                   _monitor._currentDepth,
                                                   false,
                                                   level,
                                                   finalTags,
                                                   text,
                                                   exception,
                                                   fileName,
                                                   lineNumber );
            }

            DateTimeStamp ActivityMonitorLogData.IFactory.GetLogTime() => _monitor._lastLogTime = new DateTimeStamp( _monitor._lastLogTime, DateTime.UtcNow );

            public IActivityMonitorClient RegisterClient( IActivityMonitorClient client, out bool added )
            {
                Throw.CheckNotNullArgument( client );
                using( ((IActivityMonitorImpl)_monitor).ReentrancyAndConcurrencyLock() )
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
                        // - Enables the monitor to initialize itself before being solicited.
                        // - If SetMonitor method calls InitializeTopicAndAutoTags, it does not
                        //   receive a "stupid" OnTopic/AutoTagsChanged.
                        // - Any exceptions like the ones created by CreateMultipleRegisterOnBoundClientException or
                        //   CreateBoundClientIsLockedException flow to the caller and have no impacts.
                        bound.SetMonitor( _monitor, false );
                    }
                    _clients.Add( client );
                    if( bound != null ) ((IActivityMonitorImpl)_monitor).OnClientMinimalFilterChanged( LogFilter.Undefined, bound.MinimalFilter );
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
                Throw.CheckNotNullArgument( tester );
                Throw.CheckNotNullArgument( factory );
                using( ((IActivityMonitorImpl)_monitor).ReentrancyAndConcurrencyLock() )
                {
                    T? e = _clients.OfType<T>().FirstOrDefault( tester );
                    if( e == null )
                    {
                        bool forceAdded = true;
                        if( (e = factory()) != null )
                        {
                            e = (T)DoRegisterClient( e, ref forceAdded );
                            if( !tester( e ) ) Throw.InvalidOperationException( Impl.CoreResources.FactoryTesterMismatch );
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
                Throw.CheckNotNullArgument( client );
                using( ((IActivityMonitorImpl)_monitor).ReentrancyAndConcurrencyLock() )
                {
                    int idx;
                    if( (idx = _clients.IndexOf( client )) >= 0 )
                    {
                        // Removes the client first: if an exception is raised here
                        // (by a bound client), it bubbles to the caller and this is fine:
                        // UnregisterClient is a direct API call.
                        _clients.RemoveAt( idx );
                        LogFilter filter = LogFilter.Undefined;
                        if( client is IActivityMonitorBoundClient bound )
                        {
                            filter = bound.MinimalFilter;
                            bound.SetMonitor( null, false );
                        }
                        if( filter != LogFilter.Undefined ) ((IActivityMonitorImpl)_monitor).OnClientMinimalFilterChanged( filter, LogFilter.Undefined );
                        return client;
                    }
                    return null;
                }
            }

            /// <summary>
            /// Gets the list of registered <see cref="IActivityMonitorClient"/>.
            /// </summary>
            public IReadOnlyList<IActivityMonitorClient> Clients => _clients;

            internal Exception? ForceRemoveCondemnedClient( IActivityMonitorClient client )
            {
                Debug.Assert( client != null && _clients.Contains( client ) );
                if( client is IActivityMonitorBoundClient bound )
                {
                    try
                    {
                        bound.SetMonitor( null, true );
                    }
                    catch( Exception ex )
                    {
                        return ex;
                    }
                }
                _clients.Remove( client );
                return null;
            }

        }
    }
}
