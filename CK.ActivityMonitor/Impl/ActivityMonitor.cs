using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using CK.Core.Impl;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Toolkit.Diagnostics;

namespace CK.Core
{
    /// <summary>
    /// Concrete implementation of <see cref="IActivityMonitor"/>.
    /// </summary>
    public partial class ActivityMonitor : IActivityMonitorImpl
    {
        /// <summary>
        /// Prefix used by <see cref="IActivityMonitor.SetTopic"/> is "Topic: ".
        /// </summary>
        static public readonly string SetTopicPrefix = "Topic: ";

        /// <summary>
        /// String to use to break the current <see cref="LogLevel"/> (as if a different <see cref="LogLevel"/> was used).
        /// </summary>
        static public readonly string ParkLevel = "PARK-LEVEL";


        /// <summary>
        /// Thread-safe context for tags used to categorize log entries (and group conclusions).
        /// All tags used in monitoring must be <see cref="Register"/>ed here.
        /// </summary>
        /// <remarks>
        /// Tags used for conclusions should start with "c:".
        /// </remarks>
        public static class Tags
        {
            /// <summary>
            /// The central, unique, context of all monitoring related tags used in the application domain.
            /// </summary>
            public static readonly CKTraitContext Context;

            /// <summary>
            /// Shortcut to <see cref="CKTraitContext.EmptyTrait">Context.EmptyTrait</see>.
            /// </summary>
            static public readonly CKTrait Empty;

            /// <summary>
            /// Creation of dependent activities are marked with "dep:CreateActivity".
            /// </summary>
            static public readonly CKTrait CreateDependentActivity;

            /// <summary>
            /// Start of dependent activities are marked with "dep:StartActivity".
            /// </summary>
            static public readonly CKTrait StartDependentActivity;

            /// <summary>
            /// Conclusions provided to IActivityMonitor.Close(string) are marked with "c:User".
            /// </summary>
            static public readonly CKTrait UserConclusion;

            /// <summary>
            /// Conclusions returned by the optional function when a group is opened (see <see cref="IActivityMonitor.UnfilteredOpenGroup"/>) are marked with "c:GetText".
            /// </summary>
            static public readonly CKTrait GetTextConclusion;

            /// <summary>
            /// Whenever <see cref="Topic"/> changed, a <see cref="LogLevel.Info"/> is emitted marked with "MonitorTopicChanged".
            /// </summary>
            static public readonly CKTrait MonitorTopicChanged;

            /// <summary>
            /// A "MonitorEnd" tag is emitted by <see cref="ActivityMonitorExtension.MonitorEnd"/>.
            /// This indicates the logical end of life of the monitor. It should not be used anymore (but technically can
            /// be used).
            /// </summary>
            static public readonly CKTrait MonitorEnd;

            /// <summary>
            /// A "m:Internal" tag is used while replaying <see cref="IActivityMonitorImpl.InternalMonitor"/>
            /// logs.
            /// </summary>
            static public readonly CKTrait InternalMonitor;

            /// <summary>
            /// A "StackTrace" tag activates stack trace tracking and dumping when a concurrent access is detected.
            /// logs.
            /// </summary>
            static public readonly CKTrait StackTrace;

            /// <summary>
            /// Simple shortcut to <see cref="CKTraitContext.FindOrCreate(string)"/>.
            /// </summary>
            /// <param name="tags">Atomic tag or multiple tags separated by pipes (|).</param>
            /// <returns>Registered tags.</returns>
            static public CKTrait Register( string tags ) => Context.FindOrCreate( tags );

            static Tags()
            {
                Context = CKTraitContext.Create( "ActivityMonitor" );
                Empty = Context.EmptyTrait;
                UserConclusion = Context.FindOrCreate( "c:User" );
                GetTextConclusion = Context.FindOrCreate( "c:GetText" );
                MonitorTopicChanged = Context.FindOrCreate( "MonitorTopicChanged" );
                CreateDependentActivity = Context.FindOrCreate( "dep:CreateActivity" );
                StartDependentActivity = Context.FindOrCreate( "dep:StartActivity" );
                MonitorEnd = Context.FindOrCreate( "MonitorEnd" );
                InternalMonitor = Context.FindOrCreate( "m:Internal" );
                StackTrace = Context.FindOrCreate( "StackTrace" );
            }
        }

        static LogFilter _defaultFilterLevel;

