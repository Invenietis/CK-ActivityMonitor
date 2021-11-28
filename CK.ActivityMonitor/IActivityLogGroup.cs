#region LGPL License
/*----------------------------------------------------------------------------
* This file (CK.Core\ActivityMonitor\IActivityLogGroup.cs) is part of CiviKey. 
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

namespace CK.Core
{
    /// <summary>
    /// Exposes all the relevant information for a currently opened group.
    /// Groups are linked together from the current one to the very first one 
    /// thanks to the <see cref="Parent"/> property.
    /// </summary>
    public interface IActivityLogGroup
    {
        /// <summary>
        /// Gets the log data.
        /// </summary>
        ref ActivityMonitorLogData Data { get; }

        /// <summary>
        /// Gets the log time of the group closing.
        /// It is <see cref="DateTimeStamp.MinValue"/> when the group is not closed yet.
        /// </summary>
        DateTimeStamp CloseLogTime { get; }

        /// <summary>
        /// Get the previous group in its origin monitor. Null if this group is a top level group.
        /// </summary>
        IActivityLogGroup? Parent { get; }

        /// <summary>
        /// Gets the depth of this group in its origin monitor. (1 for top level groups).
        /// </summary>
        int Depth { get; }

        /// <summary>
        /// Gets the <see cref="IActivityMonitor.MinimalFilter"/> that will be restored when group will be closed.
        /// Initialized with the current value of IActivityMonitor.Filter when the group has been opened.
        /// </summary>
        LogFilter SavedMonitorFilter { get; }

        /// <summary>
        /// Gets the <see cref="IActivityMonitor.AutoTags"/> that will be restored when group will be closed.
        /// Initialized with the current value of IActivityMonitor.Tags when the group has been opened.
        /// </summary>
        CKTrait SavedMonitorTags { get; }

        /// <summary>
        /// Gets whether this group is rejected.
        /// A rejected group has an empty data and can appear only in the <see cref="Parent"/> linked list.
        /// </summary>
        bool IsRejectedGroup { get; }

    }
}
