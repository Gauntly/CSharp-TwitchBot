using TwitchLib.Api;
using TwitchLib.Client.Events;
using TwitchLib.Client;

public abstract class BaseTwitchBotPlugin : ITwitchBotPlugin
{
    protected TwitchClient Client { get; private set; }
    protected TwitchAPI Api { get; private set; }

    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract IEnumerable<string> Commands { get; }

    public virtual void Initialize(TwitchClient client, TwitchAPI api)
    {
        Client = client;
        Api = api;
    }

    public abstract Task HandleMessage(OnMessageReceivedArgs message);
}