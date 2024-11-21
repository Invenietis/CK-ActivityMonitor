namespace CK.Core;

/// <summary>
/// Specialized <see cref="IActivityMonitorFilteredClient"/> that tags clients that are collecting
/// log entries for a user that is interacting with the system (typically the one who initiated the
/// activity). See <see cref="ActivityMonitorExtension.SetInteractiveUserFilter(IActivityMonitor, LogClamper)"/>
/// and <see cref="ActivityMonitorExtension.TemporarilySetInteractiveUserFilter(IActivityMonitor, LogClamper)"/>.
/// <para>
/// The two standard console clients <see cref="ActivityMonitorConsoleClient"/> and <see cref="ColoredActivityMonitorConsoleClient"/>
/// implement this marker interface.
/// </para>
/// </summary>
public interface IActivityMonitorInteractiveUserClient : IActivityMonitorFilteredClient
{
}
