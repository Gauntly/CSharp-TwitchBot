using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Teams;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Collections.Generic;
using TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation;

public class Bot
{
    private readonly TwitchClient _client;
    private readonly TwitchAPI _api;
    private readonly BotConfig _config;
    private const string PREFIX = "!";
    private string _channelName = "";
    private readonly List<ITwitchBotPlugin> _plugins = new();
    public Bot(IConfiguration configuration)
    {
        _config = new BotConfig();
        configuration.GetSection("BotConfig").Bind(_config);

        _api = new TwitchAPI();
        _api.Settings.AccessToken = _config.AccessToken.Replace("oauth:", "");
        _api.Settings.ClientId = _config.ClientId;
        _api.Settings.Secret = _config.ClientSecret;
        _channelName = _config.ChannelName;
        Console.WriteLine($"Access Token: {_config.AccessToken}");

        _client = new TwitchClient();
        var credentials = new ConnectionCredentials(_config.Username, _config.AccessToken);
        _client.Initialize(credentials);

        // Register event handlers
        _client.OnConnected += Client_OnConnected;
        _client.OnMessageReceived += Client_OnMessageReceived;
        LoadPlugins();
    }

    private void LoadPlugins()
    {
        // Add all plugins
        _plugins.Add(new DicePlugin());
        _plugins.Add(new BasicCommandsPlugin());
        _plugins.Add(new UptimePlugin());
        _plugins.Add(new SemanticKernelPlugin(_config.OpenAiKey));
        _plugins.Add(new RaidPalPlugin());
        
        // Initialize each plugin
        foreach (var plugin in _plugins)
        {
            plugin.Initialize(_client, _api);
            Console.WriteLine($"Loaded plugin: {plugin.Name}");
        }
    }

    public async Task Setup()
    {
        try
        {
            Console.WriteLine($"Attempting to join channel: {_channelName}");
            _client.JoinChannel(_channelName);
            
            // Add a small delay to ensure join completes
            await Task.Delay(2000);
            
            // Send welcome message
            _client.SendMessage(_channelName, "Hello chat! GauntlyBot is now online! Type !commands to see what I can do!");

            // First get user ID
            var users = await _api.Helix.Users.GetUsersAsync(logins: new List<string> { _channelName });
            if (users.Users.Length > 0)
            {
                var userId = users.Users[0].Id;
                // Now get channel information using the user ID
                var channelInfo = await _api.Helix.Channels.GetChannelInformationAsync(userId);
                if (channelInfo.Data.Length > 0)
                {
                    Console.WriteLine($"Channel Title: {channelInfo.Data[0].Title}");
                    Console.WriteLine($"Game: {channelInfo.Data[0].GameName}");
                }
            }
            else
            {
                Console.WriteLine($"Could not find user {_channelName}");
            }

            // Add a small delay to ensure join completes
            await Task.Delay(2000);
            
            // Check if we actually joined
            if (_client.JoinedChannels.Any())
            {
                Console.WriteLine($"Successfully joined channels: {string.Join(", ", _client.JoinedChannels.Select(c => c.Channel))}");
            }
            else
            {
                Console.WriteLine("Failed to join any channels!");
            }

            // Check connection state
            Console.WriteLine($"Client is connected: {_client.IsConnected}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during setup: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private void Client_OnConnected(object? sender, OnConnectedArgs e)
    {
        Console.WriteLine($"Logged in as | {_client.TwitchUsername}");
        Console.WriteLine($"Connected channels | {string.Join(", ", _client.JoinedChannels.Select(c => c.Channel))}");
    }

    private async void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        // Process all plugins
        foreach (var plugin in _plugins)
        {
            await plugin.HandleMessage(e);
        }
    }

    public void ListCommands()
    {
        foreach (var plugin in _plugins)
        {
            Console.WriteLine($"{plugin.Name}: {string.Join(", ", plugin.Commands)}");
        }
    }

    public async Task Start()
    {
        Console.WriteLine("Starting bot...");
        _client.Connect();
        
        // Add a small delay to ensure connection completes
        await Task.Delay(2000);
        
        if (!_client.IsConnected)
        {
            Console.WriteLine("Failed to connect to Twitch!");
            return;
        }
        
        await Setup();
        Console.WriteLine("Bot setup completed");
    }
} 