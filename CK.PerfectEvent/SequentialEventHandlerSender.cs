using CK.Core;

using System;
using System.Diagnostics;

namespace CK.PerfectEvent
{

    /// <summary>
    /// Event handler that can be combined into a <see cref="SequentialEventHandlerSender{T}"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="e">The event argument.</param>
    public delegate void SequentialEventHandler<TEvent>( IActivityMonitor monitor, TEvent e );

    /// <summary>
    /// Implements a host for <see cref="SequentialEventHandler{TSender,TArg}"/> delegates.
    /// </summary>
    /// <remarks>
    /// This cannot be implemented as a struct because the <see cref="operator+"/> and <see cref="operator-"/> must
    /// return the instance and a value type wouldn't correctly handle the null/single/array reference.
    /// </remarks>
    public class SequentialEventHandlerSender<TEvent>
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
        public SequentialEventHandlerSender<TEvent> Add( SequentialEventHandler<TEvent> handler )
        {
            if( handler == null ) throw new ArgumentNullException( nameof( handler ) );
            Util.InterlockedSet( ref _handler, h =>
            {
                if( h == null ) return handler;
                if( h is SequentialEventHandler<TEvent> a ) return new SequentialEventHandler<TEvent>[] { a, handler };
                var ah = (SequentialEventHandler<TEvent>[])h;
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
        public SequentialEventHandlerSender<TEvent> Remove( SequentialEventHandler<TEvent> handler )
        {
            if( handler == null ) throw new ArgumentNullException( nameof( handler ) );
            Util.InterlockedSet( ref _handler, h =>
            {
                if( h == null ) return null;
                if( h is SequentialEventHandler<TEvent> a ) return a == handler ? null : h;
                var current = (SequentialEventHandler<TEvent>[])h;
                int idx = Array.IndexOf( current, handler );
                if( idx < 0 ) return current;
                Debug.Assert( current.Length > 1 );
                var ah = new SequentialEventHandler<TEvent>[current.Length - 1];
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
        public static SequentialEventHandlerSender<TEvent> operator +( SequentialEventHandlerSender<TEvent> eventHost, SequentialEventHandler<TEvent> handler ) => eventHost.Add( handler );

        /// <summary>
        /// Relays to <see cref="Remove"/>.
        /// </summary>
        /// <param name="eventHost">The host.</param>
        /// <param name="handler">The non null handler to remove.</param>
        /// <returns>The host.</returns>
        public static SequentialEventHandlerSender<TEvent> operator -( SequentialEventHandlerSender<TEvent> eventHost, SequentialEventHandler<TEvent> handler ) => eventHost.Remove( handler );

        /// <summary>
        /// Clears the delegate list.
        /// </summary>
        public void RemoveAll() => _handler = null;

        /// <summary>
        /// Raises this event.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="e">The event argument.</param>
        public void Raise( IActivityMonitor monitor, TEvent e )
        {
            var h = _handler;
            if( h == null ) return;
            if( h is SequentialEventHandler<TEvent> a ) a( monitor, e );
            else
            {
                var all = (SequentialEventHandler<TEvent>[])h;
                foreach( var x in all ) x( monitor, e );
            }
        }
    }
}
