using System;
using System.Threading.Tasks;

namespace CK.Core
{
    /// <summary>
    /// Simple abstraction of a worker with its own monitor that
    /// can execute synchronous or asynchronous actions and offers
    /// simple log methods (more complex logging can be done via
    /// the <see cref="Execute(Action{IActivityMonitor})"/> or
    /// <see cref="Execute(Func{IActivityMonitor, Task})"/> methods).
    /// <para>
    /// Note that the simple log methods here don't open/close groups and this
    /// is normal: the worker is free to interleave any workload between consecutive
    /// calls from this interface: structured groups have little chance to really be
    /// structured.
    /// </para>
    /// </summary>
    public interface IMonitoredWorker : IActivityLogger
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
    }
}
