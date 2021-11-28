using System;
using System.Threading.Tasks;

namespace CK.Core
{
    /// <summary>
    /// Simple abstraction of a worker with its own monitor that
    /// can execute synchronous or asynchronous actions and offers
    /// simple log methods (more complex logging can be done via
    /// the <see cref="Execute(Action{IActivityMonitor})"/> method).
    /// <para>
    /// Note that the simple log methods here don't open/close groups and this
    /// is normal: the worker is free to interleave any workload between consecutive
    /// calls from this interface: structured groups have little chance to really be
    /// structured.
    /// </para>
    /// </summary>
    public interface IMonitoredWorker
    {
        /// <summary>
        /// Posts the given synchronous action to be executed by this worker.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        void Execute( Action<IActivityMonitor> action );

        /// <summary>
        /// Posts the given asynchronous action to be executed by this worker.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        void Execute( Func<IActivityMonitor, Task> action );

        /// <summary>
        /// Posts an error log message into this worker monitor.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        void LogError( string msg );

        /// <summary>
        /// Posts an error log message with an exception into this worker monitor.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="ex">The exception to log.</param>
        void LogError( string msg, Exception ex );

        /// <summary>
        /// Posts a warning log message into this worker monitor.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        void LogWarn( string msg );

        /// <summary>
        /// Posts a warning log message with an exception into this worker monitor.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="ex">The exception to log.</param>
        void LogWarn( string msg, Exception ex );

        /// <summary>
        /// Posts an informational message log into this worker monitor.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        void LogInfo( string msg );

        /// <summary>
        /// Posts a trace log message into this worker monitor.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        void LogTrace( string msg );

        /// <summary>
        /// Posts a debug log message this worker event monitor.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        void LogDebug( string msg );
    }
}
