using System;

namespace CK.Core
{
    /// <summary>
    /// Describes the construction options of a <see cref="ActivityMonitor"/>.
    /// </summary>
    [Flags]
    public enum ActivityMonitorOptions
    {
        /// <summary>
        /// <see cref="ActivityMonitor.AutoConfiguration"/> is applied and
        /// the monitor has no <see cref="IActivityMonitor.ParallelLogger"/>.
        /// </summary>
        Default = 0,

        /// <summary>
        /// <see cref="ActivityMonitor.AutoConfiguration"/> is applied and
        /// the <see cref="IActivityMonitor.ParallelLogger"/> available.
        /// </summary>
        WithParallel = 1,

        /// <summary>
        /// Ignores <see cref="ActivityMonitor.AutoConfiguration"/>.
        /// </summary>
        SkipAutoConfiguration = 2,

        /// <summary>
        /// Captures initial logs and replays them into each new registered <see cref="IActivityMonitorClient"/>.
        /// The maximal number of replayed logs defaults to 1000: it can be changed thanks to <see cref="IActivityMonitorOutput.MaxInitialReplayCount"/>
        /// that can also be used to stop the log replay.
        /// <para>
        /// Replaying logs is pot-in: <see cref="IActivityMonitorOutput.RegisterClient(IActivityMonitorClient, out bool, bool)"/> must specify
        /// a true <c>replayInitialLogs</c> parameters.
        /// </para>
        /// </summary>
        WithInitialReplay = 4
    }

}
