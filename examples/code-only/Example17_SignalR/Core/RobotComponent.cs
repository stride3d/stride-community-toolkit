using Example17_SignalR_Shared.Core;
using Stride.Engine;

namespace Example17_SignalR.Core;

/// <summary>
/// Marker component describing the kind of entity and its lifecycle state.
/// </summary>
public class RobotComponent : EntityComponent
{
    /// <summary>
    /// Logical type of the associated entity.
    /// </summary>
    public EntityType Type { get; set; }

    /// <summary>
    /// True once this entity has been scheduled for deletion.
    /// </summary>
    public bool IsDeleted { get; set; }
}