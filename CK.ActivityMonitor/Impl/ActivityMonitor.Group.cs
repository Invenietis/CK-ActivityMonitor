#region LGPL License
/*----------------------------------------------------------------------------
* This file (CK.Core\ActivityMonitor\Impl\ActivityMonitor.Group.cs) is part of CiviKey. 
*  
* CiviKey is free software: you can redistribute it and/or modify 
* it under the terms of the GNU Lesser General Public License as published 
* by the Free Software Foundation, either version 3 of the License, or 
* (at your option) any later version. 
*  
* CiviKey is distributed in the hope that it will be useful, 
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
* GNU Lesser General Public License for more details. 
* You should have received a copy of the GNU Lesser General Public License 
* along with CiviKey.  If not, see <http://www.gnu.org/licenses/>. 
*  
* Copyright © 2007-2015, 
*     Invenietis <http://www.invenietis.com>,
*     In’Tech INFO <http://www.intechinfo.fr>,
* All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
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
            static readonly ActivityMonitorGroupData _rejectedGroupDataInstance = new ActivityMonitorGroupData();

            /// <summary>
            /// The monitor that owns this group.
            /// </summary>
            public readonly ActivityMonitor Monitor;

            /// <summary>
            /// The raw index of the group. 
            /// </summary>
            public readonly int Index;

            ActivityMonitorGroupData? _data;
            DateTimeStamp _closeLogTime;
            Group? _unfilteredParent;
            int _depth;

            /// <summary>
            /// Initialized a new Group at a given index.
            /// </summary>
            /// <param name="monitor">Monitor.</param>
            /// <param name="index">Index of the group.</param>
            internal Group( ActivityMonitor monitor, int index )
            {
                Monitor = monitor;
                Index = index;
            }

            /// <summary>
            /// Initializes or reinitializes this group (if it has been disposed). 
            /// </summary>
            internal void Initialize( ActivityMonitorGroupData data )
            {
                SavedMonitorFilter = Monitor._configuredFilter;
                SavedMonitorTags = Monitor._currentTag;
                SavedTrackStackTrace = Monitor._trackStackTrace;
                if( (_unfilteredParent = Monitor._currentUnfiltered) != null ) _depth = _unfilteredParent._depth + 1;
                else _depth = 1;
                // Logs everything when a Group is a fatal or an error: we then have full details available without
                // requiring to log all with Error or Fatal level.
                if( data.MaskedLevel >= LogLevel.Error && Monitor._configuredFilter != LogFilter.Debug ) Monitor.DoSetConfiguredFilter( LogFilter.Debug );
                _closeLogTime = DateTimeStamp.MinValue;
                _data = data;
            }

            /// <summary>
            /// Initializes (or reinitializes this group if it has been disposed) as a rejected group. 
            /// </summary>
            internal void InitializeRejectedGroup()
            {
                SavedMonitorFilter = Monitor._configuredFilter;
                SavedMonitorTags = Monitor._currentTag;
                SavedTrackStackTrace = Monitor._trackStackTrace;
                _unfilteredParent = Monitor._currentUnfiltered;
                _depth = 0;
                _data = _rejectedGroupDataInstance;
            }

            /// <summary>
            /// Gets whether the group is rejected.
            /// </summary>
            public bool IsRejectedGroup => _data == _rejectedGroupDataInstance;

            /// <summary>
            /// Gets the tags for the log group.
            /// </summary>
            public CKTrait GroupTags => _data?.Tags ?? throw new InvalidOperationException( $"Object not initiliazed, please call {nameof( Initialize )}." );

            /// <summary>
            /// Gets the log time for the log.
            /// </summary>
            public DateTimeStamp LogTime => _data?.LogTime ?? throw new InvalidOperationException( $"Object not initiliazed, please call {nameof( Initialize )}." );

            /// <summary>
            /// Gets the log time of the group closing.
            /// It is <see cref="DateTimeStamp.MinValue"/> when the group is not closed yet.
            /// </summary>
            public DateTimeStamp CloseLogTime
            {
                get { return _closeLogTime; }
                internal set { _closeLogTime = value; }
            }

            /// <summary>
            /// Gets the <see cref="CKExceptionData"/> that captures exception information 
            /// if it exists. Returns null if no <see cref="P:Exception"/> exists.
            /// </summary>
            public CKExceptionData? ExceptionData
            {
                get
                {
                    if( _data == null ) throw new InvalidOperationException( $"Object not initiliazed, please call {nameof( Initialize )}." );
                    return _data.ExceptionData;
                }
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
            /// Gets the level associated to this group.
            /// The <see cref="LogLevel.IsFiltered"/> can be set here: use <see cref="MaskedGroupLevel"/> to get 
            /// the actual level from <see cref="LogLevel.Trace"/> to <see cref="LogLevel.Fatal"/>.
            /// </summary>
            public LogLevel GroupLevel => _data?.Level ?? throw new InvalidOperationException( $"Object not initiliazed, please call {nameof( Initialize )}." );

            /// <summary>
            /// Gets the actual level (from <see cref="LogLevel.Trace"/> to <see cref="LogLevel.Fatal"/>) associated to this group
            /// without <see cref="LogLevel.IsFiltered"/> bit.
            /// </summary>
            public LogLevel MaskedGroupLevel => _data?.MaskedLevel ?? throw new InvalidOperationException( $"Object not initiliazed, please call {nameof( Initialize )}." );

            /// <summary>
            /// Gets the text with which this group has been opened. Null if and only if the group is closed.
            /// </summary>
            public string GroupText
            {
                get
                {
                    if( _data == null ) throw new InvalidOperationException( $"Object not initiliazed, please call {nameof( Initialize )}." );
                    return _data.Text;
                }
            }

            /// <summary>
            /// Gets the associated <see cref="Exception"/> if it exists.
            /// </summary>
            public Exception? Exception
            {
                get
                {
                    if( _data == null ) throw new InvalidOperationException( $"Object not initiliazed, please call {nameof( Initialize )}." );
                    return _data.Exception;
                }
            }

            /// <summary>
            /// Gets the group data itself. Its properties are exposed
            /// on this <see cref="IActivityLogGroup"/> interface but this can be used
            /// to capture the Group information (the <see cref="Impl.IActivityMonitorImpl.InternalMonitor"/>
            /// uses this).
            /// </summary>
            public ActivityMonitorGroupData InnerData => _data ?? throw new InvalidOperationException( $"Object not initiliazed, please call {nameof( Initialize )}." );

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

            CKTrait? _savedMonitorTags;
            /// <summary>
            /// Gets or sets the <see cref="IActivityMonitor.AutoTags"/> that will be restored when group will be closed.
            /// Initialized with the current value of IActivityMonitor.Tags when the group has been opened.
            /// </summary>
            public CKTrait SavedMonitorTags
            {
                get => _savedMonitorTags ?? throw new InvalidOperationException( $"Object not initiliazed, please call {nameof( Initialize )}." );
                private set => _savedMonitorTags = value;
            }

            /// <summary>
            /// Gets whether the <see cref="GroupText"/> is actually the <see cref="Exception"/> message.
            /// </summary>
            public bool IsGroupTextTheExceptionMessage => _data?.IsTextTheExceptionMessage ?? throw new InvalidOperationException( $"Object not initiliazed, please call {nameof( Initialize )}." );

            /// <summary>
            /// Gets the file name of the source code that issued the log.
            /// </summary>
            public string FileName
            {
                get
                {
                    if( _data == null ) throw new InvalidOperationException( $"Object not initiliazed, please call {nameof( Initialize )}." );
                    return _data.FileName;
                }
            }

            /// <summary>
            /// Gets the line number of the <see cref="FileName"/> that issued the log.
            /// </summary>
            public int LineNumber => _data?.LineNumber ?? throw new InvalidOperationException( $"Object not initiliazed, please call {nameof( Initialize )}." );

            IDisposable IDisposableGroup.ConcludeWith( Func<string> getConclusionText )
            {
                if( _data == null ) throw new InvalidOperationException( $"Object not initiliazed, please call {nameof( Initialize )}." );
                if( !IsRejectedGroup ) _data.GetConclusionText = getConclusionText;
                return this;
            }

            /// <summary>
            /// Ensures that any groups opened after this one are closed before closing this one.
            /// </summary>
            void IDisposable.Dispose()
            {
                if( _data != null )
                {
                    Group? g = Monitor._current;
                    while( g != this )
                    {
                        // The current group cannot be null (or this object would have been already disposed). We bang!
                        ((IDisposable)g!).Dispose();
                        g = Monitor._current;
                    }
                    Monitor.CloseGroup( Monitor.NextLogTime(), null );
                }
            }

            internal void GroupClosing( ref List<ActivityLogGroupConclusion>? conclusions )
            {
                if( _data == null ) throw new InvalidOperationException( $"Object not initiliazed, please call {nameof( Initialize )}." );
                string? auto = _data.ConsumeConclusionText();
                if( auto != null )
                {
                    if( conclusions == null ) conclusions = new List<ActivityLogGroupConclusion>();
                    conclusions.Add( new ActivityLogGroupConclusion( Tags.GetTextConclusion, auto ) );
                }
            }

            internal void GroupClosed() => _data = null;
        }

        IActivityLogGroup? IActivityMonitorImpl.CurrentGroup => _current;

        /// <summary>
        /// Gets the currently opened group.
        /// Null when no group is currently opened.
        /// </summary>
        protected IActivityLogGroup? CurrentGroup => _current;

    }
}
