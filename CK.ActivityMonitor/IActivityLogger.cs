using System.Diagnostics.CodeAnalysis;

namespace CK.Core
{
    /// <summary>
    /// Ultimate possible abstraction of a <see cref="IActivityMonitor"/>: it is context-less and can
    /// only log lines (not groups), there is no local <see cref="IActivityMonitor.Output"/>.
    /// <para>
    /// This unifies context-less loggers like <see cref="ActivityMonitor.StaticLogger"/> and regular
    /// contextual <see cref="ActivityMonitor"/>: filtered extension methods and any other extension
    /// methods that deals only with log lines uniformly apply to regular monitors and context-less loggers.
    /// </para>
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
        /// Logs a text regardless of any filter (except for <see cref="LogLevelFilter.Off"/>). 
        /// Each call to log is considered as a unit of text: depending on the rendering engine, a line or a 
        /// paragraph separator (or any appropriate separator) should be appended between each text if 
        /// the level is the same as the previous one.
        /// See remarks.
        /// </summary>
        /// <param name="data">
        /// Data that describes the log. When <see cref="ActivityMonitorLogData.MaskedLevel"/> 
        /// is <see cref="LogLevel.None"/>, nothing happens (whereas for group, a rejected group is recorded and returned).
        /// </param>
        /// <remarks>
        /// A null or empty <see cref="ActivityMonitorLogData.Text"/> is logged as <see cref="ActivityMonitor.NoLogText"/>.
        /// If needed, the special text <see cref="ActivityMonitor.ParkLevel"/> ("PARK-LEVEL") breaks the current <see cref="LogLevel"/>
        /// and resets it: the next log, even with the same LogLevel, should be treated as if a different LogLevel is used.
        /// </remarks>
        void UnfilteredLog( ref ActivityMonitorLogData data );

        /// <summary>
        /// Returns a valid <see cref="DateTimeStamp"/> that can be used for a log: it is based on <see cref="DateTime.UtcNow"/> and has 
        /// a <see cref="DateTimeStamp.Uniquifier"/> that will not be changed when emitting the next log.
        /// <para>
        /// This uses the <see cref="DateTimeStampProvider"/> or an internal, lock-free, current value. In both cases, this
        /// value is ever increasing.
        /// </para>
        /// </summary>
        /// <returns>The next stamp time to use for this logger or monitor.</returns>
        DateTimeStamp GetAndUpdateNextLogTime();
    }
}
