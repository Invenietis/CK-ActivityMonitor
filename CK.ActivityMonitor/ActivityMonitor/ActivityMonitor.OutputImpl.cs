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

            public int? MaxInitialReplayCount
            {
                get => _monitor._initialReplay?._maxCount;
                set
                {
                    Throw.CheckArgument( value is null || value >= 0 );
                    _monitor.ReentrantAndConcurrentCheck();
                    try
                    {
                        var r = _monitor._initialReplay;
                        if( r != null )
                        {
                            if( value is null || value <= r._count )
                            {
                                _monitor.DoStopInitialReplay();
                            }
                            else
                            {
                                r._maxCount = value.Value;
                            }
                        }
                    }
                    finally
                    {
                        _monitor.ReentrantAndConcurrentRelease();
                    }
                }
            }

            public IActivityMonitorClient RegisterClient( IActivityMonitorClient client, out bool added )
            {
                Throw.CheckNotNullArgument( client );
                _monitor.ReentrantAndConcurrentCheck();
                try
                {
                    added = false;
                    return DoRegisterClient( client, ref added );
                }
                finally
                {
                    _monitor.ReentrantAndConcurrentRelease();
                }
            }

            IActivityMonitorClient DoRegisterClient( IActivityMonitorClient client, ref bool forceAdded )
            {
                if( (forceAdded |= (_clients.IndexOf( client ) < 0)) )
                {
                    IActivityMonitorBoundClient? bound = client as IActivityMonitorBoundClient;
                    if( bound != null )
                    {
                        Throw.CheckArgument( "Cannot register a dead client.", !bound.IsDead );
                        // Calling SetMonitor before adding it to the client first: exceptions flow to
                        // the caller and have no impacts.
                        bound.SetMonitor( _monitor, false );
                    }
                    _clients.Add( client );
                    _monitor._initialReplay?.Replay( client );
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
                Throw.CheckNotNullArgument( tester );
                Throw.CheckNotNullArgument( factory );
                _monitor.ReentrantAndConcurrentCheck();
                try
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
                finally
                {
                    _monitor.ReentrantAndConcurrentRelease();
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
                _monitor.ReentrantAndConcurrentCheck();
                try
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
                        if( filter != LogFilter.Undefined ) _monitor.OnClientMinimalFilterChanged( filter, LogFilter.Undefined );
                        return client;
                    }
                    return null;
                }
                finally
                {
                    _monitor.ReentrantAndConcurrentRelease();
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