        /// <summary>
        /// Gets or sets the default filter that should be used when the <see cref="IActivityMonitor.ActualFilter"/> is <see cref="LogFilter.Undefined"/>.
        /// This configuration is per application domain (the backing field is static).
        /// It defaults to <see cref="LogFilter.Trace"/> and must be fully defined: <see cref="LogFilter.Line"/> nor <see cref="LogFilter.Group"/>
        /// can be <see cref="LogLevelFilter.None"/>.
        /// </summary>
        public static LogFilter DefaultFilter
        {
            get { return _defaultFilterLevel; }
            set
            {
                if( value.Line == LogLevelFilter.None || value.Group == LogLevelFilter.None ) ThrowHelper.ThrowArgumentException( nameof( DefaultFilter ), "Line nor Group can be LogLevelFilter.None." );
                _defaultFilterLevel = value;
            }
        }

        /// <summary>
        /// The automatic configuration actions.
        /// Registers actions via += (or <see cref="Delegate.Combine(Delegate,Delegate)"/> if you like pain), unregister with -= operator
        /// (or <see cref="Delegate.Remove"/>).
        /// Simply sets it to null to clear all currently registered actions (this, of course, only from tests and not in real code).
        /// </summary>
        static public Action<IActivityMonitor>? AutoConfiguration;

        /// <summary>
        /// The no-log text replaces any null or empty log text.
        /// </summary>
        static public readonly string NoLogText = "[no-log]";

        /// <summary>
        /// <see cref="IActivityMonitor.UniqueId"/> must be at least 4 characters long
        /// and not contain any <see cref="Char.IsWhiteSpace(char)"/>.
        /// </summary>
        public const int MinMonitorUniqueIdLength = 4;

        static ActivityMonitor()
        {
            AutoConfiguration = null;
            _defaultFilterLevel = LogFilter.Trace;
        }

        Group[] _groups;
        Group? _current;
        Group? _currentUnfiltered;
        readonly ActivityMonitorOutput _output;
        CKTrait _autoTags;
        string _topic;
        //
        volatile StackTrace? _currentStackTrace;
        int _enteredThreadId;
        int _signalFlag;
        LogFilter _actualFilter;
        LogFilter _configuredFilter;
        LogFilter _clientFilter;
        bool _trackStackTrace;

        /// <summary>
        /// Simple box around <see cref="DateTimeStamp"/> to be able to share it as needed.
        /// </summary>
        protected sealed class DateTimeStampProvider
        {
            /// <summary>
            /// Exposes the actual value.
            /// </summary>
            public DateTimeStamp Value = DateTimeStamp.MinValue;
        }

        readonly DateTimeStampProvider _lastLogTime;
        readonly string _uniqueId;
        InternalMonitor? _internalMonitor;

        static string CreateUniqueId() => Guid.NewGuid().ToString();

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitor"/> that applies all <see cref="AutoConfiguration"/>
        /// and has an empty <see cref="Topic"/> initially set.
        /// </summary>
        public ActivityMonitor()
            : this( new DateTimeStampProvider(), CreateUniqueId(), Tags.Empty, true )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitor"/> that applies all <see cref="AutoConfiguration"/> and has an initial <see cref="Topic"/> set.
        /// </summary>
        /// <param name="topic">Initial topic (can be null).</param>
        public ActivityMonitor( string topic )
            : this( new DateTimeStampProvider(), CreateUniqueId(), Tags.Empty, true )
        {
            if( topic != null ) SetTopic( topic );
        }

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitor"/> that optionally applies <see cref="AutoConfiguration"/> and with an initial topic.
        /// </summary>
        /// <param name="applyAutoConfigurations">Whether <see cref="AutoConfiguration"/> should be applied.</param>
        /// <param name="topic">Optional initial topic (can be null).</param>
        public ActivityMonitor( bool applyAutoConfigurations, string? topic = null )
            : this( new DateTimeStampProvider(), CreateUniqueId(), Tags.Empty, applyAutoConfigurations )
        {
            if( topic != null ) SetTopic( topic );
        }

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitor"/> bound to a specific <see cref="DateTimeStampProvider"/>,
        /// a unique identifier, initial <see cref="AutoTags"/> and that optionally applies <see cref="AutoConfiguration"/>.
        /// </summary>
        /// <param name="stampProvider">The stamp provider to use.</param>
        /// <param name="uniqueId">This monitor unique identifier.</param>
        /// <param name="tags">Initial tags.</param>
        /// <param name="applyAutoConfigurations">Whether <see cref="AutoConfiguration"/> should be applied.</param>
        protected ActivityMonitor( DateTimeStampProvider stampProvider,
                                   string uniqueId,
                                   CKTrait tags,
                                   bool applyAutoConfigurations )
        {
            if( uniqueId == null
                || uniqueId.Length < MinMonitorUniqueIdLength
                || uniqueId.Any( c => Char.IsWhiteSpace( c ) ) )
            {
                ThrowHelper.ThrowArgumentException( nameof( uniqueId ), $"Monitor UniqueId must be at least {MinMonitorUniqueIdLength} long and not contain any whitespace." );
            }
            _uniqueId = uniqueId;
            _lastLogTime = new DateTimeStampProvider();
            _groups = new Group[8];
            for( int i = 0; i < _groups.Length; ++i ) _groups[i] = new Group( this, i );
            _autoTags = tags ?? Tags.Empty;
            _trackStackTrace = _autoTags.AtomicTraits.Contains( Tags.StackTrace );
            _topic = String.Empty;
            _output = new ActivityMonitorOutput( this );
            if( applyAutoConfigurations )
            {
                AutoConfiguration?.Invoke( this );
            }
        }

