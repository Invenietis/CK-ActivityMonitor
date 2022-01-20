using CK.Core.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CK.Core
{
    public partial class ActivityMonitor
    {
        /// <summary>
        /// Exposes static methods that immediately relay log data to <see cref="OnExternalLog"/>
        /// event.
        /// <para>
        /// Nothing is done with these logs at this level: this is to be used by client code of this library, typically CK.Monitoring.
        /// </para>
        /// <para>
        /// This is to be used rarely: only if there's really no way to bind the calling context to a real <see cref="IActivityMonitor"/>.
        /// Handlers of the OnExternalLog event should use the <see cref="ExternalLogMonitorUniqueId"/> as the monitor identifier.
        /// </para>
        /// </summary>
        public class ExternalLog
        {
            static Handler[] _handlers = Array.Empty<Handler>();

            /// <summary>
            /// The handler signature of external logs.
            /// <para>
            /// The "by ref" argument here is to avoid any copy of the data. The data should not be altered by calling one of
            /// the 2 mutating methods <see cref="ActivityMonitorLogData.SetExplicitLogTime(DateTimeStamp)"/> or <see cref="ActivityMonitorLogData.SetExplicitTags(CKTrait)"/>
            /// unless you absolutely know what you are doing.
            /// </para>
            /// </summary>
            /// <param name="data"></param>
            public delegate void Handler( ref ActivityMonitorLogData data );

            /// <summary>
            /// Raised when Send is called.
            /// <para>
            /// Such events should be handled very quickly (typically by queuing a projection of the
            /// data or the data itself in a concurrent queue or a channel).
            /// </para>
            /// <para>
            /// This is a static event: the callbacks registered here will be referenced until removed: care should be
            /// taken to unregister callbacks otherwise referenced objects will never be garbage collected.
            /// </para>
            /// </summary>
            static public event Handler OnExternalLog
            {
                add => Util.InterlockedAdd( ref _handlers, value );
                remove => Util.InterlockedRemove( ref _handlers, value );
            }

            /// <summary>
            /// Raises the <see cref="OnExternalLog"/> event with an explicit level and no tags. 
            /// <para>
            /// This can be used to log in context-less situations (when no <see cref="IActivityMonitor"/> exists) but
            /// this should be avoided as much as possible.
            /// </para>
            /// </summary>
            /// <param name="level">The log level that may be flagged with <see cref="LogLevel.IsFiltered"/> or not.</param>
            /// <param name="text">The text of the log.</param>
            /// <param name="ex">Optional associated exception.</param>
            /// <param name="fileName">The source code file name from which the group is opened.</param>
            /// <param name="lineNumber">The line number in the source from which the group is opened.</param>
            static public void UnfilteredLog( LogLevel level,
                                    string text,
                                    Exception? ex,
                                    [CallerFilePath] string? fileName = null,
                                    [CallerLineNumber] int lineNumber = 0 )
            {
                var d = new ActivityMonitorLogData( level, Tags.Empty, text, ex, fileName, lineNumber );
                SendData( ref d );
            }

            /// <summary>
            /// Raises the <see cref="OnExternalLog"/> event with an explicit level and optional tags. 
            /// <para>
            /// This can be used to log in context-less situations (when no <see cref="IActivityMonitor"/> exists) but
            /// this should be avoided as much as possible.
            /// </para>
            /// </summary>
            /// <param name="level">The log level that may be flagged with <see cref="LogLevel.IsFiltered"/> or not.</param>
            /// <param name="tags">Optional tags.</param>
            /// <param name="text">The text of the log.</param>
            /// <param name="ex">Optional associated exception.</param>
            /// <param name="fileName">The source code file name from which the group is opened.</param>
            /// <param name="lineNumber">The line number in the source from which the group is opened.</param>
            static public void UnfilteredLog( LogLevel level,
                                    CKTrait? tags,
                                    string text,
                                    Exception? ex,
                                    [CallerFilePath] string? fileName = null,
                                    [CallerLineNumber] int lineNumber = 0 )
            {
                var d = new ActivityMonitorLogData( level, tags ?? Tags.Empty, text, ex, fileName, lineNumber );
                SendData( ref d );
            }

            /// <summary>
            /// Raises the <see cref="OnExternalLog"/> event with the logged data. 
            /// <para>
            /// This can be used to log in context-less situations (when no <see cref="IActivityMonitor"/> exists) but
            /// this should be avoided as much as possible.
            /// </para>
            /// <para>
            /// The <see cref="ActivityMonitorLogData.Level"/> can be flagged with <see cref="LogLevel.IsFiltered"/> bit.
            /// If not, this log is considered "unfiltered" and should not be filtered anymore.
            /// </para>
            /// </summary>
            /// <param name="data">Data that describes the log. </param>
            public static void SendData( ref ActivityMonitorLogData data )
            {
                foreach( var h in _handlers ) h.Invoke( ref data );               
            }

        }
    }

}
