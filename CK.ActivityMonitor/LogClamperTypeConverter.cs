using System;
using System.ComponentModel;
using System.Globalization;

namespace CK.Core
{
    /// <summary>
    /// Minimal implementation of <see cref="LogClamper"/> conversion from and to <see cref="String"/>.
    /// This allows LogClamper to appear in their text form in configuration files.
    /// </summary>
    public class LogClamperTypeConverter : TypeConverter
    {
        /// <summary>
        /// Only allows source type to be string.
        /// </summary>
        /// <param name="context">Unused.</param>
        /// <param name="sourceType">Source type (must be <see cref="String"/>).</param>
        /// <returns>True on success, false otherwise.</returns>
        public override bool CanConvertFrom( ITypeDescriptorContext? context, Type? sourceType )
        {
            return sourceType == typeof( string );
        }

        /// <summary>
        /// Converts from a string.
        /// This uses <see cref="LogClamper.LogClamper(string, out LogFilter)"/> since exceptions are usually
        /// swallowed by callers that fallback to the default value for the type (the rational behind is that
        /// conversion is not validation and that validator should be used if validation is required).
        /// </summary>
        /// <param name="context">Unused.</param>
        /// <param name="culture">Unused.</param>
        /// <param name="value">Must be a string (see <see cref="LogFilter.Parse(string)"/>).</param>
        /// <returns>The LogFilter (can be <see cref="LogFilter.Undefined"/> on error).</returns>
        public override object ConvertFrom( ITypeDescriptorContext? context, CultureInfo? culture, object value )
        {
            LogClamper.TryParse( (string)value, out var result );
            return result;
        }

        /// <summary>
        /// Only accepts conversion to string.
        /// </summary>
        /// <param name="context">Unused.</param>
        /// <param name="destinationType">Destination type (must be <see cref="String"/>).</param>
        /// <returns>True on success, false otherwise.</returns>
        public override bool CanConvertTo( ITypeDescriptorContext? context, Type? destinationType )
        {
            return destinationType == typeof( string );
        }

        /// <summary>
        /// Converts to a string (simple relay to <see cref="LogFilter.ToString()"/>.
        /// </summary>
        /// <param name="context">Unused.</param>
        /// <param name="culture">Unused.</param>
        /// <param name="value">Must be a <see cref="LogFilter"/> instance.</param>
        /// <param name="destinationType">Destination type (must be <see cref="String"/>).</param>
        /// <returns>The <see cref="LogFilter.ToString"/> result.</returns>
        public override object ConvertTo( ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType )
        {
            return value == null ? LogClamper.Undefined : ((LogClamper)value).ToString();
        }
    }

}
