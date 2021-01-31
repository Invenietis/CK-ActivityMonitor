using CK.Core;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CK.PerfectEvent
{

    /// <summary>
    /// Async event handler that can be combined into a <see cref="SequentialEventHandlerAsyncSender{T}"/>.
    /// </summary>
    /// <param name="monitor">The monitor that must be used to log activities.</param>
    /// <param name="e">The event argument.</param>
    public delegate Task SequentialEventHandlerAsync<TEvent>( IActivityMonitor monitor, TEvent e );

    /// <summary>
    /// Implements a host for <see cref="SequentialEventHandlerAsync{TEvent}"/> delegates.
    /// </summary>
    /// <remarks>
    /// This cannot be implemented as a struct because the <see cref="operator+"/> and <see cref="operator-"/> must
    /// return the instance and a value type wouldn't correctly handle the null/single/array reference.
    /// </remarks>
    public class SequentialEventHandlerAsyncSender<TEvent>
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
        public SequentialEventHandlerAsyncSender<TEvent> Add( SequentialEventHandlerAsync<TEvent> handler )
        {
            if( handler == null ) throw new ArgumentNullException( nameof( handler ) );
            Util.InterlockedSet( ref _handler, h =>
            {
                if( h == null ) return handler;
                if( h is SequentialEventHandlerAsync<TEvent> a ) return new SequentialEventHandlerAsync<TEvent>[] { a, handler };
                var ah = (SequentialEventHandlerAsync<TEvent>[])h;
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
        public SequentialEventHandlerAsyncSender<TEvent> Remove( SequentialEventHandlerAsync<TEvent> handler )
        {
            Util.InterlockedSet( ref _handler, h =>
            {
                if( h == null ) return null;
                if( h is SequentialEventHandlerAsync<TEvent> a ) return a == handler ? null : h;
                var current = (SequentialEventHandlerAsync<TEvent>[])h;
                int idx = Array.IndexOf( current, handler );
                if( idx < 0 ) return current;
                Debug.Assert( current.Length > 1 );
                var ah = new SequentialEventHandlerAsync<TEvent>[current.Length - 1];
                Array.Copy( current, 0, ah, 0, idx );
                Array.Copy( current, idx + 1, ah, idx, ah.Length - idx );
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
        public static SequentialEventHandlerAsyncSender<TEvent> operator +( SequentialEventHandlerAsyncSender<TEvent> eventHost, SequentialEventHandlerAsync<TEvent> handler ) => eventHost.Add( handler );

        /// <summary>
        /// Relays to <see cref="Remove"/>.
        /// </summary>
        /// <param name="eventHost">The host.</param>
        /// <param name="handler">The non null handler to remove.</param>
        /// <returns>The host.</returns>
        public static SequentialEventHandlerAsyncSender<TEvent> operator -( SequentialEventHandlerAsyncSender<TEvent> eventHost, SequentialEventHandlerAsync<TEvent> handler ) => eventHost.Remove( handler );

        /// <summary>
        /// Clears the delegate list.
        /// </summary>
        public void RemoveAll() => _handler = null;

        /// <summary>
        /// Raises this event.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="e">The event argument.</param>
        public Task RaiseAsync( IActivityMonitor monitor, TEvent e )
        {
            var h = _handler;
            if( h == null ) return Task.CompletedTask;
            if( h is SequentialEventHandlerAsync<TEvent> a ) return a( monitor, e );
            return RaiseSequentialAsync( monitor, (SequentialEventHandlerAsync<TEvent>[])h, e );
        }

        static async Task RaiseSequentialAsync( IActivityMonitor monitor, SequentialEventHandlerAsync<TEvent>[] all, TEvent e )
        {
            foreach( var h in all ) await h( monitor, e );
        }
    }
}
