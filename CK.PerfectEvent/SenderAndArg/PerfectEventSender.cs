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
    /// Instances of this class should be kept private: only the sender object should be able to call <see cref="RaiseAsync(IActivityMonitor, TSender, TArg)"/>
    /// or <see cref="SafeRaiseAsync(IActivityMonitor, TSender, TArg, string?, int)"/>.
    /// What should be exposed is the <see cref="PerfectEvent"/> property that restricts the API to event registration.
    /// </para>
    /// </summary>
    /// <typeparam name="TSender">The type of the event sender.</typeparam>
    /// <typeparam name="TArg">The type of the event argument.</typeparam>
    public class PerfectEventSender<TSender, TArg>
    {
        readonly SequentialEventHandlerSender<TSender, TArg> _seq;
        readonly SequentialEventHandlerAsyncSender<TSender, TArg> _seqAsync;
        readonly ParallelEventHandlerAsyncSender<TSender, TArg> _parallelAsync;

        /// <summary>
        /// Initializes a new <see cref="PerfectEventSender{TSender, TArg}"/>.
        /// </summary>
        public PerfectEventSender()
        {
            _seq = new SequentialEventHandlerSender<TSender, TArg>();
            _seqAsync = new SequentialEventHandlerAsyncSender<TSender, TArg>();
            _parallelAsync = new ParallelEventHandlerAsyncSender<TSender, TArg>();
        }

        /// <summary>
        /// Gets the event that should be exposed to the external world: through the <see cref="PerfectEvent{TSender, TArg}"/>,
        /// only registration/unregistration is possible.
        /// </summary>
        public PerfectEvent<TSender, TArg> PerfectEvent => new PerfectEvent<TSender, TArg>( this );

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
        /// Note that handlers of the 3 types can be added and removed directly to this <see cref="PerfectEventSender{TSender, TArg}"/>:
        /// this event is a helper that better express the intent of the code.
        /// </remarks>
        public event SequentialEventHandler<TSender, TArg> Sync
        {
            add => _seq.Add( value );
            remove => _seq.Remove( value );
        }

        /// <summary>
        /// Gets the Asynchronous event registration point.
        /// </summary>
        /// <remarks>
        /// Note that handlers of the 3 types can be added and removed directly to this <see cref="PerfectEventSender{TSender, TArg}"/>:
        /// this event is a helper that better express the intent of the code.
        /// </remarks>
        public event SequentialEventHandlerAsync<TSender, TArg> Async
        {
            add => _seqAsync.Add( value );
            remove => _seqAsync.Remove( value );
        }

        /// <summary>
        /// Gets the Parallel Asynchronous event registration point.
        /// </summary>
        /// <remarks>
        /// Note that handlers of the 3 types can be added and removed directly to this <see cref="PerfectEventSender{TSender, TArg}"/>:
        /// this event is a helper that better express the intent of the code.
        /// </remarks>
        public event ParallelEventHandlerAsync<TSender, TArg> ParallelAsync
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
        public PerfectEventSender<TSender, TArg> Add( SequentialEventHandler<TSender, TArg> handler )
        {
            _seq.Add( handler );
            return this;
        }

        /// <summary>
        /// Removes a handler if it exists. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">The handler to remove. Cannot be null.</param>
        /// <returns>This PerfectEventSender.</returns>
        public PerfectEventSender<TSender, TArg> Remove( SequentialEventHandler<TSender, TArg> handler )
        {
            _seq.Remove( handler );
            return this;
        }

        /// <summary>
        /// Relays to <see cref="Add(SequentialEventHandler{TSender, TArg})"/>.
        /// </summary>
        /// <param name="this">This PerfectEventSender.</param>
        /// <param name="handler">The non null handler to add.</param>
        /// <returns>This PerfectEventSender.</returns>
        public static PerfectEventSender<TSender, TArg> operator +( PerfectEventSender<TSender, TArg> @this, SequentialEventHandler<TSender, TArg> handler ) => @this.Add( handler );

        /// <summary>
        /// Relays to <see cref="Remove(SequentialEventHandler{TSender, TArg})"/>.
        /// </summary>
        /// <param name="this">The host.</param>
        /// <param name="handler">The non null handler to remove.</param>
        /// <returns>This PerfectEventSender.</returns>
        public static PerfectEventSender<TSender, TArg> operator -( PerfectEventSender<TSender, TArg> @this, SequentialEventHandler<TSender, TArg> handler ) => @this.Remove( handler );

        #endregion

        #region Sequential Async.

        /// <summary>
        /// Adds a handler. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">Non null handler.</param>
        /// <returns>This PerfectEventSender.</returns>
        public PerfectEventSender<TSender, TArg> Add( SequentialEventHandlerAsync<TSender, TArg> handler )
        {
            _seqAsync.Add( handler );
            return this;
        }

        /// <summary>
        /// Removes a handler if it exists. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">The handler to remove. Cannot be null.</param>
        /// <returns>This PerfectEventSender.</returns>
        public PerfectEventSender<TSender, TArg> Remove( SequentialEventHandlerAsync<TSender, TArg> handler )
        {
            _seqAsync.Remove( handler );
            return this;
        }

        /// <summary>
        /// Relays to <see cref="Add(SequentialEventHandlerAsync{TSender,TArg})"/>.
        /// </summary>
        /// <param name="this">This PerfectEventSender.</param>
        /// <param name="handler">The non null handler to add.</param>
        /// <returns>This PerfectEventSender.</returns>
        public static PerfectEventSender<TSender, TArg> operator +( PerfectEventSender<TSender, TArg> @this, SequentialEventHandlerAsync<TSender, TArg> handler ) => @this.Add( handler );

        /// <summary>
        /// Relays to <see cref="Remove(SequentialEventHandlerAsync{TSender,TArg})"/>.
        /// </summary>
        /// <param name="this">This PerfectEventSender.</param>
        /// <param name="handler">The non null handler to remove.</param>
        /// <returns>This PerfectEventSender.</returns>
        public static PerfectEventSender<TSender, TArg> operator -( PerfectEventSender<TSender, TArg> @this, SequentialEventHandlerAsync<TSender, TArg> handler ) => @this.Remove( handler );

        #endregion

        #region Parallel Async.

        /// <summary>
        /// Adds a handler. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">Non null handler.</param>
        /// <returns>This PerfectEventSender.</returns>
        public PerfectEventSender<TSender, TArg> Add( ParallelEventHandlerAsync<TSender, TArg> handler )
        {
            _parallelAsync.Add( handler );
            return this;
        }

        /// <summary>
        /// Removes a handler if it exists. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="handler">The handler to remove. Cannot be null.</param>
        /// <returns>This PerfectEventSender.</returns>
        public PerfectEventSender<TSender, TArg> Remove( ParallelEventHandlerAsync<TSender, TArg> handler )
        {
            _parallelAsync.Remove( handler );
            return this;
        }

        /// <summary>
        /// Relays to <see cref="Add(ParallelEventHandlerAsync{TSender, TArg})"/>.
        /// </summary>
        /// <param name="this">This PerfectEventSender.</param>
        /// <param name="handler">The non null handler to add.</param>
        /// <returns>This PerfectEventSender.</returns>
        public static PerfectEventSender<TSender, TArg> operator +( PerfectEventSender<TSender, TArg> @this, ParallelEventHandlerAsync<TSender, TArg> handler ) => @this.Add( handler );

        /// <summary>
        /// Relays to <see cref="Remove(ParallelEventHandlerAsync{TSender, TArg})"/>.
        /// </summary>
        /// <param name="this">This PerfectEventSender.</param>
        /// <param name="handler">The non null handler to remove.</param>
        /// <returns>This PerfectEventSender.</returns>
        public static PerfectEventSender<TSender, TArg> operator -( PerfectEventSender<TSender, TArg> @this, ParallelEventHandlerAsync<TSender, TArg> handler ) => @this.Remove( handler );


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
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The argument of the event.</param>
        /// <param name="fileName">The source filename where this event is raised.</param>
        /// <param name="lineNumber">The source line number in the filename where this event is raised.</param>
        /// <returns>True on success, false if an exception occurred.</returns>
        public async Task<bool> SafeRaiseAsync( IActivityMonitor monitor, TSender sender, TArg e, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0 )
        {
            try
            {
                Task task = _parallelAsync.RaiseAsync( monitor, sender, e );
                _seq.Raise( monitor, sender, e );
                await Task.WhenAll( task, _seqAsync.RaiseAsync( monitor, sender, e ) );
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
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The argument of the event.</param>
        public Task RaiseAsync( IActivityMonitor monitor, TSender sender, TArg e )
        {
            Task task = _parallelAsync.RaiseAsync( monitor, sender, e );
            _seq.Raise( monitor, sender, e );
            return Task.WhenAll( task, _seqAsync.RaiseAsync( monitor, sender, e ) );
        }


    }
}
