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
        /// Creates a <see cref="ActivityMonitorLogData"/>. If <paramref name="text"/> is null or empty
        /// the text is set to the exception's message or to <see cref="ActivityMonitor.NoLogText"/>.
        /// <para>
        /// This is a low level function that is used by extension methods, it should never be used directly.
        /// When a data is created, it MUST be sent to the monitor or logger as quickly as possible.
        /// </para>
        /// </summary>
        /// <param name="level">The log level that may be flagged with <see cref="LogLevel.IsFiltered"/> or not.</param>
        /// <param name="finalTags">The final tags that should already be combined with the source <see cref="IActivityLineEmitter.AutoTags"/>.</param>
        /// <param name="text">The text.</param>
        /// <param name="exception">Optional <see cref="Exception"/> or <see cref="CKExceptionData"/> (any other type throws an <see cref="ArgumentException"/>).</param>
        /// <param name="fileName">Source file name of the log.</param>
        /// <param name="lineNumber">Source line number of the log.</param>
        /// <param name="isOpenGroup">True if this log opens a group.</param>
        /// <returns>The ready to send data.</returns>
        ActivityMonitorLogData CreateActivityMonitorLogData( LogLevel level,
                                                             CKTrait finalTags,
                                                             string? text,
                                                             object? exception,
                                                             string? fileName,
                                                             int lineNumber,
                                                             bool isOpenGroup );

        /// <summary>
        /// Sends a line of logs regardless of any filter. 
        /// </summary>
        /// <param name="data">Data that describes the log.</param>
        void UnfilteredLog( ref ActivityMonitorLogData data );
    }
}
