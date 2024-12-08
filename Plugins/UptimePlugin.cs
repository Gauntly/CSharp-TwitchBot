using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;

public class UptimePlugin : ITwitchBotPlugin
{
    private TwitchClient _client;
    private TwitchAPI _api;

    public string Name => "Uptime";
    public string Description { get; }
    public string[] Commands => new[] { "!uptime" };

    public void Initialize(TwitchClient client, TwitchAPI api)
    {
        _client = client;
        _api = api;
    }

    public async Task HandleMessage(OnMessageReceivedArgs e)
    {
        if (e.ChatMessage.Message.ToLower() != "!uptime") return;

        string channel = e.ChatMessage.Channel;
        try
        {
            var stream = await _api.Helix.Streams.GetStreamsAsync(userLogins: new List<string> { channel });
            if (stream.Streams.Length > 0)
            {
                TimeSpan uptime = DateTime.UtcNow - stream.Streams[0].StartedAt;
                _client.SendMessage(channel, $"Stream has been live for {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s");
            }
            else
            {
                _client.SendMessage(channel, "Stream is offline!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting uptime: {ex.Message}");
        }
    }

    IEnumerable<string> ITwitchBotPlugin.Commands => Commands;
} 