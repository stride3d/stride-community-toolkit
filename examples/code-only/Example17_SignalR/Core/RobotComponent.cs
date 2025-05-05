using Example17_SignalR_Shared.Core;
using Stride.Engine;

namespace Example17_SignalR.Core;

public class RobotComponent : EntityComponent
{
    public EntityType Type { get; set; }
    public bool IsDeleted { get; set; }
}