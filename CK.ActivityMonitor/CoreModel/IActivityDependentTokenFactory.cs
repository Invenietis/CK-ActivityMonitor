using System.Runtime.CompilerServices;

namespace CK.Core
{
    /// <summary>
    /// Applies to <see cref="IParallelLogger"/> and <see cref="IActivityMonitor"/>.
    /// </summary>
    public interface IActivityDependentTokenFactory
    {
        /// <summary>
        /// Creates a token for a dependent activity that will set a specified topic (or that will not change the dependent monitor's topic
        /// if null is specified).
        /// <para>
        /// The extension method <see cref="ActivityMonitorExtension.StartDependentActivity"/>
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
