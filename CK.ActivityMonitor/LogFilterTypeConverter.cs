#region LGPL License
/*----------------------------------------------------------------------------
* This file (CK.Core\ActivityMonitor\LogFilter.cs) is part of CiviKey. 
*  
* CiviKey is free software: you can redistribute it and/or modify 
* it under the terms of the GNU Lesser General Public License as published 
* by the Free Software Foundation, either version 3 of the License, or 
* (at your option) any later version. 
*  
* CiviKey is distributed in the hope that it will be useful, 
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
* GNU Lesser General Public License for more details. 
* You should have received a copy of the GNU Lesser General Public License 
* along with CiviKey.  If not, see <http://www.gnu.org/licenses/>. 
*  
* Copyright © 2007-2015, 
*     Invenietis <http://www.invenietis.com>,
*     In’Tech INFO <http://www.intechinfo.fr>,
* All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.ComponentModel;
using System.Globalization;

namespace CK.Core
{
    /// <summary>
    /// Minimal implementation of <see cref="LogFilter"/> conversion from and to <see cref="String"/>.
    /// This allows LoFilter to appear in their text form in configuration files.
    /// </summary>
    public class LogFilterTypeConverter : TypeConverter
    {
        /// <summary>
        /// Only allows source type to be string.
        /// </summary>
        /// <param name="context">Unused.</param>
        /// <param name="sourceType">Source type (must be <see cref="String"/>).</param>
        /// <returns>True on success, false otherwise.</returns>
        public override bool CanConvertFrom( ITypeDescriptorContext context, Type sourceType )
        {
            return sourceType == typeof( string );
        }

        /// <summary>
        /// Converts from a string.
        /// This uses <see cref="LogFilter.TryParse(string, out LogFilter)"/> since exceptions are usually
        /// swallowed by callers that fallback to the default value for the type (the rational behind being that
        /// conversion is not validation and that validator should be used if validation is required).
        /// </summary>
        /// <param name="context">Unused.</param>
        /// <param name="culture">Unused.</param>
        /// <param name="value">Must be as string (see <see cref="LogFilter.Parse(string)"/>).</param>
        /// <returns>The LogFilter (can be <see cref="LogFilter.Undefined"/> on error).</returns>
        public override object ConvertFrom( ITypeDescriptorContext context, CultureInfo culture, object value )
        {
            LogFilter.TryParse( (string)value, out var result );
            return result;
        }

        /// <summary>
        /// Only accepts conversion to string.
        /// </summary>
        /// <param name="context">Unused.</param>
        /// <param name="destinationType">Destination type (must be <see cref="String"/>).</param>
        /// <returns>True on success, false otherwise.</returns>
        public override bool CanConvertTo( ITypeDescriptorContext context, Type destinationType )
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
        public override object ConvertTo( ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType )
        {
            return ((LogFilter)value).ToString();
        }
    }

}
