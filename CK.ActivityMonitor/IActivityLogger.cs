using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CK.Core
{

    /// <summary>
    /// Ultimate possible abstraction of <see cref="IActivityMonitor"/> and <see cref="IParallelLogger"/>:
    /// it is context-less and can only log lines (not groups), there is no local <see cref="IActivityMonitor.Output"/>
    /// and no <see cref="IParallelLogger.CreateDependentToken(string?, string?, string?, int)"/> capability.
    /// </summary>
    public interface IActivityLogger
    {
        /// <summary>
        /// Gets the unique identifier for this logger or monitor.
        /// </summary>
        string UniqueId { get; }

        /// <summary>
        /// Gets the tags that will be combined to the logged ones before filtering
        /// by <see cref="ActivityMonitorExtension.ShouldLogLine(IActivityLogger, LogLevel, CKTrait?, out CKTrait)"/>
        /// or by sender with interpolated string handlers.
        /// </summary>
        CKTrait AutoTags { get; }

        /// <summary>
        /// Gets the line filter level to apply.
        /// </summary>
        LogLevelFilter ActualFilter { get; }

        /// <summary>
        /// Low level factory of log data.
        /// </summary>
        ActivityMonitorLogData.IFactory DataFactory { get; }

        /// <summary>
        /// Sends a line of logs regardless of any filter. 
        /// </summary>
        /// <param name="data">Data that describes the log.</param>
        void UnfilteredLog( ref ActivityMonitorLogData data );
    }
}
