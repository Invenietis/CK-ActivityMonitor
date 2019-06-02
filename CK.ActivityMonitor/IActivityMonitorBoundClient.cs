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

using CK.Core.Impl;

namespace CK.Core
{
    /// <summary>
    /// Specialized <see cref="IActivityMonitorClient"/> that is bound to one <see cref="IActivityMonitor"/>.
    /// Clients that can not be registered into multiple outputs (and receive logs from multiple monitors at
    /// the same time) should implement this interface in order to control their registration/un-registration.
    /// </summary>
    public interface IActivityMonitorBoundClient : IActivityMonitorClient
    {
        /// <summary>
        /// Gets the minimal log level that this Client expects. 
        /// Should default to <see cref="LogFilter.Undefined"/> if this client has no
        /// filtering requirements.
        /// </summary>
        LogFilter MinimalFilter { get; }

        /// <summary>
        /// Gets whether this client is dead: it should be removed from the source activity monitor's clients.
        /// It should obviously defaults to false (and once true should remain true).
        /// Implementations should call <see cref="IActivityMonitorImpl.SignalChange"/> on its current source
        /// to trigger the removal.
        /// </summary>
        bool IsDead { get; }

        /// <summary>
        /// Called by <see cref="IActivityMonitorOutput"/> when registering or unregistering
        /// this client.
        /// </summary>
        /// <param name="source">The monitor that will send log.</param>
        /// <param name="forceBuggyRemove">
        /// True if this client must be removed because one of its method thrown an exception.
        /// The <paramref name="source"/> is necessarily null and a client has no way to
        /// prevent the removal.
        /// </param>
        void SetMonitor( IActivityMonitorImpl source, bool forceBuggyRemove );
    }
}
