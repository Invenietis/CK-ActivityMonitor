using CK.Core;

namespace CK.PerfectEvent
{
    /// <summary>
    /// Registration facade for <see cref="PerfectEventSender{TSender, TArg}"/>.
    /// To subscribe and unsubscribe to this event, use the <see cref="Sync"/>, <see cref="Async"/> or <see cref="ParallelAsync"/> with
    /// <c>+=</c> and <c>-=</c> standard event operators, or one of the Add/Remove method overloads.
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
        public event SequentialEventHandler<TSender, TArg> Sync
        {
            add => _sender.Add( value );
            remove => _sender.Remove( value );
        }

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        /// <summary>
        /// Gets the Asynchronous event registration point.
        /// </summary>
        public event SequentialEventHandlerAsync<TSender, TArg> Async
        {
            add => _sender.Add( value );
            remove => _sender.Remove( value );
        }

        /// <summary>
        /// Gets the Parallel Asynchronous event registration point.
        /// </summary>
        public event ParallelEventHandlerAsync<TSender, TArg> ParallelAsync
        {
            add => _sender.Add( value );
            remove => _sender.Add( value );
        }
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods

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

        #endregion

    }
}
