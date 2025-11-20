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

      // builder.UseCommands(
      //   (IServiceProvider services, CommandsExtension extension) =>
      //   {
          
      //   }
      // );
      
      var client = builder.Build();

      var status =
        new DiscordActivity("with power", DiscordActivityType.Playing);
      await client.ConnectAsync(status, DiscordUserStatus.Online);

      await Task.Delay(-1);
    }


    private static ulong ReadDebugGuildIdFromEnvironment()
    {
      var rawString = Environment.GetEnvironmentVariable("DEBUG_GUILD_ID");

      if (ulong.TryParse(rawString, out var id))
        return id;
      
      return 0;
    }
  }
}
