using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CK.Core.Impl
{
    /// <summary>
    /// String values for errors and messages.
    /// </summary>
    public class ActivityMonitorResources
    {
#pragma warning disable 1591
        public static readonly string ActivityMonitorBoundClientMultipleRegister = "A '{0}' can be registered in only one IActivityMonitor.Output at the same time. Unregister it before Registering it in another monitor.";
        public static readonly string ActivityMonitorBoundClientIsLocked = "'{0}' is bound and locked to its IActivityMonitor. Unlock it before unregistering it.";
        public static readonly string ActivityMonitorConcurrentThreadAccess = "Concurrent accesses to the same ActivityMonitor '{0}' has been detected. Current Thread n°{1} is trying to enter but Thread n°{2} is already in.";
        public static readonly string ActivityMonitorDependentTokenMustBeDelayedLaunch = "Dependent token must have been created with CreateToken( true ) to enable Lauch( token ) to be called later (or CreateTokenWithTopic).";
        public static readonly string ActivityMonitorErrorWhileGetConclusionText = "Unexpected error while getting conclusion text: '{0}'.";
        public static readonly string ActivityMonitorInvalidLogLevel = "The level must be a valid level (Trace, Info, Warn, Error or Fatal).";
        public static readonly string ActivityMonitorReentrancyCallOnly = "Only reentrant calls to this method are supported.";
        public static readonly string ActivityMonitorReentrancyError = "A reentrant call in ActivityMonitor '{0}' has been detected. A monitor usage must not trigger another operation on the same monitor.";
        public static readonly string ActivityMonitorReentrancyReleaseError = "Internal error on Monitor '{0}': Error during release reentrancy operation. Current Thread n°{1} is trying to exit it but Thread {2} entered it.";
        public static readonly string ActivityMonitorTagMustBeRegistered = "The Tag (CKTrait) must be registered in ActivityMonitor.Tags.";
        public static readonly string ClosedByBridgeRemoved = "Prematurely closed by Bridge removed.";
        public static readonly string InvalidRootLogPath = "Invalid Root log path. The path must be rooted (it must not be a relative path)";
        public static readonly string LogFileRootLogPathSetOnlyOnce = "Root log path must be set once and only once.";
        public static readonly string RootLogPathMustBeSet = @"A Root log path must be set: LogFile.RootLogPath = ""<absolute path>""; It is currently null.";
        public static readonly string ErrorWhileReplayingInternalLogs = @"Error while replaying internal monitor logs.";
        public static readonly string ReplayRestoreTopic = @"Restoring changed Topic.";
    }
}
