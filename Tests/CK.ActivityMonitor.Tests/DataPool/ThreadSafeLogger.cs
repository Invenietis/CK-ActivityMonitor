using System.Diagnostics;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CK.Core.Tests.Monitoring
{

    /// <summary>
    /// Demo of a thread safe, non blocking logger without thread.
    /// <para>
    /// This logger is a pattern rather than an actual helper. In practice such beast offer
    /// other services than being only a IActivityLogger, they usually are "Loops" or "Micro Agents"
    /// that handle sync-to-async calls or background processes.
    /// </para>
    /// </summary>
    public sealed class ThreadSafeLogger : IActivityLogger
    {
        // The monitor of this Logger.
        readonly ActivityMonitor _monitor;
        // We use a nullable ActivityMonitorExternalLogData to signal the Stop here
        // (no need for a cancellation token source).
        readonly Channel<ActivityMonitorExternalLogData?> _channel;

        /// <summary>
        /// Initializes a new Logger with a name (that is the <see cref="IActivityMonitor.Topic"/>.
        /// </summary>
        /// <param name="name">This logger's name.</param>
        public ThreadSafeLogger( string name )
        {
            // We need the monitor to have a thread safe DateTimeStampProvider.
            // This guaranties that LogTime of all logs received by this logger
            // are ever increasing and unique.
            _monitor = new ActivityMonitor( name, new DateTimeStampProvider() );
            _channel = Channel.CreateUnbounded<ActivityMonitorExternalLogData?>( new UnboundedChannelOptions { SingleReader = true } );
            Stopped = Task.Run( RunAsync );
        }

        /// <summary>
        /// Gets the internal monitor used by this logger.
        /// </summary>
        public IActivityMonitor InternalMonitor => _monitor;

        /// <summary>
        /// Stops this logger: no more logs will be received.
        /// </summary>
        /// <returns>True if this is this call that stopped this logger, false if it has already been closed.</returns>
        public bool Stop()
        {
            // Writes the null sentinel to the channel and completes the channel.
            // There's a race condition here: an entry may be written after the null sentinel
            // but before the completion. This is why the RunAsync must drain the channel 
            // to release all the entries.
            return _channel.Writer.TryWrite( null ) && _channel.Writer.TryComplete();
        }

        /// <summary>
        /// Gets a task that is completed when this Logger is stopped.
        /// </summary>
        public Task Stopped { get; private set; }

        /// <summary>
        /// The traits are the one of the monitor.
        /// </summary>
        CKTrait IActivityLogger.AutoTags => _monitor.AutoTags;

        /// <summary>
        /// The filter is the one of the filter.
        /// </summary>
        LogLevelFilter IActivityLogger.ActualFilter => _monitor.ActualFilter.Line;

        /// <summary>
        /// Sends the log to the monitor.
        /// </summary>
        /// <param name="data"></param>
        public void UnfilteredLog( ref ActivityMonitorLogData data )
        {
            // Acquires the data and if the channel is completed, release
            // it immediately.
            Debug.Assert( _monitor.SafeStampProvider != null, "Using the stamp provider of the monitor." );
            var e = data.AcquireExternalData( _monitor.SafeStampProvider );
            if( !_channel.Writer.TryWrite( e ) )
            {
                e.Release();
            }
        }

        async Task RunAsync()
        {
            // We pool the channel until the null closing signal.
            ActivityMonitorExternalLogData? data;
            while( (data = await _channel.Reader.ReadAsync()) != null )
            {
                var d = new ActivityMonitorLogData( data );
                _monitor.UnfilteredLog( ref d );
                // If the data has been acquired again by Clients, it will
                // live longer, but for us, we are done with it.
                data.Release();
            }
            // Securing the race condition described in Stop().
            while( _channel.Reader.TryRead( out data ) )
            {
                // We "may" receive duplicate null closing signal.
                data?.Release();
            }
            _monitor.MonitorEnd();
        }
    }
}
