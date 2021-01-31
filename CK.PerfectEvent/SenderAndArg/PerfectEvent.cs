using CK.Core;

namespace CK.PerfectEvent
{
    /// <summary>
    /// Registration facade for <see cref="PerfectEventSender{TSender, TArg}"/>.
    /// You can use the <see cref="Sync"/>, <see cref="Async"/> or <see cref="ParallelAsync"/> properties
    /// or directly add or remove one of the 3 handler types, either by using the Add/Remove method overloads
    /// or, more directly, <c>+=</c> and <c>-=</c> standard event operators.
    /// </summary>
    /// <typeparam name="TSender">The type of the event sender.</typeparam>
    /// <typeparam name="TArg">The type of the event argument.</typeparam>
    public readonly struct PerfectEvent<TSender, TArg>
    {
        readonly PerfectEventSender<TSender, TArg> _sender;

        internal PerfectEvent( PerfectEventSender<TSender, TArg> sender )
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
        /// Note that handlers of the 3 types can be added and removed directly to this <see cref="PerfectEvent{TSender, TArg}"/>:
        /// this event is a helper that better express the intent of the code.
        /// </remarks>
        public event SequentialEventHandler<TSender, TArg> Sync
        {
            add => _sender.Add( value );
            remove => _sender.Remove( value );
        }

        /// <summary>
        /// Gets the Asynchronous event registration point.
        /// </summary>
        /// <remarks>
        /// Note that handlers of the 3 types can be added and removed directly to this <see cref="PerfectEvent{TSender, TArg}"/>:
        /// this event is a helper that better express the intent of the code.
        /// </remarks>
        public event SequentialEventHandlerAsync<TSender, TArg> Async
        {
            add => _sender.Add( value );
            remove => _sender.Remove( value );
        }

        /// <summary>
        /// Gets the Parallel Asynchronous event registration point.
        /// </summary>
        /// <remarks>
        /// Note that handlers of the 3 types can be added and removed directly to this <see cref="PerfectEvent{TSender, TArg}"/>:
        /// this event is a helper that better express the intent of the code.
        /// </remarks>
        public event ParallelEventHandlerAsync<TSender, TArg> ParallelAsync
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
        public PerfectEvent<TSender, TArg> Add( SequentialEventHandler<TSender, TArg> handler )
        {
            _sender.Add( handler );
            return this;
        }

        /// <summary>
        /// Removes a handler if it exists. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">The handler to remove. Cannot be null.</param>
        /// <returns>This PerfectEvent.</returns>
        public PerfectEvent<TSender, TArg> Remove( SequentialEventHandler<TSender, TArg> handler )
        {
            _sender.Remove( handler );
            return this;
        }

        /// <summary>
        /// Relays to <see cref="Add(SequentialEventHandler{TSender, TArg})"/>.
        /// </summary>
        /// <param name="this">This PerfectEvent.</param>
        /// <param name="handler">The non null handler to add.</param>
        /// <returns>This PerfectEvent.</returns>
        public static PerfectEvent<TSender, TArg> operator +( PerfectEvent<TSender, TArg> @this, SequentialEventHandler<TSender, TArg> handler ) => @this.Add( handler );

        /// <summary>
        /// Relays to <see cref="Remove(SequentialEventHandler{TSender, TArg})"/>.
        /// </summary>
        /// <param name="this">The host.</param>
        /// <param name="handler">The non null handler to remove.</param>
        /// <returns>This PerfectEvent.</returns>
        public static PerfectEvent<TSender, TArg> operator -( PerfectEvent<TSender, TArg> @this, SequentialEventHandler<TSender, TArg> handler ) => @this.Remove( handler );

        #endregion

        #region Sequential Async.

        /// <summary>
        /// Adds a handler. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">Non null handler.</param>
        /// <returns>This PerfectEvent.</returns>
        public PerfectEvent<TSender, TArg> Add( SequentialEventHandlerAsync<TSender, TArg> handler )
        {
            _sender.Add( handler );
            return this;
        }

        /// <summary>
        /// Removes a handler if it exists. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">The handler to remove. Cannot be null.</param>
        /// <returns>This PerfectEvent.</returns>
        public PerfectEvent<TSender, TArg> Remove( SequentialEventHandlerAsync<TSender, TArg> handler )
        {
            _sender.Remove( handler );
            return this;
        }

        /// <summary>
        /// Relays to <see cref="Add(SequentialEventHandlerAsync{TSender,TArg})"/>.
        /// </summary>
        /// <param name="this">This PerfectEvent.</param>
        /// <param name="handler">The non null handler to add.</param>
        /// <returns>This PerfectEvent.</returns>
        public static PerfectEvent<TSender, TArg> operator +( PerfectEvent<TSender, TArg> @this, SequentialEventHandlerAsync<TSender, TArg> handler ) => @this.Add( handler );

        /// <summary>
        /// Relays to <see cref="Remove(SequentialEventHandlerAsync{TSender,TArg})"/>.
        /// </summary>
        /// <param name="this">This PerfectEvent.</param>
        /// <param name="handler">The non null handler to remove.</param>
        /// <returns>This PerfectEvent.</returns>
        public static PerfectEvent<TSender, TArg> operator -( PerfectEvent<TSender, TArg> @this, SequentialEventHandlerAsync<TSender, TArg> handler ) => @this.Remove( handler );

        #endregion

        #region Parallel Async.

        /// <summary>
        /// Adds a handler. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">Non null handler.</param>
        /// <returns>This PerfectEvent.</returns>
        public PerfectEvent<TSender, TArg> Add( ParallelEventHandlerAsync<TSender, TArg> handler )
        {
            _sender.Add( handler );
            return this;
        }

        /// <summary>
        /// Removes a handler if it exists. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">The handler to remove. Cannot be null.</param>
        /// <returns>This PerfectEvent.</returns>
        public PerfectEvent<TSender, TArg> Remove( ParallelEventHandlerAsync<TSender, TArg> handler )
        {
            _sender.Remove( handler );
            return this;
        }

        /// <summary>
        /// Relays to <see cref="Add(ParallelEventHandlerAsync{TSender, TArg})"/>.
        /// </summary>
        /// <param name="this">This PerfectEvent.</param>
        /// <param name="handler">The non null handler to add.</param>
        /// <returns>This PerfectEvent.</returns>
        public static PerfectEvent<TSender, TArg> operator +( PerfectEvent<TSender, TArg> @this, ParallelEventHandlerAsync<TSender, TArg> handler ) => @this.Add( handler );

        /// <summary>
        /// Relays to <see cref="Remove(ParallelEventHandlerAsync{TSender, TArg})"/>.
        /// </summary>
        /// <param name="this">This PerfectEvent.</param>
        /// <param name="handler">The non null handler to remove.</param>
        /// <returns>This PerfectEvent.</returns>
        public static PerfectEvent<TSender, TArg> operator -( PerfectEvent<TSender, TArg> @this, ParallelEventHandlerAsync<TSender, TArg> handler ) => @this.Remove( handler );

        #endregion

    }
}
