using TwitchLib.Api;
using TwitchLib.Client.Events;
using TwitchLib.Client;

public interface ITwitchBotPlugin
{
    string Name { get; }
    string Description { get; }
    void Initialize(TwitchClient client, TwitchAPI api);
    Task HandleMessage(OnMessageReceivedArgs message);
    IEnumerable<string> Commands { get; }
}