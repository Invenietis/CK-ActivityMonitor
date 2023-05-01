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
    public sealed partial class ActivityMonitor : IActivityMonitor, IActivityMonitorImpl
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
#pragma warning disable CA2211 // Non-constant fields should not be visible
        static public Action<IActivityMonitor>? AutoConfiguration;
#pragma warning restore CA2211 // Non-constant fields should not be visible

        /// <summary>
        /// The no-log text replaces any null or empty log text.
        /// </summary>
        public const string NoLogText = "[no-log]";

        /// <summary>
        /// A monitor identifier must be at least 4 characters long
        /// and not contain any <see cref="Char.IsWhiteSpace(char)"/>.
        /// </summary>
        public const int MinMonitorUniqueIdLength = 4;

        /// <summary>
        /// The name of the fake monitor for external logs.
        /// </summary>
        public const string ExternalLogMonitorUniqueId = "§ext";

        /// <summary>
        /// The name for <see cref="StaticLogger"/> logs.
        /// </summary>
        public const string StaticLogMonitorUniqueId = "§§§§";

        static readonly FastUniqueIdGenerator _generatorId;

        static ActivityMonitor()
        {
            AutoConfiguration = null;
            _defaultFilterLevel = LogFilter.Trace;
            _generatorId = new FastUniqueIdGenerator();
            _staticLogger = new LoggerStatic();
        }

        Group? _current;
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
        InitialLogsReplayPseudoClient? _initialReplay;

        readonly Logger _logger;
        DateTimeStamp _lastLogTime;

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitor"/> that applies all <see cref="AutoConfiguration"/>,
        /// has an empty <see cref="Topic"/> initially set.
        /// </summary>
        public ActivityMonitor()
            : this( _generatorId.GetNextString(), Tags.Empty, ActivityMonitorOptions.Default )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitor"/>.
        /// </summary>
        /// <param name="options">Creation options.</param>
        /// <param name="topic">Optional initial topic.</param>
        /// <param name="initialTags">Optional initial tags.</param>
        public ActivityMonitor( ActivityMonitorOptions options, string? topic = null, CKTrait? initialTags = null )
            : this( _generatorId.GetNextString(), initialTags, options )
        {
            if( topic != null ) SetTopic( topic );
        }

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitor"/> with a non null or empty initial topic.
        /// </summary>
        /// <param name="topic">Initial topic.</param>
        /// <param name="options">Optional creation options.</param>
        /// <param name="initialTags">Optional initial tags.</param>
        public ActivityMonitor( string topic, ActivityMonitorOptions options = ActivityMonitorOptions.Default, CKTrait? initialTags = null )
            : this( _generatorId.GetNextString(), initialTags, options )
        {
            Throw.CheckNotNullOrEmptyArgument( topic );
            SetTopic( topic );
        }

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitor"/> and calls <see cref="ActivityMonitorExtension.StartDependentActivity">StartDependentActivity</see>
        /// with the <paramref name="token"/>.
        /// </summary>
        /// <param name="token">Dependent token that starts this activity.</param>
        /// <param name="options">Optional creation options.</param>
        /// <param name="initialTags">Optional initial tags.</param>
        public ActivityMonitor( Token token, ActivityMonitorOptions options = ActivityMonitorOptions.Default, CKTrait? initialTags = null )
            : this( _generatorId.GetNextString(), initialTags, options )
        {
            Throw.CheckNotNullArgument( token );
            _ = this.StartDependentActivity( token );
        }

        ActivityMonitor( string uniqueId,
                         CKTrait? tags,
                         ActivityMonitorOptions options,
                         Logger? logger = null )
        {
            Debug.Assert( uniqueId != null && uniqueId.Length >= MinMonitorUniqueIdLength && !uniqueId.Any( c => Char.IsWhiteSpace( c ) ) );
            _uniqueId = uniqueId;
            _autoTags = tags ?? Tags.Empty;
            _trackStackTrace = _autoTags.AtomicTraits.Contains( Tags.StackTrace );
            _topic = String.Empty;
            _output = new OutputImpl( this );
            if( logger != null )
            {
                // Explicit logger parameter is used only for the LogRecorder internal monitor.
                _logger = logger;
            }
            else
            {
                _logger = new Logger( this );
                if( (options & ActivityMonitorOptions.WithInitialReplay) != 0 )
                {
                    _initialReplay = new InitialLogsReplayPseudoClient( this );
                }
                if( (options & ActivityMonitorOptions.SkipAutoConfiguration) == 0 )
                {
                    AutoConfiguration?.Invoke( this );
                }
            }
        }

        /// <inheritdoc />
        public string UniqueId => _uniqueId;

        /// <inheritdoc />
        public IActivityMonitorOutput Output => _output;

        /// <inheritdoc />
        public string Topic => _topic;

        /// <inheritdoc />
        public IParallelLogger ParallelLogger => _logger;

        /// <inheritdoc />
        public void SetTopic( string? newTopic, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0 )
        {
            newTopic ??= String.Empty;
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
            var d = _logger.CreateLogLineData( false,
                                               LogLevel.Info | LogLevel.IsFiltered,
                                               _autoTags | Tags.TopicChanged,
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
            get => _autoTags;
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
            MonoParameterSafeCall( static ( client, tags ) => client.OnAutoTagsChanged( tags ), newTags );
        }

        /// <inheritdoc />
        public LogFilter MinimalFilter
        {
            get => _configuredFilter;
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

        LogLevelFilter IActivityLineEmitter.ActualFilter => ActualFilter.Line;

        /// <inheritdoc />
        public LogFilter ActualFilter
        {
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            get
            {
                if( Interlocked.Exchange( ref _signalFlag, 0 ) == 1 )
                {
                    ResyncActualFilter();
                }
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
                            buggyClients ??= new List<IActivityMonitorClient>();
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

        internal void OnClientMinimalFilterChanged( LogFilter oldLevel, LogFilter newLevel )
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
            Throw.CheckArgument( !data.IsOpenGroup );
            SendUnfilteredLog( ref data );
        }

        internal void ReplayUnfilteredLog( ref ActivityMonitorLogData data, IActivityMonitorClient? single = null )
        {
            Debug.Assert( !data.IsOpenGroup );
            if( single == null )
            {
                SendUnfilteredLog( ref data );
            }
            else
            {
                List<IActivityMonitorClient>? buggyClients = null;
                try
                {
                    single.OnUnfilteredLog( ref data );
                }
                catch( Exception exCall )
                {
                    if( !InternalLogUnhandledClientError( exCall, single, ref buggyClients ) ) throw;
                }
                if( buggyClients != null )
                {
                    HandleBuggyClients( buggyClients );
                }

            }
        }

        void SendUnfilteredLog( ref ActivityMonitorLogData data )
        {
            List<IActivityMonitorClient>? buggyClients = null;
            _initialReplay?.OnUnfilteredLog( ref data );
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
            _current = _current?.EnsureNext() ?? new Group( this, null );
            if( data.MaskedLevel == LogLevel.None )
            {
                _current.InitializeRejectedGroup();
                return _current;
            }
            Throw.CheckArgument( data.IsOpenGroup );
            _current.Initialize( ref data );
            _initialReplay?.OnOpenGroup( ref data );
            MonoParameterSafeCall( static ( client, group ) => client.OnOpenGroup( group ), _current );
            return _current;
        }

        internal void ReplayOpenGroup( ref ActivityMonitorLogData data, IActivityMonitorClient? single = null )
        {
            Debug.Assert( _enteredThreadId == Environment.CurrentManagedThreadId );

            _current = _current?.EnsureNext() ?? new Group( this, null );
            _current.Initialize( ref data );
            if( single == null )
            {
                _initialReplay?.OnOpenGroup( ref data );
                MonoParameterSafeCall( static ( client, group ) => client.OnOpenGroup( group ), _current );
            }
            else
            {
                List<IActivityMonitorClient>? buggyClients = null;
                try
                {
                    single.OnOpenGroup( _current );
                }
                catch( Exception exCall )
                {
                    if( !InternalLogUnhandledClientError( exCall, single, ref buggyClients ) ) throw;
                }
                if( buggyClients != null )
                {
                    HandleBuggyClients( buggyClients );
                }

            }
        }

        /// <inheritdoc />
        public bool CloseGroup( object? userConclusion = null )
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

        bool DoCloseGroup( object? userConclusion )
        {
            Debug.Assert( _enteredThreadId == Environment.CurrentManagedThreadId );
            Group? g = _current;
            if( g == null ) return false;
            // Handles the rejected case first (easiest).
            if( g.IsRejectedGroup )
            {
                g.CloseGroup();
                return true;
            }
            #region Setup the conclusions.
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
            g.AddGetConclusionText( ref conclusions );
            #endregion

            // Obtains the close log time: this decrements the _currentDepth in the lock.
            g.CloseLogTime = _logger.GetLogTimeForClosingGroup();

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
            var sentConclusions = conclusions ?? (IReadOnlyList<ActivityLogGroupConclusion>)Array.Empty<ActivityLogGroupConclusion>();

            g.CloseGroup();
            SendClosedGroup( g, sentConclusions );
            return true;
        }

        internal void ReplayClosedGroup( DateTimeStamp closeLogTime, IReadOnlyList<ActivityLogGroupConclusion> conclusions, IActivityMonitorClient? single = null )
        {
            Debug.Assert( _enteredThreadId == Environment.CurrentManagedThreadId );
            Group? g = _current;
            Debug.Assert( g != null && !g.IsRejectedGroup );
            g.CloseLogTime = closeLogTime;
            g.CloseGroup();
            if( single == null )
            {
                SendClosedGroup( g, conclusions );
            }
            else
            {
                List<IActivityMonitorClient>? buggyClients = null;
                try
                {
                    single.OnGroupClosed( g, conclusions );
                }
                catch( Exception exCall )
                {
                    if( !InternalLogUnhandledClientError( exCall, single, ref buggyClients ) ) throw;
                }
                if( buggyClients != null )
                {
                    HandleBuggyClients( buggyClients );
                }
            }
        }

        void SendClosedGroup( Group g, IReadOnlyList<ActivityLogGroupConclusion> sentConclusions )
        {
            Debug.Assert( _enteredThreadId == Environment.CurrentManagedThreadId );
            _initialReplay?.OnGroupClosed( g.CloseLogTime, sentConclusions );
            List<IActivityMonitorClient>? buggyClients = null;
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

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        void RentrantOnlyCheck()
        {
            if( _enteredThreadId != Environment.CurrentManagedThreadId ) Throw.InvalidOperationException( ActivityMonitorResources.ActivityMonitorReentrancyCallOnly );
        }

        internal void ReentrantAndConcurrentCheck()
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

        internal void ReentrantAndConcurrentRelease()
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

        ActivityMonitorLogData IActivityLineEmitter.CreateActivityMonitorLogData( LogLevel level,
                                                                                  CKTrait finalTags,
                                                                                  string? text,
                                                                                  object? exception,
                                                                                  string? fileName,
                                                                                  int lineNumber,
                                                                                  bool isOpenGroup )
        {
            return isOpenGroup
                    ? _logger.CreateOpenGroupData( false, level, finalTags, text, exception, fileName, lineNumber )
                    : _logger.CreateLogLineData( false, level, finalTags, text, exception, fileName, lineNumber );
        }
    }
}
