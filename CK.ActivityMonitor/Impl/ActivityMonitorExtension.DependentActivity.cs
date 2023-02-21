using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    /// <summary>
    /// Provides extension methods for <see cref="IActivityMonitor"/> and other types from the Activity monitor framework.
    /// </summary>
    public static partial class ActivityMonitorExtension
    {
        /// <summary>
        /// Creates a token for a dependent activity that will set a specified topic (or that will not change the dependent monitor's topic
        /// if null is specified).
        /// <para>
        /// The extension method <see cref="StartDependentActivity(IActivityMonitor, ActivityMonitor.DependentToken, bool, LogLevel, bool, string?, int)">StartDependentActivity( token )</see>
        /// must be used on the target monitor to open and close the activity. If not null, the provided topic will be temporarily set on the
        /// target monitor otherwise the target topic will not be changed.
        /// </para>
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="message">Optional message for the token creation log.</param>
        /// <param name="dependentTopic">Optional dependent topic (can be this monitor's <see cref="IActivityMonitor.Topic"/>).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler but can be explicitly set).</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler but can be explicitly set).</param>
        static public ActivityMonitor.DependentToken CreateDependentToken( this IActivityMonitor @this,
                                                                           string? message = null,
                                                                           string? dependentTopic = null,
                                                                           [CallerFilePath] string? fileName = null,
                                                                           [CallerLineNumber] int lineNumber = 0 )
        {
            if( string.IsNullOrWhiteSpace( message ) ) message = null;
            if( string.IsNullOrWhiteSpace( dependentTopic ) ) dependentTopic = null;
            var t = new ActivityMonitor.DependentToken( @this.UniqueId, @this.GetAndUpdateNextLogTime(), message, dependentTopic );
            if( message != null )
            {
                if( dependentTopic != null ) message += $" (With topic '{dependentTopic}'.)";
            }
            else if( dependentTopic != null )
            {
                message = $"(With topic '{dependentTopic}'.)";
            }
            Debug.Assert( message == null || t.ToString().EndsWith( message ), "Checking that inline magic strings are the same." );
            var d = new ActivityMonitorLogData( LogLevel.Info, ActivityMonitor.Tags.CreateDependentToken, message, null, fileName, lineNumber );
            d.SetExplicitLogTime( t.CreationDate );
            @this.UnfilteredLog( ref d );
            return t;
        }

        /// <summary>
        /// Starts a dependent activity. This temporarily sets the <see cref="ActivityMonitor.DependentToken.Topic"/> if it is not null and opens a group
        /// tagged with <see cref="ActivityMonitor.Tags.StartDependentActivity"/> and a message that can be parsed back thanks
        /// to <see cref="ActivityMonitor.DependentToken.TryParseStartMessage"/>.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="token">Token that describes the origin of the activity.</param>
        /// <param name="temporarilySetTopic">False to ignore the <see cref="ActivityMonitor.DependentToken.Topic"/> even if it is not null.</param>
        /// <param name="groupLevel">
        /// Group level. Use <see cref="LogLevel.None"/> to not open a group.
        /// <para>
        /// Note that this group is filtered by <see cref="ActivityMonitor.Tags.Filters"/> by default.
        /// </para>
        /// </param>
        /// <param name="alwaysOpenGroup">True to not apply <see cref="ActivityMonitor.Tags.Filters"/> to the group.</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler but can be explicitly set).</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler but can be explicitly set).</param>
        /// <returns>A disposable object. It must be disposed at the end of the activity.</returns>
        static public IDisposable StartDependentActivity( this IActivityMonitor @this,
                                                          ActivityMonitor.DependentToken token,
                                                          bool temporarilySetTopic = true,
                                                          LogLevel groupLevel = LogLevel.Info,
                                                          bool alwaysOpenGroup = false,
                                                          [CallerFilePath] string? fileName = null,
                                                          [CallerLineNumber] int lineNumber = 0 )
        {
            Throw.CheckNotNullArgument( token );
            string msg = token.ToString( "Starting: " );
            string? currentTopic = null;
            if( temporarilySetTopic && token.Topic != null )
            {
                currentTopic = @this.Topic;
                @this.SetTopic( token.Topic, fileName, lineNumber );
            }
            if( groupLevel != LogLevel.None )
            {
                CKTrait finalTags;
                bool doOpen;
                if( doOpen = alwaysOpenGroup ) finalTags = @this.AutoTags + ActivityMonitor.Tags.StartDependentActivity;
                else doOpen = @this.ShouldLogGroup( groupLevel, ActivityMonitor.Tags.StartDependentActivity, out finalTags );
                if( doOpen )
                {
                    var d = new ActivityMonitorLogData( groupLevel | LogLevel.IsFiltered, finalTags, msg, null, fileName, lineNumber );
                    var g = @this.UnfilteredOpenGroup( ref d );
                    if( currentTopic != null )
                    {
                        return Util.CreateDisposableAction( () => { g.Dispose(); @this.SetTopic( currentTopic, fileName, lineNumber ); } );
                    }
                    return g;
                }
            }
            return currentTopic != null
                    ? Util.CreateDisposableAction( () => @this.SetTopic( currentTopic, fileName, lineNumber ) )
                    : Util.EmptyDisposable;
        }

    }
}
