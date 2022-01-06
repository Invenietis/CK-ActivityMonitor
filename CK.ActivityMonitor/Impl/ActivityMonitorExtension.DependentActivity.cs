using System;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    /// <summary>
    /// Provides extension methods for <see cref="IActivityMonitor"/> and other types from the Activity monitor framework.
    /// </summary>
    public static partial class ActivityMonitorExtension
    {
        /// <summary>
        /// Offers dependent token creation and launching.
        /// </summary>
        public readonly struct DependentSender
        {
            readonly IActivityMonitor _monitor;
            readonly string? _fileName;
            readonly int _lineNumber;

            internal DependentSender( IActivityMonitor m, string? f, int n )
            {
                _monitor = m;
                _fileName = f;
                _lineNumber = n;
            }

            /// <summary>
            /// <para>
            /// Creates a token for a dependent activity that will use the current monitor's topic.
            /// </para>
            /// <para>
            /// By default (when <paramref name="delayedLaunch"/> is false), a line with <see cref="ActivityMonitor.Tags.CreateDependentActivity"/> is 
            /// logged that describes the creation of the token. If <paramref name="delayedLaunch"/> is true, the actual launch of the dependent
            /// activity must be signaled thanks to <see cref="Launch(ActivityMonitor.DependentToken)"/> (otherwise there will 
            /// be no way to bind the two activities). 
            /// </para>
            /// <para>
            /// The extension method <see cref="ActivityMonitorExtension.StartDependentActivity(IActivityMonitor, ActivityMonitor.DependentToken, string?, int)">StartDependentActivity( token )</see>
            /// must be used on the target monitor to signal the activity.
            /// </para>
            /// </summary>
            /// <param name="delayedLaunch">True to use <see cref="Launch(ActivityMonitor.DependentToken)"/> later to indicate the actual launch of the dependent activity.</param>
            /// <returns>A dependent token.</returns>
            public ActivityMonitor.DependentToken CreateToken( bool delayedLaunch = false )
            {
                var t = ActivityMonitor.DependentToken.CreateWithMonitorTopic( _monitor, delayedLaunch, out string msg );
                if( delayedLaunch ) t.DelayedLaunchMessage = msg;
                else _monitor.UnfilteredLog( LogLevel.Info, ActivityMonitor.Tags.CreateDependentActivity, msg, null, _fileName, _lineNumber );
                return t;
            }

            /// <summary>
            /// <para>
            /// Creates a token for a dependent activity that will be bound to a specified topic (or that will not change the dependent monitor's topic
            /// if null is specified).
            /// </para>
            /// <para>
            /// By default (when <paramref name="delayedLaunch"/> is false), a line with <see cref="ActivityMonitor.Tags.CreateDependentActivity"/> is 
            /// logged that describes the creation of the token.
            /// </para>
            /// <para>
            /// If <paramref name="delayedLaunch"/> is true, the actual launch of the dependent activity must be 
            /// signaled thanks to <see cref="Launch(ActivityMonitor.DependentToken)"/> (otherwise there will 
            /// be no way to bind the two activities). 
            /// </para>
            /// </summary>
            /// <param name="dependentTopic">Topic for the dependent activity. Use null to not change the target monitor's topic.</param>
            /// <param name="delayedLaunch">True to use <see cref="Launch(ActivityMonitor.DependentToken)"/> later to indicate the actual launch of the dependent activity.</param>
            /// <returns>A dependent token.</returns>
            public ActivityMonitor.DependentToken CreateTokenWithTopic( string? dependentTopic, bool delayedLaunch = false )
            {
                var t = ActivityMonitor.DependentToken.CreateWithDependentTopic( _monitor, delayedLaunch, dependentTopic, out string msg );
                if( delayedLaunch ) t.DelayedLaunchMessage = msg;
                else
                {
                    var d = new ActivityMonitorLogData( LogLevel.Info, _monitor.AutoTags | ActivityMonitor.Tags.CreateDependentActivity, msg, null, _fileName, _lineNumber );
                    d.SetExplicitLogTime( t.CreationDate );
                    _monitor.UnfilteredLog( ref d );
                }
                return t;
            }

            /// <summary>
            /// Signals the launch of one or more dependent activities by emitting a log line that describes the token.
            /// The token must have been created by <see cref="CreateToken"/> or <see cref="CreateTokenWithTopic"/> with a true delayedLaunch parameter
            /// otherwise an <see cref="InvalidOperationException"/> is thrown.
            /// </summary>
            /// <param name="token">Dependent token.</param>
            public void Launch( ActivityMonitor.DependentToken token )
            {
                if( token.DelayedLaunchMessage == null ) throw new InvalidOperationException( Impl.ActivityMonitorResources.ActivityMonitorDependentTokenMustBeDelayedLaunch );
                _monitor.UnfilteredLog( LogLevel.Info, ActivityMonitor.Tags.CreateDependentActivity, token.DelayedLaunchMessage, null, _fileName, _lineNumber );
            }

            /// <summary>
            /// Launches one or more dependent activities (thanks to a delegate) that will use the current monitor's topic.
            /// This creates a new <see cref="ActivityMonitor.DependentToken"/> and opens a group that wraps the execution of the <paramref name="dependentLauncher"/>.
            /// </summary>
            /// <param name="dependentLauncher">Must create and launch dependent activities that should use the created token.</param>
            /// <returns>A dependent token.</returns>
            public void Launch( Action<ActivityMonitor.DependentToken> dependentLauncher )
            {
                Throw.CheckNotNullArgument( dependentLauncher );
                var t = ActivityMonitor.DependentToken.CreateWithMonitorTopic( _monitor, true, out string msg );
                var d = new ActivityMonitorLogData( LogLevel.Info, _monitor.AutoTags | ActivityMonitor.Tags.CreateDependentActivity, msg, null, _fileName, _lineNumber );
                d.SetExplicitLogTime( t.CreationDate );
                using( _monitor.UnfilteredOpenGroup( ref d ) )
                {
                    dependentLauncher( t );
                    _monitor.CloseGroup( "Success." );
                }
            }

            /// <summary>
            /// Launches one or more dependent activities (thanks to a delegate) that will be bound to a specified topic (or that will not change 
            /// the dependent monitor's topic if null is specified).
            /// This creates a new <see cref="ActivityMonitor.DependentToken"/> and opens a group that wraps the execution of the <paramref name="dependentLauncher"/>.
            /// </summary>
            /// <param name="dependentLauncher">Must create and launch dependent activities that should use the created token.</param>
            /// <param name="dependentTopic">Topic for the dependent activity: the receiver's monitor will set this topic .</param>
            public void LaunchWithTopic( Action<ActivityMonitor.DependentToken> dependentLauncher, string dependentTopic )
            {
                Throw.CheckNotNullArgument( dependentLauncher );
                var t = ActivityMonitor.DependentToken.CreateWithDependentTopic( _monitor, true, dependentTopic, out string msg );
                var d = new ActivityMonitorLogData( LogLevel.Info, _monitor.AutoTags | ActivityMonitor.Tags.CreateDependentActivity, msg, null, _fileName, _lineNumber );
                d.SetExplicitLogTime( t.CreationDate );
                using( _monitor.UnfilteredOpenGroup( ref d ) )
                {
                    dependentLauncher( t );
                    _monitor.CloseGroup( "Success." );
                }
            }
        }
        
        /// <summary>
        /// Enables dependent activities token creation and activities launch.
        /// Use <see cref="StartDependentActivity">IActivityMonitor.StartDependentActivity</see> to declare the start of a 
        /// dependent activity on the target monitor.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler but can be explicitly set).</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler but can be explicitly set).</param>
        /// <returns>Sender object.</returns>
        static public DependentSender DependentActivity( this IActivityMonitor @this, [CallerFilePath]string? fileName = null, [CallerLineNumber]int lineNumber = 0 )
        {
            return new DependentSender( @this, fileName, lineNumber );
        }
        
        /// <summary>
        /// Starts a dependent activity. This temporarily sets the <see cref="ActivityMonitor.DependentToken.Topic"/> if it is not null and opens a group
        /// tagged with <see cref="ActivityMonitor.Tags.StartDependentActivity"/> and a message that can be parsed back thanks to <see cref="ActivityMonitor.DependentToken.TryParseStartMessage"/>.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="token">Token that describes the origin of the activity.</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler but can be explicitly set).</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler but can be explicitly set).</param>
        /// <returns>A disposable object. It must be disposed at the end of the activity.</returns>
        static public IDisposable StartDependentActivity( this IActivityMonitor @this, ActivityMonitor.DependentToken token, [CallerFilePath]string? fileName = null, [CallerLineNumber]int lineNumber = 0 )
        {
            Throw.CheckNotNullArgument( token );
            return ActivityMonitor.DependentToken.Start( token, @this, fileName, lineNumber );
        }

    }
}