        /// <summary>
        /// Gets the unique identifier for this monitor.
        /// </summary>
        public string UniqueId => _uniqueId;

        /// <summary>
        /// Gets the <see cref="IActivityMonitorOutput"/> for this monitor.
        /// </summary>
        public IActivityMonitorOutput Output => _output;

        /// <summary>
        /// Gets the last <see cref="DateTimeStamp"/> for this monitor.
        /// </summary>
        public DateTimeStamp LastLogTime => _lastLogTime.Value;

        /// <summary>
        /// Gets the current topic for this monitor. This can be any non null string (null topic is mapped to the empty string) that describes
        /// the current activity. It must be set with <see cref="SetTopic"/> and unlike <see cref="MinimalFilter"/> and <see cref="AutoTags"/>, 
        /// the topic is not reseted when groups are closed.
        /// </summary>
        public string Topic => _topic;

        /// <summary>
        /// Sets the current topic for this monitor. This can be any non null string (null topic is mapped to the empty string) that describes
        /// the current activity.
        /// </summary>
        public void SetTopic( string newTopic, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0 )
        {
            if( newTopic == null ) newTopic = String.Empty;
            if( _topic != newTopic )
            {
                ReentrantAndConcurrentCheck();
                try
                {
                    DoSetTopic( newTopic, fileName, lineNumber );
                }
                finally
                {
                    ReentrantAndConcurrentRelease();
                }
            }
        }

