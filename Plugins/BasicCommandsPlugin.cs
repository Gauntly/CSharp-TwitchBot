using System;
using System.Threading.Tasks;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Api;
using TwitchLib.Client;

public class BasicCommandsPlugin : ITwitchBotPlugin
{
    private TwitchClient _client;
    private TwitchAPI _api;

    public string Name => "Basic Commands";
    public string Description { get; }
    public string[] Commands => new[] { "!hello", "!commands" };

    public void Initialize(TwitchClient client, TwitchAPI api)
    {
        _client = client;
        _api = api;
    }

    public async Task HandleMessage(OnMessageReceivedArgs e)
    {
        string message = e.ChatMessage.Message.ToLower();
        string channel = e.ChatMessage.Channel;
        string username = e.ChatMessage.DisplayName;

        switch (message)
        {
            case "!hello":
                _client.SendMessage(channel, $"Hello {username}! 👋");
                break;

            case "!commands":
                _client.SendMessage(channel, "Available commands: !hello, !uptime, !dice, !commands");
                break;
        }
    }

    IEnumerable<string> ITwitchBotPlugin.Commands => Commands;
} 