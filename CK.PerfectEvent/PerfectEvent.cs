using CK.Core;

namespace CK.PerfectEvent
{
    /// <summary>
    /// Registration facade for <see cref="PerfectEventSender{TEvent}"/>.
    /// To subscribe and unsubscribe to this event, use the <see cref="Sync"/>, <see cref="Async"/> or <see cref="ParallelAsync"/> with
    /// <c>+=</c> and <c>-=</c> standard event operators, or one of the Add/Remove method overloads.
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
        public event SequentialEventHandler<TEvent> Sync
        {
            add => _sender.Add( value );
            remove => _sender.Remove( value );
        }

        /// <summary>
        /// Gets the Asynchronous event registration point.
        /// </summary>
        public event SequentialEventHandlerAsync<TEvent> Async
        {
            add => _sender.Add( value );
            remove => _sender.Remove( value );
        }

        /// <summary>
        /// Gets the Parallel Asynchronous event registration point.
        /// </summary>
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

        #endregion

    }
}