        void DoSetTopic( string newTopic, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0 )
        {
            Debug.Assert( _enteredThreadId == Environment.CurrentManagedThreadId );
            Debug.Assert( newTopic != null && _topic != newTopic );
            _topic = newTopic!;
            _output.BridgeTarget.TargetTopicChanged( newTopic!, fileName, lineNumber );
            MonoParameterSafeCall( ( client, topic ) => client.OnTopicChanged( topic!, fileName, lineNumber ), newTopic );
            if( Interlocked.Exchange( ref _signalFlag, 0 ) == 1 ) DoResyncActualFilter();
            SendTopicLogLine( fileName, lineNumber );
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        void SendTopicLogLine( [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0 )
        {
            var d = new ActivityMonitorLogData( LogLevel.Info,
                                                _autoTags | Tags.MonitorTopicChanged,
                                                SetTopicPrefix + _topic,
                                                null,
                                                fileName,
                                                lineNumber );
            DoUnfilteredLog( ref d );
        }

        /// <summary>
        /// Gets or sets the tags of this monitor: any subsequent logs will be tagged by these tags.
        /// The <see cref="CKTrait"/> must be registered in <see cref="ActivityMonitor.Tags"/>.
        /// Modifications to this property are scoped to the current Group since when a Group is closed, this
        /// property (like <see cref="MinimalFilter"/>) is automatically restored to its original value (captured when the Group was opened).
        /// </summary>
        [AllowNull]
        public CKTrait AutoTags
        {
            get { return _autoTags; }
            set
            {
                if( value == null ) value = Tags.Empty;
                else if( value.Context != Tags.Context ) throw new ArgumentException( ActivityMonitorResources.ActivityMonitorTagMustBeRegistered, nameof( value ) );
                if( _autoTags != value )
                {
                    ReentrantAndConcurrentCheck();
                    try
                    {
                        DoSetAutoTags( value );
                    }
                    finally
                    {
                        ReentrantAndConcurrentRelease();
                    }
                }
            }
        }

        void DoSetAutoTags( CKTrait newTags )
        {
            Debug.Assert( _enteredThreadId == Environment.CurrentManagedThreadId );
            Debug.Assert( newTags != null && _autoTags != newTags && newTags.Context == Tags.Context );
            _autoTags = newTags!;
            _trackStackTrace = _autoTags.AtomicTraits.Contains( Tags.StackTrace );
            _output.BridgeTarget.TargetAutoTagsChanged( newTags! );
            MonoParameterSafeCall( ( client, tags ) => client.OnAutoTagsChanged( tags! ), newTags );
        }

        /// <summary>
        /// Called by IActivityMonitorBoundClient clients to initialize Topic and AutoTag from 
        /// inside their SetMonitor or any other methods provided that a reentrant and concurrent lock 
        /// has been obtained (otherwise an InvalidOperationException is thrown).
        /// </summary>
        void IActivityMonitorImpl.InitializeTopicAndAutoTags( string newTopic, CKTrait newTags, string? fileName, int lineNumber )
        {
            RentrantOnlyCheck();
            if( newTopic != null && _topic != newTopic ) DoSetTopic( newTopic, fileName, lineNumber );
            if( newTags != null && _autoTags != newTags ) DoSetAutoTags( newTags );
        }

        /// <summary>
        /// Gets or sets a filter based for the log level.
        /// Modifications to this property are scoped to the current Group since when a Group is closed, this
        /// property (like <see cref="AutoTags"/>) is automatically restored to its original value (captured when the Group was opened).
        /// </summary>
        public LogFilter MinimalFilter
        {
            get { return _configuredFilter; }
            set
            {
                if( _configuredFilter != value )
                {
                    ReentrantAndConcurrentCheck();
                    try
                    {
                        DoSetConfiguredFilter( value );
                    }
                    finally
                    {
                        ReentrantAndConcurrentRelease();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the actual filter level for logs: this combines the configured <see cref="MinimalFilter"/> and the minimal requirements
        /// of any <see cref="IActivityMonitorBoundClient"/> that specifies such a minimal filter level.
        /// </summary>
        /// <remarks>
        /// This does NOT take into account the static (application-domain) <see cref="ActivityMonitor.DefaultFilter"/>.
        /// This global default must be used if this ActualFilter is <see cref="LogFilter.Undefined"/> for <see cref="LogFilter.Line"/> or <see cref="LogFilter.Group"/>: 
        /// the <see cref="ActivityMonitorExtension.ShouldLogLine">ShouldLog</see> extension method takes it into account.
        /// </remarks>
        public LogFilter ActualFilter
        {
            get
            {
                if( Interlocked.Exchange( ref _signalFlag, 0 ) == 1 ) ResyncActualFilter();
                return _actualFilter;
            }
        }

        void ResyncActualFilter()
        {
            ReentrantAndConcurrentCheck();
            try
            {
                DoResyncActualFilter();
            }
            finally
            {
                ReentrantAndConcurrentRelease();
            }
        }

        void DoResyncActualFilter()
        {
            do
            {
                _clientFilter = HandleBoundClientsSignal();
            }
            while( Interlocked.Exchange( ref _signalFlag, 0 ) == 1 );
            UpdateActualFilter();
        }

        internal void DoSetConfiguredFilter( LogFilter value )
        {
            Debug.Assert( _enteredThreadId == Environment.CurrentManagedThreadId );
            Debug.Assert( _configuredFilter != value );
            _configuredFilter = value;
            UpdateActualFilter();
        }

        void UpdateActualFilter()
        {
            Debug.Assert( _enteredThreadId == Environment.CurrentManagedThreadId );
            LogFilter newLevel = _configuredFilter.Combine( _clientFilter );
            if( newLevel != _actualFilter )
            {
                _actualFilter = newLevel;
                _output.BridgeTarget.TargetActualFilterChanged();
            }
        }

        LogFilter HandleBoundClientsSignal()
        {
            Debug.Assert( _enteredThreadId == Environment.CurrentManagedThreadId );

            LogFilter minimal = LogFilter.Undefined;
            List<IActivityMonitorClient>? buggyClients = null;
            foreach( var l in _output.Clients )
            {
                if( l is IActivityMonitorBoundClient bound )
                {
                    try
                    {
                        if( bound.IsDead )
                        {
                            if( buggyClients == null ) buggyClients = new List<IActivityMonitorClient>();
                            buggyClients.Add( l );
                        }
                        else
                        {
                            minimal = minimal.Combine( bound.MinimalFilter );
                        }
                    }
                    catch( Exception exCall )
                    {
                        if( !InternalLogUnhandledClientError( exCall, l, ref buggyClients ) ) throw;
                    }
                }
            }
            if( buggyClients != null )
            {
                HandleBuggyClients( buggyClients );
            }
            return minimal;
        }

        void IActivityMonitorImpl.SignalChange()
        {
            _signalFlag = 1;
            Interlocked.MemoryBarrier();
            // By signaling here the change to the bridge, we handle the case where the current
            // active thread works on a bridged monitor: the bridged monitor's _actualFilterIsDirty
            // is set to true and any interaction with its ActualFilter will trigger a resynchronization
            // of this _actualFilter.
            _output.BridgeTarget.TargetActualFilterChanged();
        }

        void IActivityMonitorImpl.OnClientMinimalFilterChanged( LogFilter oldLevel, LogFilter newLevel )
        {
            // Silently ignores stupid calls.
            if( oldLevel == newLevel ) return;
            bool isNotReentrant = ConcurrentOnlyCheck();
            try
            {
                bool dirty = Interlocked.Exchange( ref _signalFlag, 0 ) == 1;
                do
                {
                    // Optimization for some cases: if we can be sure that the oldLevel has no impact on the current 
                    // client filter, we can conclude without processing all the minimal filters.
                    if( !dirty
                        && ((oldLevel.Line == LogLevelFilter.None || oldLevel.Line > _clientFilter.Line)
                        && (oldLevel.Group == LogLevelFilter.None || oldLevel.Group > _clientFilter.Group)) )
                    {
                        // This Client had no impact on the current final client filter (and no signal has been received): 
                        // if its new level has no impact on the current client filter, there is nothing to do.
                        var f = _clientFilter.Combine( newLevel );
                        if( f == _clientFilter ) return;
                        _clientFilter = f;
                    }
                    else
                    {
                        // Whatever the new level is we have to update our client final filter.
                        // We handle it as if SignalChange has been called by one of our bound
                        // clients.
                        _clientFilter = HandleBoundClientsSignal();
                    }
                }
                while( (dirty = Interlocked.Exchange( ref _signalFlag, 0 ) == 1) );
                UpdateActualFilter();
            }
            finally
            {
                if( isNotReentrant ) ReentrantAndConcurrentRelease();
            }
        }

        /// <summary>
        /// Logs a text regardless of <see cref="MinimalFilter"/> level (except for <see cref="LogLevelFilter.Off"/>). 
        /// Each call to log is considered as a unit of text: depending on the rendering engine, a line or a 
        /// paragraph separator (or any appropriate separator) should be appended between each text if 
        /// the level is the same as the previous one.
        /// See remarks.
        /// </summary>
        /// <param name="data">
        /// Data that describes the log. When null or when <see cref="ActivityMonitorLogData.MaskedLevel"/> 
        /// is <see cref="LogLevel.None"/>, nothing happens (whereas for group, a rejected group is recorded and returned).
        /// </param>
        /// <remarks>
        /// A null or empty <see cref="ActivityMonitorLogData.Text"/> is logged as <see cref="ActivityMonitor.NoLogText"/>.
        /// If needed, the special text <see cref="ActivityMonitor.ParkLevel"/> ("PARK-LEVEL") breaks the current <see cref="LogLevel"/>
        /// and resets it: the next log, even with the same LogLevel, will be treated as if a different LogLevel is used.
        /// </remarks>
        public void UnfilteredLog( ref ActivityMonitorLogData data )
        {
            if( data.MaskedLevel == LogLevel.None ) return;
            ReentrantAndConcurrentCheck();
            try
            {
                DoUnfilteredLog( ref data );
            }
            finally
            {
                ReentrantAndConcurrentRelease();
            }
        }

        void DoUnfilteredLog( ref ActivityMonitorLogData data )
        {
            Debug.Assert( _enteredThreadId == Environment.CurrentManagedThreadId );
            Debug.Assert( data.Level != LogLevel.None );
            Debug.Assert( !String.IsNullOrEmpty( data.Text ) );

            // We consider that as long has the log IsFiltered, the decision has already
            // being taken and UnfilteredLog must do its job: handling the dispatch of the log.
            // But for logs that do not claim to have been filtered, we ensure here that the ultimate
            // level Off is not the current one.
            if( !data.IsFilteredLog )
            {
                if( Interlocked.Exchange( ref _signalFlag, 0 ) == 1 ) DoResyncActualFilter();
                if( _actualFilter.Line == LogLevelFilter.Off
                    || (_actualFilter.Line == LogLevelFilter.None && DefaultFilter.Line == LogLevelFilter.Off) )
                {
                    return;
                }
            }

            if( !data.LogTime.IsKnown )
            {
                _lastLogTime.Value = data.SetLogTime( new DateTimeStamp( _lastLogTime.Value, DateTime.UtcNow ) );
            }

            List<IActivityMonitorClient>? buggyClients = null;
            foreach( var l in _output.Clients )
            {
                try
                {
                    l.OnUnfilteredLog( ref data );
                }
                catch( Exception exCall )
                {
                    if( !InternalLogUnhandledClientError( exCall, l, ref buggyClients ) ) throw;
                }
            }
            if( buggyClients != null )
            {
                HandleBuggyClients( buggyClients );
            }
        }

        /// <summary>
        /// Opens a group regardless of <see cref="ActualFilter"/> level (except for <see cref="LogLevelFilter.Off"/>). 
        /// The group is open even if <paramref name="data"/> is null or its <see cref="ActivityMonitorLogData.MaskedLevel"/>
        /// is <see cref="LogLevel.None"/>: either <see cref="CloseGroup"/> must be called and/or the returned object must
        /// be disposed (both can be called on the same group: when the group is closed with CloseGroup, the dispose action is
        /// ignored).
        /// </summary>
        /// <param name="data">
        /// Data that describes the log. When <see cref="ActivityMonitorLogData.MaskedLevel"/> 
        /// is <see cref="LogLevel.None"/> a <see cref="IActivityLogGroup.IsRejectedGroup"/> is recorded and returned and must be closed.
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
        /// Note that this automatic configuration restoration works even if the group has been filtered and rejected.
        /// </para>
        /// </remarks>
        public virtual IDisposableGroup UnfilteredOpenGroup( ref ActivityMonitorLogData data )
        {
            ReentrantAndConcurrentCheck();
            try
            {
                return DoOpenGroup( ref data );
            }
            finally
            {
                ReentrantAndConcurrentRelease();
            }
        }

        IDisposableGroup DoOpenGroup( ref ActivityMonitorLogData data )
        {
            Debug.Assert( _enteredThreadId == Environment.CurrentManagedThreadId );

            int idxNext = _current != null ? _current.Index + 1 : 0;
            if( idxNext == _groups.Length )
            {
                Array.Resize( ref _groups, _groups.Length * 2 );
                for( int i = idxNext; i < _groups.Length; ++i ) _groups[i] = new Group( this, i );
            }
            _current = _groups[idxNext];
            if( data.MaskedLevel == LogLevel.None )
            {
                _current.InitializeRejectedGroup();
                return _current;
            }
            // We consider that as long has the log IsFiltered, the decision has already
            // being taken and UnfilteredLog must do its job: handling the dispatch of the log.
            // But for logs that do not claim to have been filtered, we ensure here that the ultimate
            // level Off is not the current one.
            if( !data.IsFilteredLog )
            {
                if( Interlocked.Exchange( ref _signalFlag, 0 ) == 1 ) DoResyncActualFilter();
                if( _actualFilter.Group == LogLevelFilter.Off
                    || (_actualFilter.Group == LogLevelFilter.None && DefaultFilter.Group == LogLevelFilter.Off) )
                {
                    _current.InitializeRejectedGroup();
                    return _current;
                }
            }
            if( !data.LogTime.IsKnown )
            {
                _lastLogTime.Value = data.SetLogTime( new DateTimeStamp( _lastLogTime.Value, DateTime.UtcNow ) );
            }

            _current.Initialize( ref data );
            _currentUnfiltered = _current;
            MonoParameterSafeCall( ( client, group ) => client.OnOpenGroup( group ), _current );
            return _current;
        }

        /// <inheritdoc />
        public virtual bool CloseGroup( object? userConclusion = null )
        {
            bool isNoReentrant = ConcurrentOnlyCheck();
            try
            {
                return DoCloseGroup( userConclusion );
            }
            finally
            {
                if( isNoReentrant ) ReentrantAndConcurrentRelease();
            }
        }

        bool DoCloseGroup( object? userConclusion, DateTimeStamp logTime = default )
        {
            Debug.Assert( _enteredThreadId == Environment.CurrentManagedThreadId );
            Group? g = _current;
            if( g == null ) return false;
            // Handles the rejected case first (easiest).
            if( g.IsRejectedGroup )
            {
                if( g.SavedMonitorFilter != _configuredFilter ) DoSetConfiguredFilter( g.SavedMonitorFilter );
                _autoTags = g.SavedMonitorTags;
                _trackStackTrace = g.SavedTrackStackTrace;
                _current = g.Index > 0 ? _groups[g.Index - 1] : null;
            }
            else
            {
                #region Closing the group

                if( logTime.IsKnown )
                {
                    g.CloseLogTime = logTime;
                }
                else
                {
                    g.CloseLogTime = _lastLogTime.Value = new DateTimeStamp( _lastLogTime.Value, DateTime.UtcNow );
                }
                List<ActivityLogGroupConclusion>? conclusions = userConclusion as List<ActivityLogGroupConclusion>;
                if( conclusions == null && userConclusion != null )
                {
                    conclusions = new List<ActivityLogGroupConclusion>();
                    if( userConclusion is string s )
                    {
                        conclusions.Add( new ActivityLogGroupConclusion( Tags.UserConclusion, s ) );
                    }
                    else
                    {
                        if( userConclusion is ActivityLogGroupConclusion c )
                        {
                            conclusions.Add( c );
                        }
                        else
                        {
                            if( userConclusion is IEnumerable<ActivityLogGroupConclusion> multi )
                            {
                                conclusions.AddRange( multi );
                            }
                            else
                            {
                                conclusions.Add( new ActivityLogGroupConclusion( Tags.UserConclusion, userConclusion.ToString() ?? String.Empty ) );
                            }
                        }
                    }
                }
                g.GroupClosing( ref conclusions );

                List<IActivityMonitorClient>? buggyClients = null;
                foreach( var l in _output.Clients )
                {
                    try
                    {
                        l.OnGroupClosing( g, ref conclusions );
                    }
                    catch( Exception exCall )
                    {
                        if( !InternalLogUnhandledClientError( exCall, l, ref buggyClients ) ) throw;
                    }
                }
                if( buggyClients != null )
                {
                    HandleBuggyClients( buggyClients );
                    buggyClients = null;
                }
                if( g.SavedMonitorFilter != _configuredFilter ) DoSetConfiguredFilter( g.SavedMonitorFilter );
                _autoTags = g.SavedMonitorTags;
                _trackStackTrace = g.SavedTrackStackTrace;
                _current = g.Index > 0 ? _groups[g.Index - 1] : null;
                _currentUnfiltered = (Group?)g.Parent;

                var sentConclusions = conclusions != null ? conclusions : (IReadOnlyList<ActivityLogGroupConclusion>)Array.Empty<ActivityLogGroupConclusion>();
                foreach( var l in _output.Clients )
                {
                    try
                    {
                        l.OnGroupClosed( g, sentConclusions );
                    }
                    catch( Exception exCall )
                    {
                        if( !InternalLogUnhandledClientError( exCall, l, ref buggyClients ) ) throw;
                    }
                }
                if( buggyClients != null )
                {
                    HandleBuggyClients( buggyClients );
                }
                #endregion
            }
            g.GroupClosed();
            return true;
        }

        /// <summary>
        /// Generalizes calls to IActivityMonitorClient methods that have only one parameter.
        /// </summary>
        void MonoParameterSafeCall<T>( Action<IActivityMonitorClient, T> call, T arg )
        {
            Debug.Assert( _enteredThreadId == Environment.CurrentManagedThreadId );
            List<IActivityMonitorClient>? buggyClients = null;
            foreach( var l in _output.Clients )
            {
                try
                {
                    call( l, arg );
                }
                catch( Exception exCall )
                {
                    if( !InternalLogUnhandledClientError( exCall, l, ref buggyClients ) ) throw;
                }
            }
            if( buggyClients != null )
            {
                HandleBuggyClients( buggyClients );
            }
        }


        class RAndCChecker : IDisposable
        {
            readonly ActivityMonitor _m;

            public RAndCChecker( ActivityMonitor m )
            {
                _m = m;
                _m.ReentrantAndConcurrentCheck();
            }

            public void Dispose()
            {
                _m.ReentrantAndConcurrentRelease();
            }
        }

        IDisposable IActivityMonitorImpl.ReentrancyAndConcurrencyLock()
        {
            return new RAndCChecker( this );
        }

        /// <summary>
        /// Gets a disposable object that checks for reentrant and concurrent calls.
        /// </summary>
        /// <returns>A disposable object (that must be disposed).</returns>
        protected IDisposable ReentrancyAndConcurrencyLock()
        {
            return new RAndCChecker( this );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RentrantOnlyCheck()
        {
            if( _enteredThreadId != Environment.CurrentManagedThreadId ) ThrowHelper.ThrowInvalidOperationException( ActivityMonitorResources.ActivityMonitorReentrancyCallOnly );
        }

        void ReentrantAndConcurrentCheck()
        {
            int currentThreadId = Environment.CurrentManagedThreadId;
            int alreadyEnteredId;
            if( (alreadyEnteredId = Interlocked.CompareExchange( ref _enteredThreadId, currentThreadId, 0 )) != 0 )
            {
                if( alreadyEnteredId == currentThreadId )
                {
                    throw new CKException( ActivityMonitorResources.ActivityMonitorReentrancyError, _uniqueId );
                }
                else
                {
                    ThrowConcurrentThreadAccessException( ActivityMonitorResources.ActivityMonitorConcurrentThreadAccess, alreadyEnteredId, currentThreadId );
                }
            }
            if( _trackStackTrace )
            {
                _currentStackTrace = new StackTrace( 3, true );
            }
        }

        /// <summary>
        /// Checks only for concurrency issues. 
        /// False if a call already exists (reentrant call): when true is returned, ReentrantAndConcurrentRelease must be called
        /// to cleanup the concurrency detection internal state.
        /// </summary>
        /// <returns>False for a reentrant call, true otherwise.</returns>
        bool ConcurrentOnlyCheck()
        {
            int currentThreadId = Environment.CurrentManagedThreadId;
            int alreadyEnteredId;
            if( (alreadyEnteredId = Interlocked.CompareExchange( ref _enteredThreadId, currentThreadId, 0 )) != 0 )
            {
                if( alreadyEnteredId == currentThreadId )
                {
                    return false;
                }
                else
                {
                    ThrowConcurrentThreadAccessException( ActivityMonitorResources.ActivityMonitorConcurrentThreadAccess, alreadyEnteredId, currentThreadId );
                }
            }
            if( _trackStackTrace )
            {
                _currentStackTrace = new StackTrace( 3, true );
            }
            return true;
        }

        void ThrowConcurrentThreadAccessException( string messageFormat, int alreadyEnteredId, int currentThreadId )
        {
            StackTrace? t = _currentStackTrace;
            while( _trackStackTrace && t == null )
            {
                Thread.Yield();
                t = _currentStackTrace;
            }
            var msg = String.Format( messageFormat, _uniqueId, currentThreadId, alreadyEnteredId );
            if( t != null ) msg = AddCurrentStackTrace( msg, t );
            throw new CKException( msg );
        }

        void ReentrantAndConcurrentRelease()
        {
            if( _internalMonitor != null && _internalMonitor.Recorder.History.Count > 0 )
            {
                DoReplayInternalLogs();
            }
            _currentStackTrace = null;
#if DEBUG
            int currentThreadId = Environment.CurrentManagedThreadId;
            int alreadyEnteredId = Interlocked.CompareExchange( ref _enteredThreadId, 0, currentThreadId );
            Debug.Assert( alreadyEnteredId == currentThreadId, $"Internal error on Monitor '{_uniqueId}': Error during release reentrancy operation. Current Thread n°{alreadyEnteredId} is trying to exit it but Thread {currentThreadId} entered it." );
#else
            Interlocked.Exchange( ref _enteredThreadId, 0 );
#endif
        }

        static string AddCurrentStackTrace( string msg, StackTrace trace )
        {
            bool inLogs = false;
            StringBuilder b = new StringBuilder( msg );
            StackFrame[] frames = trace.GetFrames();
            for( int i = 0; i < frames.Length; ++i )
            {
                StackFrame f = frames[i];
                MethodBase m = f.GetMethod()!;
                Type t = m.DeclaringType!;
                if( inLogs || t.Assembly != typeof( ActivityMonitor ).Assembly )
                {
                    if( !inLogs ) b.AppendLine().Append( "-- Other Monitor's StackTrace (" ).Append( trace.FrameCount - i ).Append( " frames):" ).AppendLine();
                    inLogs = true;
                    b.Append( t.FullName ).Append( '.' ).Append( m.Name )
                     .Append( " at " )
                     .Append( f.GetFileName() ).Append( " (" ).Append( f.GetFileLineNumber() ).Append( ',' ).Append( f.GetFileColumnNumber() ).Append( ')' )
                     .AppendLine();
                }
            }
            return b.ToString();
        }

        [DoesNotReturn]
        internal static void ThrowOnGroupOrDataNotInitialized()
        {
            throw new InvalidOperationException( $"Group or Data not initialized, please call Initialize." );
        }

    }
}
