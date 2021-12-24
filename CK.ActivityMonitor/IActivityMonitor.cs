using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CK.Core
{

    /// <summary>
    /// Defines the core Activity Monitor interface. Small is beautiful. 
    /// </summary>
    /// <remarks>
    /// This is not the same as a classical logging framework: the "activity" captures by an activity monitor is structured. 
    /// It can be seen as a "Story Writer": its output can be displayed to an end user (even if some structured information 
    /// can easily be injected).
    /// Furthermore, activities can be tracked (with the help of the developer of course and the <see cref="ActivityMonitor.DependentToken"/>)
    /// across threads, tasks or application domain.
    /// </remarks>
    public interface IActivityMonitor
    {
        /// <summary>
        /// Gets the unique identifier for this monitor.
        /// </summary>
        string UniqueId { get; }

        /// <summary>
        /// Gets or sets the tags of this monitor: any subsequent logs will be tagged by these tags.
        /// The <see cref="CKTrait"/> must be registered in <see cref="ActivityMonitor.Tags"/>.
        /// Modifications to this property are scoped to the current Group since when a Group is closed, this
        /// property (and <see cref="MinimalFilter"/>) is automatically restored to its original value (captured when the Group was opened).
        /// <para>
        /// This property can be set with overloaded operators += (same as |= to call <see cref="CKTrait.Union(CKTrait)"/>), -= (<see cref="CKTrait.Except(CKTrait)"/>),
        /// &amp;= (<see cref="CKTrait.Intersect(CKTrait)"/>) or ^= (<see cref="CKTrait.SymmetricExcept(CKTrait)"/>).
        /// </para>
        /// </summary>
        [AllowNull]
        CKTrait AutoTags { get; set; }

        /// <summary>
        /// Gets or sets a minimal filter for the log level that contributes to the value of the <see cref="ActualFilter"/>.
        /// Modifications to this property are scoped to the current Group since when a Group is closed, this
        /// property (and <see cref="AutoTags"/>) is automatically restored to its original value (captured when the Group was opened).
        /// Defaults to <see cref="LogFilter.Undefined"/>.
        /// </summary>
        LogFilter MinimalFilter { get; set; }
        
        /// <summary>
        /// Gets the actual filter level for logs: this combines the configured <see cref="MinimalFilter"/> and the minimal requirements
        /// of any <see cref="IActivityMonitorBoundClient"/> that specifies such a minimal filter level.
        /// <remarks>
        /// This does NOT take into account the static (application-domain) <see cref="ActivityMonitor.DefaultFilter"/>.
        /// This global default must be used if this ActualFilter is <see cref="LogLevelFilter.None"/> for <see cref="LogFilter.Line"/> or <see cref="LogFilter.Group"/>: 
        /// the <see cref="ActivityMonitorExtension.ShouldLogLine">ShouldLog</see> extension method takes it into account.
        /// </remarks>
        /// </summary>
        LogFilter ActualFilter { get; }

        /// <summary>
        /// Gets the current topic for this monitor. This can be any non null string (null topic is mapped to the empty string) that describes
        /// the current activity. It must be set with <see cref="SetTopic"/> and unlike <see cref="MinimalFilter"/> and <see cref="AutoTags"/>, 
        /// the topic is not reseted when groups are closed.
        /// </summary>
        /// <remarks>
        /// Clients are warned of the change thanks to <see cref="IActivityMonitorClient.OnTopicChanged"/> and an unfiltered <see cref="LogLevel.Info"/> log 
        /// with the new topic prefixed with "Topic:" and tagged with <see cref="ActivityMonitor.Tags.MonitorTopicChanged"/> is emitted.
        /// </remarks>
        string Topic { get; }

        /// <summary>
        /// Sets the current topic for this monitor. This can be any non null string (null topic is mapped to the empty string) that describes
        /// the current activity.
        /// </summary>
        /// <param name="fileName">The source code file name from which the topic is set.</param>
        /// <param name="lineNumber">The line number in the source from which the topic is set.</param>
        /// <param name="newTopic">The new topic string to associate to this monitor.</param>
        void SetTopic( string newTopic, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0 );

        /// <summary>
        /// Logs a line regardless of <see cref="ActualFilter"/> level (except for <see cref="LogLevelFilter.Off"/>). 
        /// </summary>
        /// <param name="data">Data that describes the log. </param>
        void UnfilteredLog( ref ActivityMonitorLogData data );

        /// <summary>
        /// Opens a group regardless of <see cref="ActualFilter"/> level (except for <see cref="LogLevelFilter.Off"/>). 
        /// The group is open even if <paramref name="data"/> is null or its <see cref="ActivityMonitorLogData.MaskedLevel"/>
        /// is <see cref="LogLevel.None"/>: either <see cref="CloseGroup"/> must be called and/or the returned object must
        /// be disposed (both can be called on the same group: when the group is closed with CloseGroup, the dispose action is
        /// ignored).
        /// </summary>
        /// <param name="data">
        /// Data that describes the log. When null or when <see cref="ActivityMonitorLogData.MaskedLevel"/> 
        /// is <see cref="LogLevel.None"/> a rejected group is recorded and returned and must be closed.
        /// </param>
        /// <returns>A disposable object that can be used to set a function that provides a conclusion text and/or close the group.</returns>
        /// <remarks>
        /// <para>
        /// Opening a group does not change the current <see cref="MinimalFilter"/>, except when opening a <see cref="LogLevel.Fatal"/> or <see cref="LogLevel.Error"/> group:
        /// in such case, the MinimalFilter is automatically sets to <see cref="LogFilter.Debug"/> to capture all potential information inside the error group.
        /// </para>
        /// <para>
        /// Changes to the monitor's current Filter or AutoTags that occur inside a group are automatically restored to their original values when the group is closed.
        /// This behavior guaranties that a local modification (deep inside unknown called code) does not impact caller code: groups are a way to easily isolate such 
        /// configuration changes.
        /// </para>
        /// <para>
        /// Note that this automatic configuration restoration works even if the group has been filtered.
        /// </para>
        /// </remarks>
        IDisposableGroup UnfilteredOpenGroup( ref ActivityMonitorLogData data );

        /// <summary>
        /// Closes the current Group. Optional parameter is polymorphic. It can be a string, a <see cref="ActivityLogGroupConclusion"/>, 
        /// a <see cref="List{T}"/> or an <see cref="IEnumerable{T}"/> of ActivityLogGroupConclusion, or any object with an overridden <see cref="Object.ToString"/> method. 
        /// See remarks (especially for List&lt;ActivityLogGroupConclusion&gt;).
        /// </summary>
        /// <param name="userConclusion">
        /// Optional string, ActivityLogGroupConclusion object, enumerable of ActivityLogGroupConclusion or object to
        /// conclude the group. See remarks.
        /// </param>
        /// <param name="explicitLogTime">
        /// Optional log time to be used typically when replaying logs.
        /// </param>
        /// <returns>True if a group has actually been closed, false if there is no more opened group.</returns>
        /// <remarks>
        /// An untyped object is used here to easily and efficiently accommodate both string and already existing ActivityLogGroupConclusion.
        /// When a List&lt;ActivityLogGroupConclusion&gt; is used, it will be directly used to collect conclusion objects (new conclusions will be added to it). This is an optimization.
        /// </remarks>
        bool CloseGroup( object? userConclusion = null, DateTimeStamp explicitLogTime = default );

        /// <summary>
        /// Gets the <see cref="IActivityMonitorOutput"/> for this monitor.
        /// </summary>
        IActivityMonitorOutput Output { get; }

        /// <summary>
        /// Gets the last <see cref="DateTimeStamp"/> for this monitor.
        /// </summary>
        DateTimeStamp LastLogTime { get; }
    }

}
