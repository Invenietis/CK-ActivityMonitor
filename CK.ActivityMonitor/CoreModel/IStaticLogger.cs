namespace CK.Core;

/// <summary>
/// The <see cref="ActivityMonitor.StaticLogger"/> is only able to emit log lines
/// from any thread at any time.
/// </summary>
public interface IStaticLogger : IActivityLineEmitter
{
}
