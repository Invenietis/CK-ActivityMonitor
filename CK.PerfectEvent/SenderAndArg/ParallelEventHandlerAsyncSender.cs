using CK.Core;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CK.PerfectEvent
{

    /// <summary>
    /// Async event handler that can be combined into a <see cref="ParallelEventHandlerAsyncSender{TSender, TArg}"/>.
    /// </summary>
    /// <typeparam name="TSender">Type of the sender.</typeparam>
    /// <typeparam name="TArg">Type of the event argument.</typeparam>
    /// <param name="token">The activity token to use in any other monitor.</param>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event argument.</param>
    public delegate Task ParallelEventHandlerAsync<TSender, TArg>( ActivityMonitor.DependentToken token, TSender sender, TArg e );

    /// <summary>
    /// Implements a host for <see cref="ParallelEventHandlerAsync{TSender,TArg}"/> delegates.
    /// </summary>
    /// <remarks>
    /// This cannot be implemented as a struct because the <see cref="operator+"/> and <see cref="operator-"/> must
    /// return the instance and a value type wouldn't correctly handle the null/single/array reference.
    /// </remarks>
    /// <typeparam name="TSender">Type of the sender.</typeparam>
    /// <typeparam name="TArg">Type of the event argument.</typeparam>
    public class ParallelEventHandlerAsyncSender<TSender, TArg>
    {
        object? _handler;

        /// <summary>
        /// Gets whether at least one handler is registered.
        /// </summary>
        public bool HasHandlers => _handler != null;

        /// <summary>
        /// Adds a handler. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">Non null handler.</param>
        public ParallelEventHandlerAsyncSender<TSender, TArg> Add( ParallelEventHandlerAsync<TSender, TArg> handler )
        {
            if( handler == null ) throw new ArgumentNullException( nameof( handler ) );
            Util.InterlockedSet( ref _handler, h =>
            {
                if( h == null ) return handler;
                if( h is ParallelEventHandlerAsync<TSender, TArg> a ) return new ParallelEventHandlerAsync<TSender, TArg>[] { a, handler };
                var ah = (ParallelEventHandlerAsync<TSender, TArg>[])h;
                int len = ah.Length;
                Array.Resize( ref ah, len + 1 );
                ah[len] = handler;
                return ah;
            } );
            return this;
        }

        /// <summary>
        /// Removes a handler if it exists. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">The handler to remove. Cannot be null.</param>
        public ParallelEventHandlerAsyncSender<TSender, TArg> Remove( ParallelEventHandlerAsync<TSender, TArg> handler )
        {
            if( handler == null ) throw new ArgumentNullException( nameof( handler ) );
            Util.InterlockedSet( ref _handler, h =>
            {
                if( h == null ) return null;
                if( h is ParallelEventHandlerAsync<TSender, TArg> a ) return a == handler ? null : h;
                var current = (ParallelEventHandlerAsync<TSender, TArg>[])h;
                int idx = Array.IndexOf( current, handler );
                if( idx < 0 ) return current;
                Debug.Assert( current.Length > 1 );
                var ah = new ParallelEventHandlerAsync<TSender, TArg>[current.Length - 1];
                System.Array.Copy( current, 0, ah, 0, idx );
                System.Array.Copy( current, idx + 1, ah, idx, ah.Length - idx );
                return ah;
            } );
            return this;
        }

        /// <summary>
        /// Relays to <see cref="Add"/>.
        /// </summary>
        /// <param name="eventHost">The host.</param>
        /// <param name="handler">The non null handler to add.</param>
        /// <returns>The host.</returns>
        public static ParallelEventHandlerAsyncSender<TSender, TArg> operator +( ParallelEventHandlerAsyncSender<TSender, TArg> eventHost, ParallelEventHandlerAsync<TSender, TArg> handler ) => eventHost.Add( handler );

        /// <summary>
        /// Relays to <see cref="Remove"/>.
        /// </summary>
        /// <param name="eventHost">The host.</param>
        /// <param name="handler">The non null handler to remove.</param>
        /// <returns>The host.</returns>
        public static ParallelEventHandlerAsyncSender<TSender, TArg> operator -( ParallelEventHandlerAsyncSender<TSender, TArg> eventHost, ParallelEventHandlerAsync<TSender, TArg> handler ) => eventHost.Remove( handler );

        /// <summary>
        /// Clears the delegate list.
        /// </summary>
        public void RemoveAll() => _handler = null;

        /// <summary>
        /// Raises this event.
        /// </summary>
        /// <param name="monitor">The monitor from which <see cref="ActivityMonitor.DependentToken"/> will be issued.</param>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="args">The event argument.</param>
        public Task RaiseAsync( IActivityMonitor monitor, TSender sender, TArg args )
        {
            var h = _handler;
            if( h == null ) return Task.CompletedTask;
            if( h is ParallelEventHandlerAsync<TSender, TArg> a ) return a( monitor.DependentActivity().CreateToken(), sender, args );
            var all = (ParallelEventHandlerAsync<TSender, TArg>[])h;
            return Task.WhenAll( all.Select( x => x( monitor.DependentActivity().CreateToken(), sender, args ) ) );
        }
    }
}
