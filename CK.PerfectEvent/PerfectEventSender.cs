using CK.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CK.PerfectEvent
{
    /// <summary>
    /// A perfect event sender offers synchronous, asynchronous and parallel asynchronous event support.
    /// You can use the <see cref="Sync"/>, <see cref="Async"/> or <see cref="ParallelAsync"/> properties
    /// or directly add or remove one of the 3 handler types, either by using the Add/Remove method overloads
    /// or, more directly, <c>+=</c> and <c>-=</c> standard event operators.
    /// <para>
    /// Instances of this class should be kept private: only the sender object should be able to call <see cref="RaiseAsync(IActivityMonitor, TEvent)"/>
    /// or <see cref="SafeRaiseAsync(IActivityMonitor, TEvent, string?, int)"/>.
    /// What should be exposed is the <see cref="PerfectEvent"/> property that restricts the API to event registration.
    /// </para>
    /// </summary>
    /// <typeparam name="TEvent">The type of the event argument.</typeparam>
    public class PerfectEventSender<TEvent>
    {
        readonly SequentialEventHandlerSender<TEvent> _seq;
        readonly SequentialEventHandlerAsyncSender<TEvent> _seqAsync;
        readonly ParallelEventHandlerAsyncSender<TEvent> _parallelAsync;

        /// <summary>
        /// Initializes a new <see cref="PerfectEventSender{TEvent}"/>.
        /// </summary>
        public PerfectEventSender()
        {
            _seq = new SequentialEventHandlerSender<TEvent>();
            _seqAsync = new SequentialEventHandlerAsyncSender<TEvent>();
            _parallelAsync = new ParallelEventHandlerAsyncSender<TEvent>();
        }

        /// <summary>
        /// Gets the event that should be exposed to the external world: through the <see cref="PerfectEvent{TEvent}"/>,
        /// only registration/unregistration is possible.
        /// </summary>
        public PerfectEvent<TEvent> PerfectEvent => new PerfectEvent<TEvent>( this );

        /// <summary>
        /// Gets whether at least one handler is registered.
        /// </summary>
        public bool HasHandlers => _seq.HasHandlers || _seqAsync.HasHandlers || _parallelAsync.HasHandlers;

        /// <summary>
        /// Clears the delegate list.
        /// </summary>
        public void RemoveAll()
        {
            _seq.RemoveAll();
            _seqAsync.RemoveAll();
            _parallelAsync.RemoveAll();
        }

        /// <summary>
        /// Gets the Synchronous event registration point.
        /// </summary>
        /// <remarks>
        /// Note that handlers of the 3 types can be added and removed directly to this <see cref="PerfectEventSender{TEvent}"/>:
        /// this event is a helper that better express the intent of the code.
        /// </remarks>
        public event SequentialEventHandler<TEvent> Sync
        {
            add => _seq.Add( value );
            remove => _seq.Remove( value );
        }

        /// <summary>
        /// Gets the Asynchronous event registration point.
        /// </summary>
        /// <remarks>
        /// Note that handlers of the 3 types can be added and removed directly to this <see cref="PerfectEventSender{TEvent}"/>:
        /// this event is a helper that better express the intent of the code.
        /// </remarks>
        public event SequentialEventHandlerAsync<TEvent> Async
        {
            add => _seqAsync.Add( value );
            remove => _seqAsync.Remove( value );
        }

        /// <summary>
        /// Gets the Parallel Asynchronous event registration point.
        /// </summary>
        /// <remarks>
        /// Note that handlers of the 3 types can be added and removed directly to this <see cref="PerfectEventSender{TEvent}"/>:
        /// this event is a helper that better express the intent of the code.
        /// </remarks>
        public event ParallelEventHandlerAsync<TEvent> ParallelAsync
        {
            add => _parallelAsync.Add( value );
            remove => _parallelAsync.Add( value );
        }

        #region Sequential.

        /// <summary>
        /// Adds a handler. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">Non null handler.</param>
        /// <returns>This PerfectEventSender.</returns>
        public PerfectEventSender<TEvent> Add( SequentialEventHandler<TEvent> handler )
        {
            _seq.Add( handler );
            return this;
        }

        /// <summary>
        /// Removes a handler if it exists. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">The handler to remove. Cannot be null.</param>
        /// <returns>This PerfectEventSender.</returns>
        public PerfectEventSender<TEvent> Remove( SequentialEventHandler<TEvent> handler )
        {
            _seq.Remove( handler );
            return this;
        }

        /// <summary>
        /// Relays to <see cref="Add(SequentialEventHandler{TEvent})"/>.
        /// </summary>
        /// <param name="this">This PerfectEventSender.</param>
        /// <param name="handler">The non null handler to add.</param>
        /// <returns>This PerfectEventSender.</returns>
        public static PerfectEventSender<TEvent> operator +( PerfectEventSender<TEvent> @this, SequentialEventHandler<TEvent> handler ) => @this.Add( handler );

        /// <summary>
        /// Relays to <see cref="Remove(SequentialEventHandler{TEvent})"/>.
        /// </summary>
        /// <param name="this">The host.</param>
        /// <param name="handler">The non null handler to remove.</param>
        /// <returns>This PerfectEventSender.</returns>
        public static PerfectEventSender<TEvent> operator -( PerfectEventSender<TEvent> @this, SequentialEventHandler<TEvent> handler ) => @this.Remove( handler );

        #endregion

        #region Sequential Async.

        /// <summary>
        /// Adds a handler. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">Non null handler.</param>
        /// <returns>This PerfectEventSender.</returns>
        public PerfectEventSender<TEvent> Add( SequentialEventHandlerAsync<TEvent> handler )
        {
            _seqAsync.Add( handler );
            return this;
        }

        /// <summary>
        /// Removes a handler if it exists. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">The handler to remove. Cannot be null.</param>
        /// <returns>This PerfectEventSender.</returns>
        public PerfectEventSender<TEvent> Remove( SequentialEventHandlerAsync<TEvent> handler )
        {
            _seqAsync.Remove( handler );
            return this;
        }

        /// <summary>
        /// Relays to <see cref="Add(SequentialEventHandlerAsync{TEvent})"/>.
        /// </summary>
        /// <param name="this">This PerfectEventSender.</param>
        /// <param name="handler">The non null handler to add.</param>
        /// <returns>This PerfectEventSender.</returns>
        public static PerfectEventSender<TEvent> operator +( PerfectEventSender<TEvent> @this, SequentialEventHandlerAsync<TEvent> handler ) => @this.Add( handler );

        /// <summary>
        /// Relays to <see cref="Remove(SequentialEventHandlerAsync{TEvent})"/>.
        /// </summary>
        /// <param name="this">This PerfectEventSender.</param>
        /// <param name="handler">The non null handler to remove.</param>
        /// <returns>This PerfectEventSender.</returns>
        public static PerfectEventSender<TEvent> operator -( PerfectEventSender<TEvent> @this, SequentialEventHandlerAsync<TEvent> handler ) => @this.Remove( handler );

        #endregion

        #region Parallel Async.

        /// <summary>
        /// Adds a handler. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">Non null handler.</param>
        /// <returns>This PerfectEventSender.</returns>
        public PerfectEventSender<TEvent> Add( ParallelEventHandlerAsync<TEvent> handler )
        {
            _parallelAsync.Add( handler );
            return this;
        }

        /// <summary>
        /// Removes a handler if it exists. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">The handler to remove. Cannot be null.</param>
        /// <returns>This PerfectEventSender.</returns>
        public PerfectEventSender<TEvent> Remove( ParallelEventHandlerAsync<TEvent> handler )
        {
            _parallelAsync.Remove( handler );
            return this;
        }

        /// <summary>
        /// Relays to <see cref="Add(ParallelEventHandlerAsync{TEvent})"/>.
        /// </summary>
        /// <param name="this">This PerfectEventSender.</param>
        /// <param name="handler">The non null handler to add.</param>
        /// <returns>This PerfectEventSender.</returns>
        public static PerfectEventSender<TEvent> operator +( PerfectEventSender<TEvent> @this, ParallelEventHandlerAsync<TEvent> handler ) => @this.Add( handler );

        /// <summary>
        /// Relays to <see cref="Remove(ParallelEventHandlerAsync{TEvent})"/>.
        /// </summary>
        /// <param name="this">This PerfectEventSender.</param>
        /// <param name="handler">The non null handler to remove.</param>
        /// <returns>This PerfectEventSender.</returns>
        public static PerfectEventSender<TEvent> operator -( PerfectEventSender<TEvent> @this, ParallelEventHandlerAsync<TEvent> handler ) => @this.Remove( handler );


        #endregion

        /// <summary>
        /// Raises this event: <see cref="ParallelAsync"/> events are executing while <see cref="Sync"/> events and then <see cref="Async"/>
        /// events are executing.
        /// <para>
        /// The returned task is resolved once the parrallels, the synchronous and the asynhronous event handlers have finished their jobs.
        /// </para>
        /// <para>
        /// If exceptions occurred, they are logged and false is returned.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="e">The argument of the event.</param>
        /// <param name="fileName">The source filename where this event is raised.</param>
        /// <param name="lineNumber">The source line number in the filename where this event is raised.</param>
        /// <returns>True on success, false if an exception occurred.</returns>
        public async Task<bool> SafeRaiseAsync( IActivityMonitor monitor, TEvent e, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0 )
        {
            try
            {
                Task task = _parallelAsync.RaiseAsync( monitor, e );
                _seq.Raise( monitor, e );
                await Task.WhenAll( task, _seqAsync.RaiseAsync( monitor, e ) );
                return true;
            }
            catch( Exception ex )
            {
                if( monitor.ShouldLogLine( LogLevel.Error, fileName, lineNumber ) )
                {
                    monitor.UnfilteredLog( null, LogLevel.Error, $"While raising event '{e}'.", monitor.NextLogTime(), ex, fileName, lineNumber );
                }
                return false;
            }
        }

        /// <summary>
        /// Raises this event: <see cref="ParallelAsync"/> events are executing while <see cref="Sync"/> events and then <see cref="Async"/>
        /// events are executing.
        /// The returned task is resolved once the parallels, the synchronous and the asynhronous event handlers have finished their jobs.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="e">The argument of the event.</param>
        public Task RaiseAsync( IActivityMonitor monitor, TEvent e )
        {
            Task task = _parallelAsync.RaiseAsync( monitor, e );
            _seq.Raise( monitor, e );
            return Task.WhenAll( task, _seqAsync.RaiseAsync( monitor, e ) );
        }


    }
}
