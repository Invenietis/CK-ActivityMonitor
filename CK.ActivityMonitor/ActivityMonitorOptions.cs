﻿using System;

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
        /// Ignores <see cref="ActivityMonitor.AutoConfiguration"/> and
        /// provides a <see cref="IActivityMonitor.ParallelLogger"/>.
        /// </summary>
        WithParallelAndSkipAutoConfiguration = 3,
    }

}
