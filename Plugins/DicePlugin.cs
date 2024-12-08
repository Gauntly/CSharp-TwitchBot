using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

public class DicePlugin : BaseTwitchBotPlugin
{
    public override string Name => "Dice Roller";
    public override string Description => "Adds dice rolling commands to the bot";
    public override IEnumerable<string> Commands => new[] { "!dice", "!roll" };

    private readonly Random _random = new Random();

    public override Task HandleMessage(OnMessageReceivedArgs message)
    {
        if (message.ChatMessage.Message.ToLower() == "!dice")
        {
            Client.SendMessage(message.ChatMessage.Channel,
                $"{message.ChatMessage.DisplayName} rolled a {_random.Next(1, 7)} 🎲");
        }
        return Task.CompletedTask;
    }
}