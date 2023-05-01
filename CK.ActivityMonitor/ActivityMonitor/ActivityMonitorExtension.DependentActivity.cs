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
        /// Starts a dependent activity. This temporarily sets the <see cref="ActivityMonitor.Token.Topic"/> if it is not null and opens a group
        /// tagged with <see cref="ActivityMonitor.Tags.StartActivity"/> and a message that can be parsed back thanks
        /// to <see cref="ActivityMonitor.Token.TryParseStartMessage"/>.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="token">Token that describes the origin of the activity.</param>
        /// <param name="temporarilySetTopic">False to ignore the <see cref="ActivityMonitor.Token.Topic"/> even if it is not null.</param>
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
                                                          ActivityMonitor.Token token,
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
                if( doOpen = alwaysOpenGroup ) finalTags = @this.AutoTags + ActivityMonitor.Tags.StartActivity;
                else doOpen = @this.ShouldLogGroup( groupLevel, ActivityMonitor.Tags.StartActivity, out finalTags );
                if( doOpen )
                {
                    var g = @this.UnfilteredOpenGroup( groupLevel | LogLevel.IsFiltered, finalTags, msg, null, fileName, lineNumber );
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
