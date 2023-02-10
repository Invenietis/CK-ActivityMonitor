using System.Diagnostics.CodeAnalysis;

namespace CK.Core
{
    /// <summary>
    /// Ultimate possible abstraction of a <see cref="IActivityMonitor"/>: it is context-less and can
    /// only log lines (not groups).
    /// <para>
    /// This unifies context-less loggers like <see cref="ActivityMonitor.StaticLogger"/> and regular
    /// contextual <see cref="ActivityMonitor"/>: filtered extension methods and any other extension
    /// methods that deals only with log lines uniformly apply to regular monitors and context-less loggers.
    /// </para>
    /// </summary>
    public interface IActivityLogger
    {
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
        /// Logs an already filtered line. 
        /// </summary>
        /// <param name="data">Data that describes the log. </param>
        void UnfilteredLog( ref ActivityMonitorLogData data );
    }
}
