namespace CK.Core
{
    /// <summary>
    /// Internal interface that allows ActivityMonitorBridgeTarget to call back
    /// the ActivityMonitorBridges that are bound to it.
    /// </summary>
    interface IActivityMonitorBridgeCallback
    {
        /// <summary>
        /// Gets whether this bridge updates the Topic and AutoTags of its monitor whenever they change on the target monitor.
        /// </summary>
        bool PullTopicAndAutoTagsFromTarget { get; }

        /// <summary>
        /// Called when the target filter changed or is dirty. This can be called on any thread.
        /// </summary>
        void OnTargetActualFilterChanged();

        /// <summary>
        /// Called when the target AutoTags changed.
        /// </summary>
        void OnTargetAutoTagsChanged( CKTrait newTags );

        /// <summary>
        /// Called when the target Topic changed.
        /// </summary>
        /// <param name="newTopic">The new topic.</param>
        /// <param name="fileName">File name of the caller.</param>
        /// <param name="lineNumber">Line number in the caller's file.</param>
        void OnTargetTopicChanged( string newTopic, string? fileName, int lineNumber );
    }
}
