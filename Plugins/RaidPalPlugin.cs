using System.Net.Http;
using System.Net.Http.Json;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Api;

public class RaidPalPlugin : ITwitchBotPlugin
{
    private TwitchClient _client;
    private TwitchAPI _api;
    private readonly HttpClient _httpClient;
    private const string RAIDPAL_API_URL = "https://api.raidpal.com/rest";  // Replace with actual API endpoint
    private string _accessToken;
    private string _clientId;

    public string Name => "RaidPal";
    public string Description { get; }
    public string[] Commands => new[] { "!raid", "!raidstats", "!events" };

    public RaidPalPlugin(string clientId = null, string accessToken = null)
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(RAIDPAL_API_URL);
        
        if (accessToken != null)
        {
            _accessToken = accessToken;
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
        }
        
        if (clientId != null)
        {
            _clientId = clientId;
            _httpClient.DefaultRequestHeaders.Add("Client-ID", _clientId);
        }
    }

    public void Initialize(TwitchClient client, TwitchAPI api)
    {
        _client = client;
        _api = api;
        
        // Use the bot's existing auth
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_api.Settings.AccessToken}");
        if (!string.IsNullOrEmpty(_api.Settings.ClientId))
        {
            _httpClient.DefaultRequestHeaders.Add("Client-ID", _api.Settings.ClientId);
        }
    }

    public async Task HandleMessage(OnMessageReceivedArgs e)
    {
        string message = e.ChatMessage.Message.ToLower();
        string channel = e.ChatMessage.Channel;
        string username = e.ChatMessage.DisplayName;

        if (message.StartsWith("!raid "))
        {
            string targetChannel = message.Substring(6).Trim();
            await HandleRaidCommand(channel, username, targetChannel);
        }
        else if (message == "!raidstats")
        {
            await HandleRaidStatsCommand(channel, username);
        }
        else if (message.StartsWith("!events"))
        {
            string targetChannel = null;
            var parts = message.Split(' ');
            if (parts.Length > 1)
            {
                targetChannel = parts[1].TrimStart('@');
            }
            await HandleEventsCommand(channel, username, targetChannel);
        }
    }

    IEnumerable<string> ITwitchBotPlugin.Commands => Commands;

    private async Task HandleRaidCommand(string channel, string username, string targetChannel)
    {
        try
        {
            // First check if both users are registered on RaidPal
            var sourceUserResponse = await _httpClient.GetAsync($"/user/{channel}");
            var targetUserResponse = await _httpClient.GetAsync($"/user/{targetChannel}");

            // Handle 401 Unauthorized responses
            if (sourceUserResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                targetUserResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _client.SendMessage(channel, 
                    $"@{username} Authentication error. Please reconnect your Twitch account to RaidPal.");
                return;
            }

            if (!sourceUserResponse.IsSuccessStatusCode || !targetUserResponse.IsSuccessStatusCode)
            {
                _client.SendMessage(channel, 
                    $"@{username} Both channels need to be registered on RaidPal! Register at https://raidpal.com/");
                return;
            }

            // If both users are registered, proceed with raid
            var response = await _httpClient.PostAsJsonAsync("/events", new
            {
                source = channel,
                target = targetChannel,
                initiator = username
            });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RaidResponse>();
                _client.SendMessage(channel, 
                    $"@{username} Raid initiated to {targetChannel}! Join the raid train! Check details at https://raidpal.com/");
            }
            else
            {
                _client.SendMessage(channel, 
                    $"@{username} Failed to initiate raid. Please try again later.");
            }
        }
        catch (Exception ex)
        {
            // Add more specific error handling
            if (ex is HttpRequestException httpEx && httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _client.SendMessage(channel, 
                    $"@{username} Authentication error. Please reconnect your Twitch account to RaidPal.");
            }
            else
            {
                Console.WriteLine($"Error in RaidPal API call: {ex.Message}");
                _client.SendMessage(channel, 
                    $"@{username} Sorry, there was an error processing your raid request.");
            }
        }
    }

    private async Task HandleRaidStatsCommand(string channel, string username)
    {
        try
        {
            // Example API call to get raid statistics
            var response = await _httpClient.GetAsync($"/stats/{channel}");

            if (response.IsSuccessStatusCode)
            {
                var stats = await response.Content.ReadFromJsonAsync<RaidStats>();
                _client.SendMessage(channel, 
                    $"@{username} Raid Stats: Raids Led: {stats.RaidsLed}, " +
                    $"Participated In: {stats.RaidsParticipated}, " +
                    $"Total Impact: {stats.TotalImpact} viewers");
            }
            else
            {
                _client.SendMessage(channel, 
                    $"@{username} Unable to fetch raid stats at this time.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching RaidPal stats: {ex.Message}");
            _client.SendMessage(channel, 
                $"@{username} Sorry, there was an error fetching your raid stats.");
        }
    }

    private async Task HandleEventsCommand(string channel, string username, string targetChannel = null)
    {
        try
        {
            string lookupChannel = targetChannel ?? channel;
            var response = await _httpClient.GetAsync($"/rest/user/{lookupChannel}");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _client.SendMessage(channel, 
                    $"@{username} Authentication error. The bot needs to be reconnected to RaidPal.");
                return;
            }

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RaidPalUserResponse>();
                if (result?.user != null)
                {
                    var upcomingEvents = result.user.events_joined
                        .Where(e => e.starttime > DateTime.UtcNow)
                        .OrderBy(e => e.starttime)
                        .Take(3);  // Show next 3 upcoming events

                    if (upcomingEvents.Any())
                    {
                        var eventsList = string.Join(" | ", upcomingEvents.Select(e => 
                            $"{e.title} ({e.starttime:MMM dd HH:mm} UTC)"));
                        _client.SendMessage(channel, 
                            $"@{username} Upcoming events for {result.user.display_name}: {eventsList}");
                    }
                    else
                    {
                        _client.SendMessage(channel, 
                            $"@{username} No upcoming events found for {result.user.display_name}");
                    }
                }
                else
                {
                    _client.SendMessage(channel, 
                        $"@{username} Not on RaidPal? Register at https://raidpal.com/");
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _client.SendMessage(channel, 
                    $"@{username} User not found on RaidPal. Register at https://raidpal.com/");
            }
            else
            {
                _client.SendMessage(channel, 
                    $"@{username} Unable to fetch events for {lookupChannel}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching RaidPal events: {ex.Message}");
            _client.SendMessage(channel, 
                $"@{username} Sorry, there was an error fetching events.");
        }
    }
}

// Response models for RaidPal API
public class RaidResponse
{
    public string RaidId { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RaidStats
{
    public int RaidsLed { get; set; }
    public int RaidsParticipated { get; set; }
    public int TotalImpact { get; set; }
}

// Update RaidPalUser model to match API response
public class RaidPalUser
{
    public string display_name { get; set; }
    public string profile_image { get; set; }
    public string twitch_uri { get; set; }
    public string timezone { get; set; }
    public List<RaidPalEvent> events_joined { get; set; }
}

// Update RaidPalEvent model to match API response
public class RaidPalEvent
{
    public string title { get; set; }
    public DateTime starttime { get; set; }
    public DateTime endtime { get; set; }
    public string raidpal_link { get; set; }
    public string api_link { get; set; }
}

// Add new response model to match the API
public class RaidPalUserResponse
{
    public RaidPalUser user { get; set; }
} 