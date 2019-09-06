using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using CK.Core.Impl;
using System.Linq;

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
            public static readonly CKTagContext Context;

            /// <summary>
            /// Shortcut to <see cref="CKTagContext.EmptyTag">Context.EmptyTag</see>.
            /// </summary>
            static public readonly CKTag Empty;

            /// <summary>
            /// Creation of dependent activities are marked with "dep:CreateActivity".
            /// </summary>
            static public readonly CKTag CreateDependentActivity;

            /// <summary>
            /// Start of dependent activities are marked with "dep:StartActivity".
            /// </summary>
            static public readonly CKTag StartDependentActivity;

            /// <summary>
            /// Conclusions provided to IActivityMonitor.Close(string) are marked with "c:User".
            /// </summary>
            static public readonly CKTag UserConclusion;

            /// <summary>
            /// Conclusions returned by the optional function when a group is opened (see <see cref="IActivityMonitor.UnfilteredOpenGroup"/>) are marked with "c:GetText".
            /// </summary>
            static public readonly CKTag GetTextConclusion;

            /// <summary>
            /// Whenever <see cref="Topic"/> changed, a <see cref="LogLevel.Info"/> is emitted marked with "MonitorTopicChanged".
            /// </summary>
            static public readonly CKTag MonitorTopicChanged;

            /// <summary>
            /// A "MonitorEnd" tag is emitted by <see cref="ActivityMonitorExtension.MonitorEnd"/>.
            /// This indicates the logical end of life of the monitor. It should not be used anymore (but technically can
            /// be used).
            /// </summary>
            static public readonly CKTag MonitorEnd;

            /// <summary>
            /// A "m:Internal" tag is used while replaying <see cref="IActivityMonitorImpl.InternalMonitor"/>
            /// logs.
            /// </summary>
            static public readonly CKTag InternalMonitor;

            /// <summary>
            /// Simple shortcut to <see cref="CKTagContext.FindOrCreate(string)"/>.
            /// </summary>
            /// <param name="tags">Atomic tag or multiple tags separated by pipes (|).</param>
            /// <returns>Registered tags.</returns>
            static public CKTag Register( string tags )
            {
                return Context.FindOrCreate( tags );
            }

            static Tags()
            {
                Context = new CKTagContext( "ActivityMonitor" );
                Empty = Context.EmptyTag;
                UserConclusion = Context.FindOrCreate( "c:User" );
                GetTextConclusion = Context.FindOrCreate( "c:GetText" );
                MonitorTopicChanged = Context.FindOrCreate( "MonitorTopicChanged" );
                CreateDependentActivity = Context.FindOrCreate( "dep:CreateActivity" );
                StartDependentActivity = Context.FindOrCreate( "dep:StartActivity" );
                MonitorEnd = Context.FindOrCreate("MonitorEnd");
                InternalMonitor = Context.FindOrCreate( "m:Internal" );
            }
        }

        /// <summary>
        /// The monitoring error collector. 
        /// Any error that occurs while dispatching logs to <see cref="IActivityMonitorClient"/>
        /// are collected and the culprit is removed from <see cref="Output"/>.
        /// See <see cref="T:CriticalErrorCollector"/>.
        /// </summary>
        public static readonly CriticalErrorCollector CriticalErrorCollector;

        static LogFilter _defaultFilterLevel;
        
        /// <summary>
        /// Internal event used by ActivityMonitorBridgeTarget that have at least one ActivityMonitorBridge in another application domain.
        /// </summary>
        static internal event EventHandler DefaultFilterLevelChanged;
        static object _lockDefaultFilterLevel;

        /// <summary>
        /// Gets or sets the default filter that should be used when the <see cref="IActivityMonitor.ActualFilter"/> is <see cref="LogFilter.Undefined"/>.
        /// This configuration is per application domain (the backing field is static).
        /// It defaults to <see cref="LogFilter.Trace"/>.
        /// </summary>
        public static LogFilter DefaultFilter
        {
            get { return _defaultFilterLevel; }
            set 
            {
                lock( _lockDefaultFilterLevel )
                {
                    if( _defaultFilterLevel != value )
                    {
                        _defaultFilterLevel = value;
                        var h = DefaultFilterLevelChanged;
                        if( h != null )
                        {
                            try
                            {
                                h( null, EventArgs.Empty );
                            }
                            catch( Exception ex )
                            {
                                CriticalErrorCollector.Add( ex, "DefaultFilter changed." );
                            }
                        }
                    }
                }
            } 
        }

        /// <summary>
        /// The automatic configuration actions.
        /// Registers actions via += (or <see cref="Delegate.Combine(Delegate,Delegate)"/> if you like pain), unregister with -= operator (or <see cref="Delegate.Remove"/>).
        /// Simply sets it to null to clear all currently registered actions (this, of course, only from tests and not in real code).
        /// </summary>
        static public Action<IActivityMonitor> AutoConfiguration;

        /// <summary>
        /// The no-log text replaces any null or empty log text.
        /// </summary>
        static public readonly string NoLogText = "[no-log]";

        static ActivityMonitor()
        {
            CriticalErrorCollector = new CriticalErrorCollector();
            AutoConfiguration = null;
            _defaultFilterLevel = LogFilter.Trace;
            _lockDefaultFilterLevel = new object();
        }

        LogFilter _actualFilter;
        LogFilter _configuredFilter;
        LogFilter _clientFilter;
        int _signalFlag;
        Group[] _groups;
        Group _current;
        Group _currentUnfiltered;
        readonly ActivityMonitorOutput _output;
        CKTag _currentTag;
        int _enteredThreadId;
        string _topic;

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
        DateTimeStampProvider _lastLogTime;
        readonly Guid _uniqueId;
        InternalMonitor _internalMonitor;

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitor"/> that applies all <see cref="AutoConfiguration"/>
        /// and has an empty <see cref="Topic"/> initially set.
        /// </summary>
        public ActivityMonitor()
            : this( new DateTimeStampProvider(), Guid.NewGuid(), Tags.Empty, true )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitor"/> that applies all <see cref="AutoConfiguration"/> and has an initial <see cref="Topic"/> set.
        /// </summary>
        /// <param name="topic">Initial topic (can be null).</param>
        public ActivityMonitor( string topic )
            : this( new DateTimeStampProvider(), Guid.NewGuid(), Tags.Empty, true )
        {
            if( topic != null ) SetTopic( topic );
        }

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitor"/> that optionally applies <see cref="AutoConfiguration"/> and with an initial topic.
        /// </summary>
        /// <param name="applyAutoConfigurations">Whether <see cref="AutoConfiguration"/> should be applied.</param>
        /// <param name="topic">Optional initial topic (can be null).</param>
        public ActivityMonitor( bool applyAutoConfigurations, string topic = null )
            : this( new DateTimeStampProvider(), Guid.NewGuid(), Tags.Empty, applyAutoConfigurations )
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
        protected ActivityMonitor(
            DateTimeStampProvider stampProvider,
            Guid uniqueId,
            CKTag tags,
            bool applyAutoConfigurations )
        {
            _uniqueId = uniqueId;
            _lastLogTime = new DateTimeStampProvider();
            _groups = new Group[8];
            for( int i = 0; i < _groups.Length; ++i ) _groups[i] = new Group( this, i );
            _currentTag = tags ?? Tags.Empty;
            _topic = String.Empty;
            _output = new ActivityMonitorOutput( this );
            if( applyAutoConfigurations )
            {
                AutoConfiguration?.Invoke( this );
            }
        }

        Guid IUniqueId.UniqueId => _uniqueId; 

        /// <summary>
        /// Gets the unique identifier for this monitor.
        /// </summary>
        protected Guid UniqueId => _uniqueId; 

        /// <summary>
        /// Gets the <see cref="IActivityMonitorOutput"/> for this monitor.
        /// </summary>
        public IActivityMonitorOutput Output => _output; 

        /// <summary>
        /// Gets the last <see cref="DateTimeStamp"/> for this monitor.
        /// </summary>
        public DateTimeStamp LastLogTime  => _lastLogTime.Value; 
        
        /// <summary>
        /// Gets the current topic for this monitor. This can be any non null string (null topic is mapped to the empty string) that describes
        /// the current activity. It must be set with <see cref="SetTopic"/> and unlike <see cref="MinimalFilter"/> and <see cref="AutoTags"/>, 
        /// the topic is not reseted when groups are closed.
        /// </summary>
        public string Topic  => _topic; 

        /// <summary>
        /// Sets the current topic for this monitor. This can be any non null string (null topic is mapped to the empty string) that describes
        /// the current activity.
        /// </summary>
        public void SetTopic( string newTopic, [CallerFilePath]string fileName = null, [CallerLineNumber]int lineNumber = 0 )
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

        void DoSetTopic( string newTopic, [CallerFilePath]string fileName = null, [CallerLineNumber]int lineNumber = 0 )
        {
            Debug.Assert( _enteredThreadId == Thread.CurrentThread.ManagedThreadId );
            Debug.Assert( newTopic != null && _topic != newTopic );
            _topic = newTopic;
            _output.BridgeTarget.TargetTopicChanged( newTopic, fileName, lineNumber );
            MonoParameterSafeCall( ( client, topic ) => client.OnTopicChanged( topic, fileName, lineNumber ), newTopic );
            if( Interlocked.Exchange( ref _signalFlag, 0 ) == 1 ) DoResyncActualFilter();
            SendTopicLogLine( fileName, lineNumber );
        }

        void SendTopicLogLine( [CallerFilePath]string fileName = null, [CallerLineNumber]int lineNumber = 0)
        {
            _lastLogTime.Value = new DateTimeStamp( _lastLogTime.Value, DateTime.UtcNow );
            DoUnfilteredLog( new ActivityMonitorLogData( LogLevel.Info, null, Tags.MonitorTopicChanged, SetTopicPrefix + _topic, _lastLogTime.Value, fileName, lineNumber ) );
        }

        /// <summary>
        /// Gets or sets the tags of this monitor: any subsequent logs will be tagged by these tags.
        /// The <see cref="CKTag"/> must be registered in <see cref="ActivityMonitor.Tags"/>.
        /// Modifications to this property are scoped to the current Group since when a Group is closed, this
        /// property (like <see cref="MinimalFilter"/>) is automatically restored to its original value (captured when the Group was opened).
        /// </summary>
        public CKTag AutoTags 
        {
            get { return _currentTag; }
            set
            {
                if( value == null ) value = Tags.Empty;
                else if( value.Context != Tags.Context ) throw new ArgumentException( ActivityMonitorResources.ActivityMonitorTagMustBeRegistered, "value" );
                if( _currentTag != value )
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

        void DoSetAutoTags( CKTag newTags )
        {
            Debug.Assert( _enteredThreadId == Thread.CurrentThread.ManagedThreadId );
            Debug.Assert( newTags != null && _currentTag != newTags && newTags.Context == Tags.Context );
            _currentTag = newTags;
            _output.BridgeTarget.TargetAutoTagsChanged( newTags );
            MonoParameterSafeCall( ( client, tags ) => client.OnAutoTagsChanged( tags ), newTags );
        }

        /// <summary>
        /// Called by IActivityMonitorBoundClient clients to initialize Topic and AutoTag from 
        /// inside their SetMonitor or any other methods provided that a reentrant and concurrent lock 
        /// has been obtained (otherwise an InvalidOperationException is thrown).
        /// </summary>
        void IActivityMonitorImpl.InitializeTopicAndAutoTags( string newTopic, CKTag newTags, string fileName, int lineNumber )
        {
            RentrantOnlyCheck();
            if( newTopic != null && _topic != newTopic ) DoSetTopic( newTopic, fileName, lineNumber );
            if( newTags != null && _currentTag != newTags ) DoSetAutoTags( newTags );
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
            Debug.Assert( _enteredThreadId == Thread.CurrentThread.ManagedThreadId );
            Debug.Assert( _configuredFilter != value );
            _configuredFilter = value;
            UpdateActualFilter();
        }

        void UpdateActualFilter()
        {
            Debug.Assert( _enteredThreadId == Thread.CurrentThread.ManagedThreadId );
            LogFilter newLevel = _configuredFilter.Combine( _clientFilter );
            if( newLevel != _actualFilter )
            {
                _actualFilter = newLevel;
                _output.BridgeTarget.TargetActualFilterChanged();
            }
        }

        LogFilter HandleBoundClientsSignal()
        {
            Debug.Assert( _enteredThreadId == Thread.CurrentThread.ManagedThreadId );
            
            LogFilter minimal = LogFilter.Undefined;
            List<IActivityMonitorClient> clientsToRemove = null;
            foreach( var l in _output.Clients )
            {
                IActivityMonitorBoundClient bound = l as IActivityMonitorBoundClient;
                if( bound != null )
                {
                    try
                    {
                        if( bound.IsDead )
                        {
                            if( clientsToRemove == null ) clientsToRemove = new List<IActivityMonitorClient>();
                            clientsToRemove.Add( l );
                        }
                        else
                        {
                            minimal = minimal.Combine( bound.MinimalFilter );
                        }
                    }
                    catch( Exception exCall )
                    {
                        CriticalErrorCollector.Add( exCall, l.GetType().FullName );
                        if( clientsToRemove == null ) clientsToRemove = new List<IActivityMonitorClient>();
                        clientsToRemove.Add( l );
                    }
                }
            }
            if( clientsToRemove != null )
            {
                foreach( var l in clientsToRemove ) _output.ForceRemoveCondemnedClient( l );
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
        public void UnfilteredLog( ActivityMonitorLogData data )
        {
            if( data == null || data.MaskedLevel == LogLevel.None ) return;
            ReentrantAndConcurrentCheck();
            try
            {
                DoUnfilteredLog( data );
            }
            finally
            {
                ReentrantAndConcurrentRelease();
            }
        }

        void DoUnfilteredLog( ActivityMonitorLogData data, bool trustDataTime = false )
        {
            Debug.Assert( _enteredThreadId == Thread.CurrentThread.ManagedThreadId );
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

            if( trustDataTime ) data.CombineTags( _currentTag );
            else _lastLogTime.Value = data.CombineTagsAndAdjustLogTime( _currentTag, _lastLogTime.Value );

            List<IActivityMonitorClient> buggyClients = null;
            foreach( var l in _output.Clients )
            {
                try
                {
                    l.OnUnfilteredLog( data );
                }
                catch( Exception exCall )
                {
                    CriticalErrorCollector.Add( exCall, l.GetType().FullName );
                    if( buggyClients == null ) buggyClients = new List<IActivityMonitorClient>();
                    buggyClients.Add( l );
                }
            }
            if( buggyClients != null )
            {
                foreach( var l in buggyClients ) _output.ForceRemoveCondemnedClient( l );
                _clientFilter = HandleBoundClientsSignal();
                UpdateActualFilter();
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
        /// Note that this automatic configuration restoration works even if the group has been filtered and rejected.
        /// </para>
        /// </remarks>
        public virtual IDisposableGroup UnfilteredOpenGroup( ActivityMonitorGroupData data )
        {
            ReentrantAndConcurrentCheck();
            try
            {
                return DoOpenGroup( data );
            }
            finally
            {
                ReentrantAndConcurrentRelease();
            }
        }

        IDisposableGroup DoOpenGroup( ActivityMonitorGroupData data, bool trustDataTime = false )
        {
            Debug.Assert( _enteredThreadId == Thread.CurrentThread.ManagedThreadId );

            int idxNext = _current != null ? _current.Index + 1 : 0;
            if( idxNext == _groups.Length )
            {
                Array.Resize( ref _groups, _groups.Length * 2 );
                for( int i = idxNext; i < _groups.Length; ++i ) _groups[i] = new Group( this, i );
            }
            _current = _groups[idxNext];
            if( data == null || data.MaskedLevel == LogLevel.None )
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
            if( trustDataTime ) data.CombineTags( _currentTag );
            else _lastLogTime.Value = data.CombineTagsAndAdjustLogTime( _currentTag, _lastLogTime.Value );
            _current.Initialize( data );
            _currentUnfiltered = _current;
            MonoParameterSafeCall( ( client, group ) => client.OnOpenGroup( group ), _current ); 
            return _current;
        }

        /// <summary>
        /// Closes the current <see cref="Group"/>. Optional parameter is polymorphic. It can be a string, a <see cref="ActivityLogGroupConclusion"/>, 
        /// a <see cref="List{T}"/> or an <see cref="IEnumerable{T}"/> of ActivityLogGroupConclusion, or any object with an overridden <see cref="Object.ToString"/> method. 
        /// See remarks (especially for List&lt;ActivityLogGroupConclusion&gt;).
        /// </summary>
        /// <param name="userConclusion">Optional string, enumerable of <see cref="ActivityLogGroupConclusion"/>) or object to conclude the group. See remarks.</param>
        /// <param name="logTime">Times-tamp of the group closing.</param>
        /// <remarks>
        /// An untyped object is used here to easily and efficiently accommodate both string and already existing ActivityLogGroupConclusion.
        /// When a List&lt;ActivityLogGroupConclusion&gt; is used, it will be directly used to collect conclusion objects (new conclusions will be added to it). This is an optimization.
        /// </remarks>
        public virtual bool CloseGroup( DateTimeStamp logTime, object userConclusion = null )
        {
            ReentrantAndConcurrentCheck();
            try
            {
                return DoCloseGroup( logTime, userConclusion );
            }
            finally
            {
                ReentrantAndConcurrentRelease();
            }
        }

        bool DoCloseGroup( DateTimeStamp logTime, object userConclusion, bool trustDataTime = false )
        {
            Debug.Assert( _enteredThreadId == Thread.CurrentThread.ManagedThreadId );
            Group g = _current;
            if( g == null ) return false;
            // Handles the rejected case first (easiest).
            if( g.IsRejectedGroup )
            {
                if( g.SavedMonitorFilter != _configuredFilter ) DoSetConfiguredFilter( g.SavedMonitorFilter );
                _currentTag = g.SavedMonitorTags;
                _current = g.Index > 0 ? _groups[g.Index - 1] : null;
            }
            else
            {
                #region Closing the group
                if( trustDataTime )
                {
                    g.CloseLogTime = logTime;
                }
                else
                {
                    g.CloseLogTime = _lastLogTime.Value = new DateTimeStamp( _lastLogTime.Value, logTime.IsKnown ? logTime : DateTimeStamp.UtcNow );
                }
                var conclusions = userConclusion as List<ActivityLogGroupConclusion>;
                if( conclusions == null && userConclusion != null )
                {
                    conclusions = new List<ActivityLogGroupConclusion>();
                    if( userConclusion is string s )
                    {
                        conclusions.Add( new ActivityLogGroupConclusion( Tags.UserConclusion, s ) );
                    }
                    else
                    {
                        if( userConclusion is ActivityLogGroupConclusion )
                        {
                            conclusions.Add( (ActivityLogGroupConclusion)userConclusion );
                        }
                        else
                        {
                            IEnumerable<ActivityLogGroupConclusion> multi = userConclusion as IEnumerable<ActivityLogGroupConclusion>;
                            if( multi != null ) conclusions.AddRange( multi );
                            else conclusions.Add( new ActivityLogGroupConclusion( Tags.UserConclusion, userConclusion.ToString() ) );
                        }
                    }
                }
                g.GroupClosing( ref conclusions );

                bool hasBuggyClients = false;
                List<IActivityMonitorClient> buggyClients = null;
                foreach( var l in _output.Clients )
                {
                    try
                    {
                        l.OnGroupClosing( g, ref conclusions );
                    }
                    catch( Exception exCall )
                    {
                        CriticalErrorCollector.Add( exCall, l.GetType().FullName );
                        if( buggyClients == null ) buggyClients = new List<IActivityMonitorClient>();
                        buggyClients.Add( l );
                    }
                }
                if( buggyClients != null )
                {
                    foreach( var l in buggyClients ) _output.ForceRemoveCondemnedClient( l );
                    buggyClients.Clear();
                    hasBuggyClients = true;
                }
                if( g.SavedMonitorFilter != _configuredFilter ) DoSetConfiguredFilter( g.SavedMonitorFilter );
                _currentTag = g.SavedMonitorTags;
                _current = g.Index > 0 ? _groups[g.Index - 1] : null;
                _currentUnfiltered = (Group)g.Parent;

                var sentConclusions = conclusions != null ? conclusions.ToArray() : Util.Array.Empty<ActivityLogGroupConclusion>();
                foreach( var l in _output.Clients )
                {
                    try
                    {
                        l.OnGroupClosed( g, sentConclusions );
                    }
                    catch( Exception exCall )
                    {
                        CriticalErrorCollector.Add( exCall, l.GetType().FullName );
                        if( buggyClients == null ) buggyClients = new List<IActivityMonitorClient>();
                        buggyClients.Add( l );
                    }
                }
                if( buggyClients != null )
                {
                    foreach( var l in buggyClients ) _output.ForceRemoveCondemnedClient( l );
                    hasBuggyClients = true;
                }
                if( hasBuggyClients )
                {
                    _clientFilter = HandleBoundClientsSignal();
                    UpdateActualFilter();
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
            Debug.Assert( _enteredThreadId == Thread.CurrentThread.ManagedThreadId );
            List<IActivityMonitorClient> buggyClients = null;
            foreach( var l in _output.Clients )
            {
                try
                {
                    call( l, arg );
                }
                catch( Exception exCall )
                {
                    CriticalErrorCollector.Add( exCall, l.GetType().FullName );
                    if( buggyClients == null ) buggyClients = new List<IActivityMonitorClient>();
                    buggyClients.Add( l );
                }
            }
            if( buggyClients != null )
            {
                foreach( var l in buggyClients ) _output.ForceRemoveCondemnedClient( l );
                _clientFilter = HandleBoundClientsSignal();
                UpdateActualFilter();
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

        void RentrantOnlyCheck()
        {
            if( _enteredThreadId != Thread.CurrentThread.ManagedThreadId ) throw new InvalidOperationException( ActivityMonitorResources.ActivityMonitorReentrancyCallOnly );
        }

        void ReentrantAndConcurrentCheck()
        {
            int currentThreadId = Thread.CurrentThread.ManagedThreadId;
            int alreadyEnteredId;
            if( (alreadyEnteredId = Interlocked.CompareExchange( ref _enteredThreadId, currentThreadId, 0 )) != 0 )
            {
                if( alreadyEnteredId == currentThreadId )
                {
                    throw new InvalidOperationException( ActivityMonitorResources.ActivityMonitorReentrancyError );
                }
                else
                {
                    throw new InvalidOperationException( ActivityMonitorResources.ActivityMonitorConcurrentThreadAccess );
                }
            }
        }


        /// <summary>
        /// Checks only for concurrency issues. 
        /// False if a call already exists (reentrant call): when true is returned, ReentrantAndConcurrentRelease must be called.
        /// </summary>
        /// <returns>False for a reentrant call, true otherwise.</returns>
        bool ConcurrentOnlyCheck()
        {
            int currentThreadId = Thread.CurrentThread.ManagedThreadId;
            int alreadyEnteredId;
            if( (alreadyEnteredId = Interlocked.CompareExchange( ref _enteredThreadId, currentThreadId, 0 )) != 0 )
            {
                if( alreadyEnteredId == currentThreadId )
                {
                    return false;
                }
                else
                {
                    throw new InvalidOperationException( ActivityMonitorResources.ActivityMonitorConcurrentThreadAccess );
                }
            }
            return true;
        }

        void ReentrantAndConcurrentRelease()
        {
            Debug.Assert( Thread.CurrentThread.ManagedThreadId == _enteredThreadId );
            if( _internalMonitor != null && _internalMonitor.Recorder.History.Count > 0 )
            {
                DoReplayInternalLogs();
            }
            // Reset and check even in Release.
            int currentThreadId = Thread.CurrentThread.ManagedThreadId;
            if( Interlocked.CompareExchange( ref _enteredThreadId, 0, currentThreadId ) != currentThreadId )
            {
                throw new CKException( ActivityMonitorResources.ActivityMonitorReentrancyReleaseError, _enteredThreadId, Thread.CurrentThread.Name, currentThreadId );
            }
        }

    }
}
