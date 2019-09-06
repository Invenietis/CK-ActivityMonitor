using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.Core
{
    public static partial class ActivityMonitorSimpleSenderExtension
    {
        /// <summary>
        /// Opens a given <see cref="LogLevel"/> group with an exception. 
        /// Regardless of whether it will be emitted or not (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="fileName"/> 
        /// and <paramref name="lineNumber"/>), it must always be closed.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>A disposable object that can be used to set a function that provides a conclusion text and/or close the group.</returns>
        public static IDisposableGroup OpenGroup( this IActivityMonitor @this, LogLevel level, Exception ex, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string fileName = null )
        {
            return @this.UnfilteredOpenGroup( @this.ShouldLogGroup( level, fileName, lineNumber )
                                                ? new ActivityMonitorGroupData( level | LogLevel.IsFiltered, ActivityMonitor.Tags.Empty, null, @this.NextLogTime(), ex, null, fileName, lineNumber )
                                                : null );
        }

        /// <summary>
        /// Opens a given <see cref="LogLevel"/> group with a text message. 
        /// Regardless of whether it will be emitted or not (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="fileName"/> 
        /// and <paramref name="lineNumber"/>), it must always be closed.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="text">The text to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>A disposable object that can be used to set a function that provides a conclusion text and/or close the group.</returns>
        public static IDisposableGroup OpenGroup( this IActivityMonitor @this, LogLevel level, string text, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string fileName = null )
        {
            return @this.UnfilteredOpenGroup( @this.ShouldLogGroup( level, fileName, lineNumber )
                                                ? new ActivityMonitorGroupData( level | LogLevel.IsFiltered, ActivityMonitor.Tags.Empty, text, @this.NextLogTime(), null, null, fileName, lineNumber )
                                                : null );
        }

        /// <summary>
        /// Opens a given <see cref="LogLevel"/> group with a text message associated to an exception. 
        /// Regardless of whether it will be emitted or not (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="fileName"/> 
        /// and <paramref name="lineNumber"/>), it must always be closed.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="text">The text to log.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>A disposable object that can be used to set a function that provides a conclusion text and/or close the group.</returns>
        public static IDisposableGroup OpenGroup( this IActivityMonitor @this, LogLevel level, string text, Exception ex, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string fileName = null )
        {
            return @this.UnfilteredOpenGroup( @this.ShouldLogGroup( level, fileName, lineNumber )
                                                ? new ActivityMonitorGroupData( level | LogLevel.IsFiltered, ActivityMonitor.Tags.Empty, text, @this.NextLogTime(), ex, null, fileName, lineNumber )
                                                : null );
        }

        /// <summary>
        /// Opens a given <see cref="LogLevel"/> group with a text message built only if the group must be emitted. 
        /// Regardless of whether it will be emitted or not (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="fileName"/> 
        /// and <paramref name="lineNumber"/>), it must always be closed.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="text">A function (that will be called only if required) that returns the text to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>A disposable object that can be used to set a function that provides a conclusion text and/or close the group.</returns>
        public static IDisposableGroup OpenGroup( this IActivityMonitor @this, LogLevel level, Func<string> text, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string fileName = null )
        {
            return @this.UnfilteredOpenGroup( @this.ShouldLogGroup( level, fileName, lineNumber )
                                                ? new ActivityMonitorGroupData( level | LogLevel.IsFiltered, ActivityMonitor.Tags.Empty, text?.Invoke(), @this.NextLogTime(), null, null, fileName, lineNumber )
                                                : null );
        }

        /// <summary>
        /// Opens a given <see cref="LogLevel"/> group with a text message built only if the group must be emitted and an exception. 
        /// Regardless of whether it will be emitted or not (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="fileName"/> 
        /// and <paramref name="lineNumber"/>), it must always be closed.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="text">A function (that will be called only if required) that returns the text to log.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>A disposable object that can be used to set a function that provides a conclusion text and/or close the group.</returns>
        public static IDisposableGroup OpenGroup( this IActivityMonitor @this, LogLevel level, Func<string> text, Exception ex, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string fileName = null )
        {
            return @this.UnfilteredOpenGroup( @this.ShouldLogGroup( level, fileName, lineNumber )
                                                ? new ActivityMonitorGroupData( level | LogLevel.IsFiltered, ActivityMonitor.Tags.Empty, text?.Invoke(), @this.NextLogTime(), ex, null, fileName, lineNumber )
                                                : null );
        }

        #region Debug with tags.

        /// <summary>
        /// Opens a given <see cref="LogLevel"/> group with an exception and tags. 
        /// Regardless of whether it will be emitted or not (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="fileName"/> 
        /// and <paramref name="lineNumber"/>), it must always be closed.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="tags">The tags for this log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>A disposable object that can be used to set a function that provides a conclusion text and/or close the group.</returns>
        public static IDisposableGroup OpenGroup( this IActivityMonitor @this, LogLevel level, Exception ex, CKTrait tags, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string fileName = null )
        {
            return @this.UnfilteredOpenGroup( @this.ShouldLogGroup( level, fileName, lineNumber )
                                                ? new ActivityMonitorGroupData( level | LogLevel.IsFiltered, tags, null, @this.NextLogTime(), ex, null, fileName, lineNumber )
                                                : null );
        }

        /// <summary>
        /// Opens a given <see cref="LogLevel"/> group with a text message and tags. 
        /// Regardless of whether it will be emitted or not (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="fileName"/> 
        /// and <paramref name="lineNumber"/>), it must always be closed.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="text">The text to log.</param>
        /// <param name="tags">The tags for this log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>A disposable object that can be used to set a function that provides a conclusion text and/or close the group.</returns>
        public static IDisposableGroup OpenGroup( this IActivityMonitor @this, LogLevel level, string text, CKTrait tags, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string fileName = null )
        {
            return @this.UnfilteredOpenGroup( @this.ShouldLogGroup( level, fileName, lineNumber )
                                                ? new ActivityMonitorGroupData( level | LogLevel.IsFiltered, tags, text, @this.NextLogTime(), null, null, fileName, lineNumber )
                                                : null );
        }

        /// <summary>
        /// Opens a given <see cref="LogLevel"/> group with a text message associated to an exception and tags. 
        /// Regardless of whether it will be emitted or not (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="fileName"/> 
        /// and <paramref name="lineNumber"/>), it must always be closed.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="text">The text to log.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="tags">The tags for this log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>A disposable object that can be used to set a function that provides a conclusion text and/or close the group.</returns>
        public static IDisposableGroup OpenGroup( this IActivityMonitor @this, LogLevel level, string text, Exception ex, CKTrait tags, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string fileName = null )
        {
            return @this.UnfilteredOpenGroup( @this.ShouldLogGroup( level, fileName, lineNumber )
                                                ? new ActivityMonitorGroupData( level | LogLevel.IsFiltered, tags, text, @this.NextLogTime(), ex, null, fileName, lineNumber )
                                                : null );
        }

        /// <summary>
        /// Opens a given <see cref="LogLevel"/> group with a text message built only if the group must be emitted and tags. 
        /// Regardless of whether it will be emitted or not (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="fileName"/> 
        /// and <paramref name="lineNumber"/>), it must always be closed.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="text">A function (that will be called only if required) that returns the text to log.</param>
        /// <param name="tags">The tags for this group.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>A disposable object that can be used to set a function that provides a conclusion text and/or close the group.</returns>
        public static IDisposableGroup OpenGroup( this IActivityMonitor @this, LogLevel level, Func<string> text, CKTrait tags, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string fileName = null )
        {
            return @this.UnfilteredOpenGroup( @this.ShouldLogGroup( level, fileName, lineNumber )
                                                ? new ActivityMonitorGroupData( level | LogLevel.IsFiltered, tags, text?.Invoke(), @this.NextLogTime(), null, null, fileName, lineNumber )
                                                : null );
        }

        /// <summary>
        /// Opens a given <see cref="LogLevel"/> group with a text message built only if the group must be emitted, an exception and tags. 
        /// Regardless of whether it will be emitted or not (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="fileName"/> 
        /// and <paramref name="lineNumber"/>), it must always be closed.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="text">A function (that will be called only if required) that returns the text to log.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="tags">The tags for this group.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>A disposable object that can be used to set a function that provides a conclusion text and/or close the group.</returns>
        public static IDisposableGroup OpenGroup( this IActivityMonitor @this, LogLevel level, Func<string> text, Exception ex, CKTrait tags, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string fileName = null )
        {
            return @this.UnfilteredOpenGroup( @this.ShouldLogGroup( level, fileName, lineNumber )
                                                ? new ActivityMonitorGroupData( level | LogLevel.IsFiltered, tags, text?.Invoke(), @this.NextLogTime(), ex, null, fileName, lineNumber )
                                                : null );
        }

        #endregion
    }
}
