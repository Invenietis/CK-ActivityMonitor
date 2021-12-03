namespace CK.Core
{
    /// <summary>
    /// Specialized <see cref="IActivityMonitorBoundClient"/> that exposes
    /// its <see cref="IActivityMonitorBoundClient.MinimalFilter"/> as a writable property.
    /// </summary>
    public interface IActivityMonitorFilteredClient : IActivityMonitorBoundClient
    {
        /// <summary>
        /// Gets or sets the minimal log level that this Client expects. 
        /// Setting this to any level ensures that the bounded monitor will accept
        /// at least this level (see <see cref="IActivityMonitor.ActualFilter"/>).
        /// Defaults to <see cref="LogFilter.Undefined"/> if this client has no filtering requirements.
        /// </summary>
        new LogFilter MinimalFilter { get; set; }

    }
}
