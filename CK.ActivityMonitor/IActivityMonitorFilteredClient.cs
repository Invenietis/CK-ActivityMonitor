#region LGPL License
/*----------------------------------------------------------------------------
* This file (CK.Core\ActivityMonitor\IActivityMonitorBoundClient.cs) is part of CiviKey. 
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
