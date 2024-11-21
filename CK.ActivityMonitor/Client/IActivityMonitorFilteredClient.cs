namespace CK.Core;

/// <summary>
/// Specialized <see cref="IActivityMonitorBoundClient"/> that exposes
/// its <see cref="IActivityMonitorBoundClient.MinimalFilter"/> as a <see cref="LogClamper"/> property:
/// when <see cref="LogClamper.Clamp"/> is true, logs for this client should be filtered out accordingly.
/// </summary>
/// <remarks>
/// Implementations should explicitly implement <see cref="IActivityMonitorBoundClient.MinimalFilter"/> to
/// return the <see cref="LogClamper.Filter"/>.
/// </remarks>
public interface IActivityMonitorFilteredClient : IActivityMonitorBoundClient
{
    /// <summary>
    /// Gets or sets the minimal log level that this Client expects. 
    /// Setting this to any level ensures that the bounded monitor will accept
    /// at least this level (see <see cref="IActivityMonitor.ActualFilter"/>).
    /// <para>
    /// When the <see cref="LogClamper.Clamp"/> is true, any log more verbose than
    /// the <see cref="LogClamper.Filter"/> should be ignored by this implementation.
    /// </para>
    /// <para>
    /// Defaults to <see cref="LogClamper.Undefined"/> if this client has no filtering requirements.
    /// </para>
    /// </summary>
    new LogClamper MinimalFilter { get; set; }

}
