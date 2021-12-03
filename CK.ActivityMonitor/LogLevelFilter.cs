namespace CK.Core
{
    /// <summary>
    /// Defines filters for <see cref="LogLevel"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="LogFilter"/> capture two levels: one for lines and one for groups.
    /// </remarks>
    public enum LogLevelFilter : short
    {
        /// <summary>
        /// No filter: can have the same effect as Debug but SHOULD indicate
        /// an unknown or undefined filter that must be ignored, when combined with other level filters to 
        /// compute a final (minimal) filter level.
        /// </summary>
        None = 0,

        /// <summary>
        /// Everything is logged (<see cref="LogLevel.Debug"/>).
        /// </summary>
        Debug = 1,

        /// <summary>
        /// <see cref="LogLevel.Trace"/> and above is logged..
        /// </summary>
        Trace = 2,

        /// <summary>
        /// Only <see cref="LogLevel.Info"/> and above is logged.
        /// </summary>
        Info = 4,

        /// <summary>
        /// Only <see cref="LogLevel.Warn"/> and above is logged.
        /// </summary>
        Warn = 8,

        /// <summary>
        /// Only <see cref="LogLevel.Error"/> and above is logged.
        /// </summary>
        Error = 16,

        /// <summary>
        /// Only <see cref="LogLevel.Fatal"/> is logged.
        /// </summary>
        Fatal = 32,

        /// <summary>
        /// Do not log anything.
        /// </summary>
        Off = 64,

        /// <summary>
        /// Invalid filter can be use to designate an unknown filter. 
        /// Since its value is -1, in the worst case it will not filter anything.
        /// </summary>
        Invalid = -1
    }
}
