namespace Example17_SignalR_Blazor.Dtos;

public class MessageDto
{
    public EntityType Type { get; set; }
    public required string Text { get; set; }
}