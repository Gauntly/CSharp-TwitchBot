public class BotConfig
{
    public string Username { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; } // Add this if you have it
    public string ChannelName { get; set; } // The name of the channel you want to join.
    public string OpenAiKey { get; set; }
}