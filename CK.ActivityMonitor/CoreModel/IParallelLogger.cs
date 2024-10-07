using System.Runtime.CompilerServices;

namespace CK.Core;

/// <summary>
/// Parallel logger of a monitor: <see cref="IActivityMonitor.ParallelLogger"/>.
/// It cannot manage structured logging (no groups) but is thread safe: lines can be emitted
/// and dependent tokens can be created by any thread at any time.
/// </summary>
public interface IParallelLogger : IActivityLineEmitter, IActivityDependentTokenFactory
{
    /// <summary>
    /// Gets the identifier of the activity.
    /// </summary>
    string UniqueId { get; }
}
