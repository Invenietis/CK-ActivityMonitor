using System.Runtime.CompilerServices;

namespace CK.Core
{
    /// <summary>
    /// Parallel logger can be provided by <see cref="IActivityMonitor.ParallelLogger"/>.
    /// They cannot manage structured logging (no groups), just like <see cref="IActivityLogger"/>, only lines
    /// can be emitted but this adds the capability to create dependent tokens.
    /// </summary>
    public interface IParallelLogger : IActivityLogger
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
        /// <param name="message">Optional message for the token creation log.</param>
        /// <param name="dependentTopic">Optional dependent topic.</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler but can be explicitly set).</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler but can be explicitly set).</param>
        ActivityMonitor.DependentToken CreateDependentToken( string? message = null,
                                                             string? dependentTopic = null,
                                                             [CallerFilePath] string? fileName = null,
                                                             [CallerLineNumber] int lineNumber = 0 );
    }
}
