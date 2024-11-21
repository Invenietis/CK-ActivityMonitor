
namespace CK.Core;

/// <summary>
/// Exposes all the relevant information for a currently opened group.
/// Groups are linked together from the current one to the very first one 
/// thanks to the <see cref="Parent"/> property.
/// </summary>
public interface IActivityLogGroup
{
    /// <summary>
    /// Gets the log data with its <see cref="ActivityMonitorLogData.Depth"/> (that is based
    /// on non rejected group).
    /// (<see cref="ActivityMonitorLogData.IsFrozen"/> is true).
    /// </summary>
    ref ActivityMonitorLogData Data { get; }

    /// <summary>
    /// Gets the log time of the group closing.
    /// It is <see cref="DateTimeStamp.MinValue"/> when the group is not closed yet.
    /// </summary>
    DateTimeStamp CloseLogTime { get; }

    /// <summary>
    /// Get the previous group in its origin monitor that may be <see cref="IsRejectedGroup"/>.
    /// Null if this group is a top level group.
    /// </summary>
    IActivityLogGroup? Parent { get; }

    /// <summary>
    /// Gets whether this group is rejected.
    /// A rejected group has an empty data and can appear only in the <see cref="Parent"/> linked list.
    /// </summary>
    bool IsRejectedGroup { get; }

}
