using CK.Core;

namespace CK.PerfectEvent
{
    /// <summary>
    /// Registration facade for <see cref="PerfectEventSender{TEvent}"/>.
    /// You can use the <see cref="Sync"/>, <see cref="Async"/> or <see cref="ParallelAsync"/> properties
    /// or directly add or remove one of the 3 handler types, either by using the Add/Remove method overloads
    /// or, more directly, <c>+=</c> and <c>-=</c> standard event operators.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event argument.</typeparam>
    public readonly struct PerfectEvent<TEvent>
    {
        readonly PerfectEventSender<TEvent> _sender;

        internal PerfectEvent( PerfectEventSender<TEvent> sender )
        {
            _sender = sender;
        }

        /// <summary>
        /// Gets whether at least one handler is registered.
        /// </summary>
        public bool HasHandlers => _sender.HasHandlers;

        /// <summary>
        /// Gets the Synchronous event registration point.
        /// </summary>
        /// <remarks>
        /// Note that handlers of the 3 types can be added and removed directly to this <see cref="PerfectEvent{TEvent}"/>:
        /// this event is a helper that better express the intent of the code.
        /// </remarks>
        public event SequentialEventHandler<TEvent> Sync
        {
            add => _sender.Add( value );
            remove => _sender.Remove( value );
        }

        /// <summary>
        /// Gets the Asynchronous event registration point.
        /// </summary>
        /// <remarks>
        /// Note that handlers of the 3 types can be added and removed directly to this <see cref="PerfectEvent{TEvent}"/>:
        /// this event is a helper that better express the intent of the code.
        /// </remarks>
        public event SequentialEventHandlerAsync<TEvent> Async
        {
            add => _sender.Add( value );
            remove => _sender.Remove( value );
        }

        /// <summary>
        /// Gets the Parallel Asynchronous event registration point.
        /// </summary>
        /// <remarks>
        /// Note that handlers of the 3 types can be added and removed directly to this <see cref="PerfectEvent{TEvent}"/>:
        /// this event is a helper that better express the intent of the code.
        /// </remarks>
        public event ParallelEventHandlerAsync<TEvent> ParallelAsync
        {
            add => _sender.Add( value );
            remove => _sender.Add( value );
        }

        #region Sequential.

        /// <summary>
        /// Adds a handler. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">Non null handler.</param>
        /// <returns>This PerfectEvent.</returns>
        public PerfectEvent<TEvent> Add( SequentialEventHandler<TEvent> handler )
        {
            _sender.Add( handler );
            return this;
        }

        /// <summary>
        /// Removes a handler if it exists. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">The handler to remove. Cannot be null.</param>
        /// <returns>This PerfectEvent.</returns>
        public PerfectEvent<TEvent> Remove( SequentialEventHandler<TEvent> handler )
        {
            _sender.Remove( handler );
            return this;
        }

        /// <summary>
        /// Relays to <see cref="Add(SequentialEventHandler{TEvent})"/>.
        /// </summary>
        /// <param name="this">This PerfectEvent.</param>
        /// <param name="handler">The non null handler to add.</param>
        /// <returns>This PerfectEvent.</returns>
        public static PerfectEvent<TEvent> operator +( PerfectEvent<TEvent> @this, SequentialEventHandler<TEvent> handler ) => @this.Add( handler );

        /// <summary>
        /// Relays to <see cref="Remove(SequentialEventHandler{TEvent})"/>.
        /// </summary>
        /// <param name="this">The host.</param>
        /// <param name="handler">The non null handler to remove.</param>
        /// <returns>This PerfectEvent.</returns>
        public static PerfectEvent<TEvent> operator -( PerfectEvent<TEvent> @this, SequentialEventHandler<TEvent> handler ) => @this.Remove( handler );

        #endregion

        #region Sequential Async.

        /// <summary>
        /// Adds a handler. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">Non null handler.</param>
        /// <returns>This PerfectEvent.</returns>
        public PerfectEvent<TEvent> Add( SequentialEventHandlerAsync<TEvent> handler )
        {
            _sender.Add( handler );
            return this;
        }

        /// <summary>
        /// Removes a handler if it exists. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">The handler to remove. Cannot be null.</param>
        /// <returns>This PerfectEvent.</returns>
        public PerfectEvent<TEvent> Remove( SequentialEventHandlerAsync<TEvent> handler )
        {
            _sender.Remove( handler );
            return this;
        }

        /// <summary>
        /// Relays to <see cref="Add(SequentialEventHandlerAsync{TEvent})"/>.
        /// </summary>
        /// <param name="this">This PerfectEvent.</param>
        /// <param name="handler">The non null handler to add.</param>
        /// <returns>This PerfectEvent.</returns>
        public static PerfectEvent<TEvent> operator +( PerfectEvent<TEvent> @this, SequentialEventHandlerAsync<TEvent> handler ) => @this.Add( handler );

        /// <summary>
        /// Relays to <see cref="Remove(SequentialEventHandlerAsync{TEvent})"/>.
        /// </summary>
        /// <param name="this">This PerfectEvent.</param>
        /// <param name="handler">The non null handler to remove.</param>
        /// <returns>This PerfectEvent.</returns>
        public static PerfectEvent<TEvent> operator -( PerfectEvent<TEvent> @this, SequentialEventHandlerAsync<TEvent> handler ) => @this.Remove( handler );

        #endregion

        #region Parallel Async.

        /// <summary>
        /// Adds a handler. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">Non null handler.</param>
        /// <returns>This PerfectEvent.</returns>
        public PerfectEvent<TEvent> Add( ParallelEventHandlerAsync<TEvent> handler )
        {
            _sender.Add( handler );
            return this;
        }

        /// <summary>
        /// Removes a handler if it exists. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">The handler to remove. Cannot be null.</param>
        /// <returns>This PerfectEvent.</returns>
        public PerfectEvent<TEvent> Remove( ParallelEventHandlerAsync<TEvent> handler )
        {
            _sender.Remove( handler );
            return this;
        }

        /// <summary>
        /// Relays to <see cref="Add(ParallelEventHandlerAsync{TEvent})"/>.
        /// </summary>
        /// <param name="this">This PerfectEvent.</param>
        /// <param name="handler">The non null handler to add.</param>
        /// <returns>This PerfectEvent.</returns>
        public static PerfectEvent<TEvent> operator +( PerfectEvent<TEvent> @this, ParallelEventHandlerAsync<TEvent> handler ) => @this.Add( handler );

        /// <summary>
        /// Relays to <see cref="Remove(ParallelEventHandlerAsync{TEvent})"/>.
        /// </summary>
        /// <param name="this">This PerfectEvent.</param>
        /// <param name="handler">The non null handler to remove.</param>
        /// <returns>This PerfectEvent.</returns>
        public static PerfectEvent<TEvent> operator -( PerfectEvent<TEvent> @this, ParallelEventHandlerAsync<TEvent> handler ) => @this.Remove( handler );

        #endregion

    }
}
