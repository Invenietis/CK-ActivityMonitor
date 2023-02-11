using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CK.Core.Impl;

namespace CK.Core
{
    /// <summary>
    /// Concrete implementation of <see cref="IActivityMonitor"/>.
    /// </summary>
    public partial class ActivityMonitor
    {
        /// <summary>
        /// Groups are bound to an <see cref="ActivityMonitor"/> and are linked together from 
        /// the current one to the very first one (a kind of stack).
        /// </summary>
        protected sealed class Group : IActivityLogGroup, IDisposableGroup
        {
            /// <summary>
            /// The monitor that owns this group.
            /// </summary>
            public readonly ActivityMonitor Monitor;

            /// <summary>
            /// The raw index of the group. 
            /// </summary>
            public readonly int Index;

            Func<string?>? _getConclusion;

            CKTrait _savedMonitorTags;
            Group? _unfilteredParent;
            int _depth;
            DateTimeStamp _closeLogTime;
            bool _isOpen;
            ActivityMonitorLogData _data;

            /// <summary>
            /// Initialized a new Group at a given index.
            /// </summary>
            /// <param name="monitor">Monitor.</param>
            /// <param name="index">Index of the group.</param>
            internal Group( ActivityMonitor monitor, int index )
            {
                _savedMonitorTags = ActivityMonitor.Tags.Empty;
                Monitor = monitor;
                Index = index;
            }

            /// <summary>
            /// Initializes or reinitializes this group (if it has been disposed). 
            /// </summary>
            internal void Initialize( ref ActivityMonitorLogData data )
            {
                _data = data;

                SavedMonitorFilter = Monitor._configuredFilter;
                SavedMonitorTags = Monitor._autoTags;
                SavedTrackStackTrace = Monitor._trackStackTrace;
                if( (_unfilteredParent = Monitor._currentUnfiltered) != null ) _depth = _unfilteredParent._depth + 1;
                else _depth = 1;
                // Logs everything when a Group is a fatal or an error: we then have full details available without
                // requiring to log all with Error or Fatal level.
                if( data.MaskedLevel >= LogLevel.Error && Monitor._configuredFilter != LogFilter.Debug ) Monitor.DoSetConfiguredFilter( LogFilter.Debug );
                _closeLogTime = default;
                _isOpen = true;
            }

            /// <summary>
            /// Initializes (or reinitializes this group if it has been disposed) as a rejected group. 
            /// </summary>
            internal void InitializeRejectedGroup()
            {
                _data = default;
                SavedMonitorFilter = Monitor._configuredFilter;
                SavedMonitorTags = Monitor._autoTags;
                SavedTrackStackTrace = Monitor._trackStackTrace;
                _unfilteredParent = Monitor._currentUnfiltered;
                _depth = 0;
                _isOpen = true;
            }

            /// <summary>
            /// Gets whether the group is rejected.
            /// </summary>
            public bool IsRejectedGroup => _depth == 0;

            /// <summary>
            /// Gets the log time of the group closing.
            /// <see cref="DateTimeStamp.IsKnown"/> is false until the group is closed.
            /// </summary>
            public DateTimeStamp CloseLogTime
            {
                get { return _closeLogTime; }
                internal set { _closeLogTime = value; }
            }

            /// <summary>
            /// Get the previous group in its origin monitor. Null if this group is a top level group.
            /// </summary>
            public IActivityLogGroup? Parent => _unfilteredParent;

            /// <summary>
            /// Gets the depth of this group in its origin monitor (1 for top level groups).
            /// </summary>
            public int Depth => _depth;

            /// <summary>
            /// Gets the log data itself.
            /// </summary>
            public ref ActivityMonitorLogData Data => ref _data;

            /// <summary>
            /// Gets or sets the <see cref="IActivityMonitor.MinimalFilter"/> that will be restored when group will be closed.
            /// Initialized with the current value of IActivityMonitor.Filter when the group has been opened.
            /// </summary>
            public LogFilter SavedMonitorFilter { get; private set; }

            /// <summary>
            /// Internal memory of whether the <see cref="Tags.StackTrace"/> tag exists in <see cref="IActivityMonitor.AutoTags"/>
            /// that will be restored when the group is closed.
            /// </summary>
            internal bool SavedTrackStackTrace { get; private set; }

            /// <summary>
            /// Gets or sets the <see cref="IActivityMonitor.AutoTags"/> that will be restored when group will be closed.
            /// Initialized with the current value of IActivityMonitor.Tags when the group has been opened.
            /// </summary>
            public CKTrait SavedMonitorTags
            {
                get => _savedMonitorTags;
                private set => _savedMonitorTags = value;
            }

            IDisposable IDisposableGroup.ConcludeWith( Func<string?> getConclusionText )
            {
                bool isNotReentrant = Monitor.ConcurrentOnlyCheck();
                try
                {
                    if( !IsRejectedGroup ) _getConclusion = getConclusionText;
                }
                finally
                {
                    if( isNotReentrant ) Monitor.ReentrantAndConcurrentRelease();
                }
                return this;
            }

            /// <summary>
            /// Ensures that any groups opened after this one are closed before closing this one.
            /// </summary>
            void IDisposable.Dispose()
            {
                bool isNotReentrant = Monitor.ConcurrentOnlyCheck();
                try
                {
                    if( _isOpen )
                    {
                        Group? g = Monitor._current;
                        while( g != this )
                        {
                            Debug.Assert( g != null, "The current group cannot be null (or this object would have been already disposed)." );
                            ((IDisposable)g).Dispose();
                            g = Monitor._current;
                        }
                        Monitor.CloseGroup( null );
                    }
                }
                finally
                {
                    if( isNotReentrant ) Monitor.ReentrantAndConcurrentRelease();
                }
            }

            internal void GroupClosing( ref List<ActivityLogGroupConclusion>? conclusions )
            {
                Debug.Assert( _isOpen );
                if( _getConclusion != null )
                {
                    string? auto = null;
                    try
                    {
                        auto = _getConclusion();
                    }
                    catch( Exception ex )
                    {
                        auto = String.Format( Impl.ActivityMonitorResources.ActivityMonitorErrorWhileGetConclusionText, ex.Message );
                    }
                    _getConclusion = null;
                    if( auto != null )
                    {
                        conclusions ??= new List<ActivityLogGroupConclusion>();
                        conclusions.Add( new ActivityLogGroupConclusion( ActivityMonitor.Tags.GetTextConclusion, auto ) );
                    }
                }
            }

            internal void GroupClosed() => _isOpen = false;
        }

        IActivityLogGroup? IActivityMonitorImpl.CurrentGroup => _current;

        /// <summary>
        /// Gets the currently opened group.
        /// Null when no group is currently opened.
        /// </summary>
        protected IActivityLogGroup? CurrentGroup => _current;

    }
}
