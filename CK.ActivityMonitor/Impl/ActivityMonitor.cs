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
using System.Runtime.InteropServices;

namespace CK.Core
{
    /// <summary>
    /// Concrete implementation of <see cref="IActivityMonitor"/>.
    /// </summary>
    public sealed partial class ActivityMonitor : IActivityMonitorImpl
    {
        /// <summary>
        /// Prefix used by <see cref="IActivityMonitor.SetTopic"/> is "Topic: ".
        /// </summary>
        static public readonly string SetTopicPrefix = "Topic: ";

        /// <summary>
        /// String to use to break the current <see cref="LogLevel"/> (as if a different <see cref="LogLevel"/> was used).
        /// </summary>
        static public readonly string ParkLevel = "PARK-LEVEL";

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
                Throw.CheckArgument( value.Line != LogLevelFilter.None && value.Group != LogLevelFilter.None );
                _defaultFilterLevel = value;
            }
        }

        /// <summary>
        /// The automatic configuration actions.
        /// Registers actions via +=, unregister with -= operator.
        /// Simply sets it to null to clear all currently registered actions (this, of course, only from tests and not in real code).
        /// </summary>
        static public Action<IActivityMonitor>? AutoConfiguration;

        /// <summary>
        /// The no-log text replaces any null or empty log text.
        /// </summary>
        public const string NoLogText = "[no-log]";

        /// <summary>
        /// <see cref="IActivityMonitor.UniqueId"/> must be at least 4 characters long
        /// and not contain any <see cref="Char.IsWhiteSpace(char)"/>.
        /// </summary>
        public const int MinMonitorUniqueIdLength = 4;

        /// <summary>
        /// The name of the fake monitor for external logs.
        /// </summary>
        public const string ExternalLogMonitorUniqueId = "§ext";

        static readonly FastUniqueIdGenerator _generatorId;

        static ActivityMonitor()
        {
            AutoConfiguration = null;
            _defaultFilterLevel = LogFilter.Trace;
            _generatorId = new FastUniqueIdGenerator();
            _staticLogger = new LoggerStatic();
        }

        Group[] _groups;
        Group? _current;
        Group? _currentUnfiltered;
        int _currentDepth;
        readonly OutputImpl _output;
        string _topic;
        //
        volatile StackTrace? _currentStackTrace;
        CKTrait _autoTags;
        LogFilter _actualFilter;
        int _enteredThreadId;
        int _signalFlag;
        LogFilter _configuredFilter;
        LogFilter _clientFilter;
        bool _trackStackTrace;

        readonly string _uniqueId;
        // The recorder holds the InternalMonitor.
        LogsRecorder? _recorder;

        readonly Logger? _logger;
        readonly ActivityMonitorLogData.IFactory _dataFactory;
        // The provider has the priority if it is not null.
        readonly DateTimeStampProvider? _stampProvider;
        DateTimeStamp _lastLogTime;

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitor"/> that applies all <see cref="AutoConfiguration"/>,
        /// has an empty <see cref="Topic"/> initially set and no <see cref="ParallelLogger"/>.
        /// </summary>
        public ActivityMonitor()
            : this( _generatorId.GetNextString(), Tags.Empty, true, null )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitor"/> that applies all <see cref="AutoConfiguration"/>,
        /// has an empty <see cref="Topic"/> initially set and provides a thread safe <see cref="ParallelLogger"/>.
        /// </summary>
        public ActivityMonitor( DateTimeStampProvider stampProvider )
            : this( _generatorId.GetNextString(), Tags.Empty, true, stampProvider )
        {
            Throw.CheckNotNullArgument( stampProvider );
        }

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitor"/> that applies all <see cref="AutoConfiguration"/> and has an initial <see cref="Topic"/> set.
        /// </summary>
        /// <param name="topic">Initial topic (can be null).</param>
        /// <param name="stampProvider">
        /// Optional thread safe time stamp provider that must be used when this monitor must provide a thread safe <see cref="ParallelLogger"/>.
        /// </param>
        public ActivityMonitor( string topic, DateTimeStampProvider? stampProvider = null )
            : this( _generatorId.GetNextString(), Tags.Empty, true, stampProvider )
        {
            if( topic != null ) SetTopic( topic );
        }

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitor"/> that optionally applies <see cref="AutoConfiguration"/> and with an initial topic.
        /// </summary>
        /// <param name="applyAutoConfigurations">Whether <see cref="AutoConfiguration"/> should be applied.</param>
        /// <param name="topic">Optional initial topic (can be null).</param>
        /// <param name="stampProvider">
        /// Optional thread safe time stamp provider that must be used when this monitor must provide a thread safe <see cref="ParallelLogger"/>.
        /// </param>
        public ActivityMonitor( bool applyAutoConfigurations, string? topic = null, DateTimeStampProvider? stampProvider = null )
            : this( _generatorId.GetNextString(), Tags.Empty, applyAutoConfigurations, stampProvider )
        {
            if( topic != null ) SetTopic( topic );
        }

        ActivityMonitor( string uniqueId,
                         CKTrait tags,
                         ActivityMonitorOptions options,
                         Logger? logger = null )
        {
            if( uniqueId == null
                || uniqueId.Length < MinMonitorUniqueIdLength
                || uniqueId.Any( c => Char.IsWhiteSpace( c ) ) )
            {
                Throw.ArgumentException( nameof( uniqueId ), $"Monitor UniqueId must be at least {MinMonitorUniqueIdLength} long and not contain any whitespace." );
            }
            _uniqueId = uniqueId;
            if( (options & ActivityMonitorOptions.WithParallel) != 0 )
            {
                
            }
            _groups = new Group[8];
            for( int i = 0; i < _groups.Length; ++i ) _groups[i] = new Group( this, i );
            _autoTags = tags ?? Tags.Empty;
            _trackStackTrace = _autoTags.AtomicTraits.Contains( Tags.StackTrace );
            _topic = String.Empty;
            _output = new OutputImpl( this );
            if( (_stampProvider = stampProvider) != null )
            {
                // logger parameter is used only by the LogRecorder internal monitor constructor.
                _logger = logger ?? new Logger( this );
                _dataFactory = _logger.FactoryForMonitor;
            }
            else
            {
                _dataFactory = _output;
            }
            if( applyAutoConfigurations )
            {
                AutoConfiguration?.Invoke( this );
            }
        }

        /// <inheritdoc />
        public string UniqueId => _uniqueId;

        /// <inheritdoc />
        public IActivityMonitorOutput Output => _output;

        /// <inheritdoc />
        public string Topic => _topic;

        /// <inheritdoc />
        public IActivityLogger? ParallelLogger => _logger;

        public ActivityMonitorLogData.IFactory DataFactory => (ActivityMonitorLogData.IFactory?)_logger ?? _output;

        /// <inheritdoc />
        public void SetTopic( string? newTopic, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0 )
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
            MonoParameterSafeCall( ( client, topic ) => client.OnTopicChanged( topic!, fileName, lineNumber ), newTopic );
            if( Interlocked.Exchange( ref _signalFlag, 0 ) == 1 ) DoResyncActualFilter();
            SendTopicLogLine( fileName, lineNumber );
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        void SendTopicLogLine( [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0 )
        {
            var d = DataFactory.CreateLogData( LogLevel.Info | LogLevel.IsFiltered,
                                               _autoTags | Tags.MonitorTopicChanged,
                                               SetTopicPrefix + _topic,
                                               null,
                                               fileName,
                                               lineNumber );
            DoUnfilteredLog( ref d );
        }

        /// <inheritdoc />
        [AllowNull]
        public CKTrait AutoTags
        {
            get { return _autoTags; }
            set
            {
                if( value == null ) value = Tags.Empty;
                else if( value.Context != Tags.Context ) Throw.ArgumentException( nameof( value ), ActivityMonitorResources.ActivityMonitorTagMustBeRegistered );
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
            _autoTags = newTags;
            _trackStackTrace = _autoTags.AtomicTraits.Contains( Tags.StackTrace );
            MonoParameterSafeCall( ( client, tags ) => client.OnAutoTagsChanged( tags ), newTags );
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public LogFilter ActualFilter
        {
            get
            {
                if( Interlocked.Exchange( ref _signalFlag, 0 ) == 1 ) ResyncActualFilter();
                return _actualFilter;
            }
        }

        LogLevelFilter IActivityLogger.ActualFilter
        {
            get
            {
                if( Interlocked.Exchange( ref _signalFlag, 0 ) == 1 ) ResyncActualFilter();
                return _actualFilter.Line;
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

        /// <inheritdoc />
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
            Debug.Assert( !data.IsParallel );

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

        /// <inheritdoc />
        public IDisposableGroup UnfilteredOpenGroup( ref ActivityMonitorLogData data )
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
            Debug.Assert( !data.IsParallel );

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
            _current.Initialize( ref data );
            _currentUnfiltered = _current;
            MonoParameterSafeCall( ( client, group ) => client.OnOpenGroup( group ), _current );
            ++_currentDepth;
            return _current;
        }

        /// <inheritdoc />
        public bool CloseGroup( object? userConclusion = null, DateTimeStamp explicitLogTime = default )
        {
            bool isNoReentrant = ConcurrentOnlyCheck();
            try
            {
                return DoCloseGroup( userConclusion, explicitLogTime );
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
                    g.CloseLogTime = _stampProvider != null
                                        ? _stampProvider.GetNextNow()
                                        : (_lastLogTime = new DateTimeStamp( _lastLogTime, DateTime.UtcNow ));
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
                --_currentDepth;

                var sentConclusions = conclusions ?? (IReadOnlyList<ActivityLogGroupConclusion>)Array.Empty<ActivityLogGroupConclusion>();
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

        /// <inheritdoc />
        public DateTimeStampProvider? SafeStampProvider => _stampProvider;

        /// <inheritdoc />
        public DateTimeStamp GetAndUpdateNextLogTime()
        {
            return _stampProvider != null ? _stampProvider.GetNextNow() : (_lastLogTime = new DateTimeStamp( _lastLogTime, DateTime.UtcNow ));
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

        sealed class RAndCChecker : IDisposable
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RentrantOnlyCheck()
        {
            if( _enteredThreadId != Environment.CurrentManagedThreadId ) Throw.InvalidOperationException( ActivityMonitorResources.ActivityMonitorReentrancyCallOnly );
        }

        void ReentrantAndConcurrentCheck()
        {
            int currentThreadId = Environment.CurrentManagedThreadId;
            int alreadyEnteredId;
            if( (alreadyEnteredId = Interlocked.CompareExchange( ref _enteredThreadId, currentThreadId, 0 )) != 0 )
            {
                if( alreadyEnteredId == currentThreadId )
                {
                    Throw.CKException( String.Format( ActivityMonitorResources.ActivityMonitorReentrancyError, _uniqueId ) );
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
            Throw.CKException( msg );
        }

        void ReentrantAndConcurrentRelease()
        {
            if( _recorder != null && _recorder.History.Count > 0 )
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

    }
}
