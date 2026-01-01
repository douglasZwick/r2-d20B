using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;


namespace R2D20B;


internal sealed class GatewayEventHandlers(UrlHandlingDispatcher dispatcher)
{
  static public ulong BotTestingChannelId { get; private set; }
  private static readonly string s_BotTestingChannelName = "bot-testing";
  // private readonly bool m_TestingChannelOnly = true;

  private UrlHandlingDispatcher Dispatcher { get; init; } = dispatcher;

  public DiscordGuild? DebugGuild { get; private set; }


  public async Task OnMessageCreated(DiscordClient client,
    MessageCreatedEventArgs e)
  {
    if (e.Author.IsCurrent) return;
    
    await Dispatcher.HandleMessageCreatedAsync(client, e);
  }


  public async Task OnGuildDownloadCompleted(DiscordClient client,
    GuildDownloadCompletedEventArgs e)
  {
    if (!e.Guilds.TryGetValue(BotConfig.GetDebugGuildId(), out DiscordGuild? guild)) return;

    DebugGuild = guild;
    var botTestingChannel = guild.Channels.Values.FirstOrDefault(
      c => c.Name == s_BotTestingChannelName);
    
    if (botTestingChannel is null) return;

    BotTestingChannelId = botTestingChannel.Id;
  }
}
