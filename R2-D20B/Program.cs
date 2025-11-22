using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;


namespace R2D20B
{
  internal class Program
  {
    static async Task Main(string[] args)
    {
      var token = BotConfig.GetToken();

      var builder =
        DiscordClientBuilder.CreateDefault(token, DiscordIntents.All);

      builder.UseCommands((services, extension) =>
      {
        extension.AddCommands(typeof(Commands.BasicCommands));
      });
      
      var client = builder.Build();

      var status =
        new DiscordActivity("Ligma", DiscordActivityType.Playing);
      await client.ConnectAsync(status, DiscordUserStatus.Online);

      await Task.Delay(-1);
    }
  }
}
