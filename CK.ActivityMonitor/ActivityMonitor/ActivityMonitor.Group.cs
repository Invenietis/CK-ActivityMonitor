using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CK.Core.Impl;

namespace CK.Core;

/// <summary>
/// Concrete implementation of <see cref="IActivityMonitor"/>.
/// </summary>
public sealed partial class ActivityMonitor
{
    /// <summary>
    /// Groups are bound to an <see cref="ActivityMonitor"/> and are linked together from 
    /// the current one to the very first one (a kind of stack).
    /// </summary>
    sealed class Group : IActivityLogGroup, IDisposableGroup
    {
        public readonly ActivityMonitor Monitor;
        public readonly Group? Parent;
        Group? _next;
        Func<string?>? _getConclusion;

        CKTrait _savedMonitorTags;
        LogFilter _savedMonitorFilter;
        bool _savedTrackStackTrace;
        ActivityMonitorLogData _data;
        DateTimeStamp _closeLogTime;
        bool _isOpen;
        bool _isRejected;

        internal Group( ActivityMonitor monitor, Group? parent )
        {
            _savedMonitorTags = ActivityMonitor.Tags.Empty;
            Monitor = monitor;
            Parent = parent;
        }

        internal Group? EnsureNext() => _next ??= new Group( Monitor, this );

        /// <summary>
        /// Initializes or reinitializes this group (if it has been disposed). 
        /// </summary>
        internal void Initialize( ref ActivityMonitorLogData data )
        {
            data.Freeze();
            _data = data;
            _savedMonitorFilter = Monitor._configuredFilter;
            _savedMonitorTags = Monitor._autoTags;
            _savedTrackStackTrace = Monitor._trackStackTrace;

            // Logs everything when a Group is a fatal or an error: we then have full details available without
            // requiring to log all with Error or Fatal level.
            if( data.MaskedLevel >= LogLevel.Error && Monitor._configuredFilter != LogFilter.Debug ) Monitor.DoSetConfiguredFilter( LogFilter.Debug );
            _closeLogTime = default;
            _isOpen = true;
            _isRejected = false;
        }

        /// <summary>
        /// Initializes (or reinitializes this group if it has been disposed) as a rejected group. 
        /// </summary>
        internal void InitializeRejectedGroup()
        {
            _data = default;
            _savedMonitorFilter = Monitor._configuredFilter;
            _savedMonitorTags = Monitor._autoTags;
            _savedTrackStackTrace = Monitor._trackStackTrace;
            _isOpen = true;
            _isRejected = true;
        }

        /// <summary>
        /// Gets whether the group is rejected.
        /// </summary>
        public bool IsRejectedGroup => _isRejected;

        /// <inheritdoc />
        public string? GetLogKeyString() => _isRejected ? null : _data.GetLogKeyString();

        /// <summary>
        /// Gets the log time of the group closing.
        /// <see cref="DateTimeStamp.IsKnown"/> is false until the group is closed.
        /// </summary>
        public DateTimeStamp CloseLogTime
        {
            get { return _closeLogTime; }
            internal set { _closeLogTime = value; }
        }

        IActivityLogGroup? IActivityLogGroup.Parent => Parent;

        public ref ActivityMonitorLogData Data => ref _data;

        public LogFilter SavedMonitorFilter => _savedMonitorFilter;

        /// <summary>
        /// Internal memory of whether the <see cref="Tags.StackTrace"/> tag exists in <see cref="IActivityMonitor.AutoTags"/>
        /// that will be restored when the group is closed.
        /// </summary>
        internal bool SavedTrackStackTrace => _savedTrackStackTrace;

        public CKTrait SavedMonitorTags => _savedMonitorTags;

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
            Monitor.ReentrantAndConcurrentCheck();
            try
            {
                if( _isOpen )
                {
                    while( Monitor._current != this )
                    {
                        Monitor.CloseGroup( null );
                    }
                    Monitor.CloseGroup( null );
                }
            }
            finally
            {
                Monitor.ReentrantAndConcurrentRelease();
            }
        }

        internal void AddGetConclusionText( ref List<ActivityLogGroupConclusion>? conclusions )
        {
            Throw.DebugAssert( _isOpen );
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

        internal void CloseGroup()
        {
            if( _savedMonitorFilter != Monitor._configuredFilter ) Monitor.DoSetConfiguredFilter( _savedMonitorFilter );
            Monitor._autoTags = _savedMonitorTags;
            Monitor._trackStackTrace = _savedTrackStackTrace;
            Monitor._current = Parent;
            _isOpen = false;
        }
    }
}
