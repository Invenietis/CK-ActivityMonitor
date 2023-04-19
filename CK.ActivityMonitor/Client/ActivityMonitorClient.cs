using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CK.Core
{
    /// <summary>
    /// Base class that explicitly implements <see cref="IActivityMonitorClient"/> (to hide it from public interface, except its <see cref="MinimalFilter"/>).
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ActivityMonitorClient : IActivityMonitorClient
    {
        /// <summary>
        /// Empty <see cref="IActivityMonitorClient"/> (null object design pattern).
        /// </summary>
        public static readonly ActivityMonitorClient Empty = new ActivityMonitorClient();

        /// <summary>
        /// Initialize a new <see cref="ActivityMonitorClient"/> that does nothing.
        /// </summary>
        public ActivityMonitorClient()
        {
        }

        /// <summary>
        /// Gets the minimal log level that this Client expects: defaults to <see cref="LogFilter.Undefined"/>.
        /// </summary>
        public virtual LogFilter MinimalFilter { get { return LogFilter.Undefined; } }

        /// <summary>
        /// Called for each <see cref="IActivityLineEmitter.UnfilteredLog"/>. Does nothing by default.
        /// The <see cref="ActivityMonitorLogData.Exception"/> is always null since exceptions
        /// are carried by groups.
        /// </summary>
        /// <param name="data">Log data.</param>
        protected virtual void OnUnfilteredLog( ref ActivityMonitorLogData data )
        {
        }

        /// <summary>
        /// Called for each <see cref="IActivityMonitor.UnfilteredOpenGroup"/>.
        /// Does nothing by default.
        /// </summary>
        /// <param name="group">The newly opened <see cref="IActivityLogGroup"/>.</param>
        protected virtual void OnOpenGroup( IActivityLogGroup group )
        {
        }

        /// <summary>
        /// Called once the user conclusions are known at the group level but before 
        /// the group is actually closed: clients can update the conclusions for the group.
        /// Does nothing by default.
        /// </summary>
        /// <param name="group">The closing group.</param>
        /// <param name="conclusions">
        /// Mutable conclusions associated to the closing group. 
        /// This can be null if no conclusions have been added yet. 
        /// It is up to the first client that wants to add a conclusion to instantiate a new List object to carry the conclusions.
        /// </param>
        protected virtual void OnGroupClosing( IActivityLogGroup group, ref List<ActivityLogGroupConclusion>? conclusions )
        {
        }

        /// <summary>
        /// Called when the group is actually closed.
        /// Does nothing by default.
        /// </summary>
        /// <param name="group">The closed group.</param>
        /// <param name="conclusions">Text that conclude the group.</param>
        protected virtual void OnGroupClosed( IActivityLogGroup group, IReadOnlyList<ActivityLogGroupConclusion> conclusions )
        {
        }

        /// <summary>
        /// Called when <see cref="IActivityMonitor.Topic"/> changed.
        /// Does nothing by default.
        /// </summary>
        /// <param name="newTopic">The new topic.</param>
        /// <param name="fileName">Source file name where <see cref="IActivityMonitor.SetTopic"/> has been called.</param>
        /// <param name="lineNumber">Source line number where IActivityMonitor.SetTopic has been called.</param>
        protected virtual void OnTopicChanged( string newTopic, string? fileName, int lineNumber )
        {
        }

        /// <summary>
        /// Called when <see cref="IActivityMonitor.AutoTags"/> changed.
        /// Does nothing by default.
        /// </summary>
        /// <param name="newTags">The new auto tags.</param>
        protected virtual void OnAutoTagsChanged( CKTrait newTags )
        {
        }

        /// <summary>
        /// Throws a standardized exception. Can be called from <see cref="IActivityMonitorBoundClient.SetMonitor"/>.
        /// </summary>
        /// <param name="boundClient">The bound client.</param>
        static public void ThrowMultipleRegisterOnBoundClientException( IActivityMonitorBoundClient boundClient )
        {
            throw new InvalidOperationException( String.Format( Impl.ActivityMonitorResources.ActivityMonitorBoundClientMultipleRegister, boundClient != null ? boundClient.GetType().FullName : String.Empty ) );
        }

        /// <summary>
        /// Throws standardized exception. Can be called by <see cref="IActivityMonitorBoundClient.SetMonitor"/>.
        /// </summary>
        /// <param name="boundClient">The bound client.</param>
        static public void ThrowBoundClientIsLockedException( IActivityMonitorBoundClient boundClient )
        {
            throw new InvalidOperationException( String.Format( Impl.ActivityMonitorResources.ActivityMonitorBoundClientIsLocked, boundClient != null ? boundClient.GetType().FullName : String.Empty ) );
        }

        #region IActivityMonitorClient Members

        void IActivityMonitorClient.OnUnfilteredLog( ref ActivityMonitorLogData data )
        {
            OnUnfilteredLog( ref data );
        }

        void IActivityMonitorClient.OnOpenGroup( IActivityLogGroup group )
        {
            OnOpenGroup( group );
        }

        void IActivityMonitorClient.OnGroupClosing( IActivityLogGroup group, ref List<ActivityLogGroupConclusion>? conclusions )
        {
            OnGroupClosing( group, ref conclusions );
        }

        void IActivityMonitorClient.OnGroupClosed( IActivityLogGroup group, IReadOnlyList<ActivityLogGroupConclusion> conclusions )
        {
            OnGroupClosed( group, conclusions );
        }

        void IActivityMonitorClient.OnTopicChanged( string newTopic, string? fileName, int lineNumber )
        {
            OnTopicChanged( newTopic, fileName, lineNumber );
        }

        void IActivityMonitorClient.OnAutoTagsChanged( CKTrait newTags )
        {
            OnAutoTagsChanged( newTags );
        }

        #endregion

    }
}
