using Example17_SignalR_Shared.Core;

namespace Example17_SignalR_Shared.Dtos;

public class MessageDto
{
    public EntityType Type { get; set; }
    public required string Text { get; set; }
}