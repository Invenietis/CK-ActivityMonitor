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
        /// The extension method <see cref="StartDependentActivity(IActivityMonitor, ActivityMonitor.DependentToken, bool, string?, int)">StartDependentActivity( token )</see>
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
            bool isMonitorTopic = dependentTopic == @this.Topic;
            var t = new ActivityMonitor.DependentToken( @this.UniqueId, @this.NextLogTime(), message, dependentTopic, isMonitorTopic );
            message += isMonitorTopic
                        ? $" (With monitor's topic '{dependentTopic}'.)"
                        : dependentTopic != null
                            ? $" (With topic '{dependentTopic}'.)"
                            : " (Without topic.)";
            Debug.Assert( t.ToString().EndsWith( message ), "Checking that inline magic strings are the same." );
            var d = new ActivityMonitorLogData( LogLevel.Info, ActivityMonitor.Tags.CreateDependentToken, message, null, fileName, lineNumber );
            d.SetExplicitLogTime( t.CreationDate );
            @this.UnfilteredLog( ref d );
            return t;
        }

        /// <summary>
        /// Starts a dependent activity. This temporarily sets the <see cref="ActivityMonitor.DependentToken.Topic"/> if it is not null and opens a group
        /// tagged with <see cref="ActivityMonitor.Tags.StartDependentActivity"/> and a message that can be parsed back thanks to <see cref="ActivityMonitor.DependentToken.TryParseStartMessage"/>.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="token">Token that describes the origin of the activity.</param>
        /// <param name="temporarilySetTopic">False to ignore the <see cref="ActivityMonitor.DependentToken.Topic"/> even if it is not null.</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler but can be explicitly set).</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler but can be explicitly set).</param>
        /// <returns>A disposable object. It must be disposed at the end of the activity.</returns>
        static public IDisposable StartDependentActivity( this IActivityMonitor @this,
                                                          ActivityMonitor.DependentToken token,
                                                          bool temporarilySetTopic = true,
                                                          [CallerFilePath] string? fileName = null,
                                                          [CallerLineNumber] int lineNumber = 0 )
        {
            Throw.CheckNotNullArgument( token );
            string msg = token.ToString( "Starting: " );
            if( temporarilySetTopic && token.Topic != null )
            {
                string currentTopic = @this.Topic;
                @this.SetTopic( token.Topic, fileName, lineNumber );
                var g = @this.UnfilteredOpenGroup( LogLevel.Info, ActivityMonitor.Tags.StartDependentActivity, msg, null, fileName, lineNumber );
                return Util.CreateDisposableAction( () => { g.Dispose(); @this.SetTopic( currentTopic, fileName, lineNumber ); } );
            }
            return @this.UnfilteredOpenGroup( LogLevel.Info, ActivityMonitor.Tags.StartDependentActivity, msg, null, fileName, lineNumber );
        }

    }
}
