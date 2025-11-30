using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using R2D20B.Commands;
using DSharpPlus.Commands.EventArgs;
using R2D20B.Attributes;
using DSharpPlus.Commands.Processors.TextCommands;
using Microsoft.Extensions.DependencyInjection;
using DSharpPlus.EventArgs;


namespace R2D20B
{
  internal class Bot
  {
    public DiscordClient m_Client;
    public CommandsExtension? m_CommandsExtension;
    public DiscordGuild? m_DebugGuild;
    public ulong m_BotTestingChannelId;
    public readonly string m_BotTestingChannelName = "bot-testing";


    public Bot()
    {
      var token = BotConfig.GetToken();

      m_Client = DiscordClientBuilder
        .CreateDefault(token, DiscordIntents.All)
        .ConfigureServices(services =>
        {
          services.AddSingleton(this);
        })
        .UseCommands((services, extension) =>
        {
          m_CommandsExtension = extension;
          extension.AddCommands(typeof(BasicCommands).Assembly);
          
          extension.CommandExecuted += OnCommandExecuted;
        })
        .ConfigureEventHandlers(b => b
          .HandleMessageCreated(OnMessageCreated)
          .HandleGuildDownloadCompleted(OnGuildDownloadCompleted)
        )
        .Build();
      
      // m_Commands = m_CommandsExtension!.Commands.Values;
    }

    public async Task RunAsync()
    {
      var status =
        new DiscordActivity("Ligma", DiscordActivityType.Playing);
      await m_Client.ConnectAsync(status, DiscordUserStatus.Online);

      await Task.Delay(-1);
    }


    private async Task OnCommandExecuted(CommandsExtension extension,
      CommandExecutedEventArgs e)
    {
      var attribs = e.Context.Command.Attributes;
      var shouldAutoDelete = attribs.OfType<AutoDeleteAttribute>().Any();

      if (shouldAutoDelete)
      {
        if (e.Context is TextCommandContext textCtx)
        {
          try
          {
            await textCtx.Message.DeleteAsync("Auto-delete command message");
          }
          catch (Exception ex)
          {
            var msg = textCtx.Message.Content;
            Console.WriteLine("Failed to delete auto-delete command" +
              $"message {msg}. Exception: {ex}");
          }
        }
      }
    }


    private async Task OnMessageCreated(DiscordClient client,
      MessageCreatedEventArgs e)
    {
      if (e.Channel.Id != m_BotTestingChannelId) return;
      if (e.Author == client.CurrentUser) return;

      await e.Message.RespondAsync($"Message received: {e.Message.Content}");
    }


    private async Task OnGuildDownloadCompleted(DiscordClient client,
      GuildDownloadCompletedEventArgs e)
    {
      DiscordGuild? guild;

      if (!e.Guilds.TryGetValue(BotConfig.GetDebugGuildId(), out guild)) return;

      m_DebugGuild = guild;
      var botTestingChannel = guild.Channels.Values.Where(
        c => c.Name == m_BotTestingChannelName).FirstOrDefault();
      
      if (botTestingChannel is null) return;

      m_BotTestingChannelId = botTestingChannel.Id;
    }
  }
}
