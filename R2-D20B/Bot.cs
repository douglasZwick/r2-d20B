using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using R2D20B.Commands;
using DSharpPlus.Commands.EventArgs;
using R2D20B.Attributes;
using DSharpPlus.Commands.Processors.TextCommands;
using Microsoft.Extensions.DependencyInjection;
using DSharpPlus.EventArgs;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using R2D20B.Handlers;
using Microsoft.Extensions.Logging;
using R2D20B.Components;


namespace R2D20B
{
  internal class Bot : IHostedService
  {
    static public ulong BotTestingChannelId { get; private set; }
    private static readonly string s_BotTestingChannelName = "bot-testing";
    private static readonly string s_UrlMatchPattern = @"\b(?:(?:https?)://)?" +
      @"(?:www\.)?(?:[a-zA-Z0-9-]+\.)+[a-zA-Z]{2,}(?:/[^\s]*)?";
    // private readonly bool m_TestingChannelOnly = true;
    
    public CommandsExtension? Commands { get; private set; }
    public DiscordGuild? DebugGuild { get; private set; }
    public DiscordClient Client { get; private set; }

    private IEnumerable<IUrlHandler> UrlHandlers { get; set; }
    private HttpClient HttpClient { get; set; }
    private ILogger<Bot> Logger { get; set; }


    public Bot(
      IEnumerable<IUrlHandler> urlHandlers,
      HttpClient httpClient,
      ILogger<Bot> logger)
    {
      UrlHandlers = urlHandlers;
      HttpClient = httpClient;

      Logger = logger;

      var token = BotConfig.GetToken();

      Client = DiscordClientBuilder
        .CreateDefault(token, DiscordIntents.All)
        .ConfigureServices(services =>
        {
          services.AddSingleton(_ => this);
          services.AddSingleton(_ => HttpClient);
        })
        .UseCommands((services, extension) =>
        {
          Commands = extension;
          extension.AddCommands(typeof(BasicCommands).Assembly);
          
          extension.CommandExecuted += OnCommandExecuted;
        })
        .ConfigureEventHandlers(b => b
          .HandleMessageCreated(OnMessageCreated)
          .HandleGuildDownloadCompleted(OnGuildDownloadCompleted)
        )
        .Build();
    }


    public Task StartAsync(CancellationToken cancellationToken)
    {
      var status = new DiscordActivity("Ligma", DiscordActivityType.Playing);
      return Client.ConnectAsync(status, DiscordUserStatus.Online);
    }


    public Task StopAsync(CancellationToken cancellationToken)
    {
      return Client.DisconnectAsync();
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


    private async Task OnGuildDownloadCompleted(DiscordClient client,
      GuildDownloadCompletedEventArgs e)
    {
      await Task.Run(async () =>
      {
        if (!e.Guilds.TryGetValue(BotConfig.GetDebugGuildId(), out DiscordGuild? guild)) return;

        DebugGuild = guild;
        var botTestingChannel = guild.Channels.Values.FirstOrDefault(
          c => c.Name == s_BotTestingChannelName);
        
        if (botTestingChannel is null) return;

        BotTestingChannelId = botTestingChannel.Id;
      });
    }


    private async Task OnMessageCreated(DiscordClient client,
      MessageCreatedEventArgs e)
    {
      if (e.Author.IsCurrent) return;

      await HandleUrls(client, e);
    }


    private async Task HandleUrls(DiscordClient client,
      MessageCreatedEventArgs e)
    {
      var urlInfos = CreateUrlInfoList(e.Message.Content);
      var count = urlInfos.Count;

      if (count <= 0) return;

      foreach (var handler in UrlHandlers)
        await handler.HandleAsync(urlInfos, e);
    }


    private List<UrlInfo> CreateUrlInfoList(string input)
    {
      var output = new List<UrlInfo>();
      var matches = Regex.Matches(input, s_UrlMatchPattern).Cast<Match>();

      foreach (var match in matches)
      {
        var urlInfo = new UrlInfo(match.Value);
        if (!urlInfo.IsValid) continue;
        output.Add(urlInfo);
      }
      
      return output;
    }
  }
}
