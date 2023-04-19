using System;

namespace CK.Core
{
    /// <summary>
    /// Interface obtained once a Group has been opened.
    /// </summary>
    public interface IDisposableGroup : IDisposable
    {
        /// <summary>
        /// Gets whether the groups has been filtered. 
        /// It must be closed as usual but it's opening and closing will not be recorded
        /// (this is the equivalent of the boolean returned by line logging methods).
        /// </summary>
        bool IsRejectedGroup { get; }

        /// <summary>
        /// Sets a function that will be called on group closing to generate a conclusion.
        /// When <see cref="IsRejectedGroup"/> is true, this function is not called.
        /// </summary>
        /// <param name="getConclusionText">Function that generates a group conclusion.</param>
        /// <returns>A disposable object that can be used to close the group.</returns>
        IDisposable ConcludeWith( Func<string?> getConclusionText );

    }
}
