using Example17_SignalR_Shared.Core;

namespace Example17_SignalR_Shared.Dtos;

public class CountDto
{
    public EntityType Type { get; set; }
    public int Count { get; set; }

    public CountDto() { }

    public CountDto(EntityType type, int count)
    {
        Count = count;
        Type = type;
    }
}