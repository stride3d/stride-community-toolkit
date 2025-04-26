namespace Example17_SignalR_Shared.Core;

public static class Constants
{
    public const string HubBaseUrl = "https://localhost:44304";
    public const string HubUrl = "screen1";
    public const string HubName = "Screen1Hub";

    public const string ReceiveMessageMethod = "ReceiveMessage";
    public const string ReceiveCountMethod = "ReceiveCount";

    public const string SendMessageMethod = "SendMessage";
    public const string SendCountMethod = "SendCount";

    public const int DefaultEntitiesCount = 10;
    public const string DefaultMessage = "Hello";
}