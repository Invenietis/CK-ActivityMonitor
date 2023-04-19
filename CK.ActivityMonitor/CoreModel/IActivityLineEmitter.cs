using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CK.Core
{

    /// <summary>
    /// Ultimate possible abstraction of <see cref="IActivityMonitor"/>, <see cref="IParallelLogger"/> and <see cref="IStaticLogger"/>.
    /// <para>
    /// This should not be used directly: this is intended to support commonly used are extension methods.
    /// </para>
    /// </summary>
    public interface IActivityLineEmitter
    {
        /// <summary>
        /// Gets the tags that will be combined to the logged ones before filtering
        /// by <see cref="ActivityMonitorExtension.ShouldLogLine(IActivityLineEmitter, LogLevel, CKTrait?, out CKTrait)"/>
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
