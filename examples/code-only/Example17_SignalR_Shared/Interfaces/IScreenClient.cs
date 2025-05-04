using Example17_SignalR_Shared.Dtos;

namespace Example17_SignalR_Shared.Interfaces;

public interface IScreenClient
{
    Task ReceiveMessageAsync(MessageDto dto);
    Task ReceiveCountAsync(CountDto dto);
    Task ReceiveUnitsRemovedAsync(List<CountDto> units);
}