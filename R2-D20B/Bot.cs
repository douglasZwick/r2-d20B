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
using System.Text;
using Microsoft.Extensions.Hosting;
using R2D20B.Handlers;


namespace R2D20B
{
  internal class Bot : IHostedService
  {
    static public ulong m_BotTestingChannelId;
    
    public CommandsExtension? m_CommandsExtension;
    public DiscordGuild? m_DebugGuild;
    public readonly string m_BotTestingChannelName = "bot-testing";
    public readonly bool m_TestingChannelOnly = true;

    private readonly DiscordClient m_Client;
    private readonly IEnumerable<IUrlHandler> m_UrlHandlers;
    private readonly HttpClient m_HttpClient;
    private string m_UrlMatchPattern = @"\b(?:(?:https?)://)?" +
      @"(?:www\.)?(?:[a-zA-Z0-9-]+\.)+[a-zA-Z]{2,}(?:/[^\s]*)?";


    public Bot(IEnumerable<IUrlHandler> urlHandlers, HttpClient httpClient)
    {
      m_UrlHandlers = urlHandlers;
      m_HttpClient = httpClient;

      var token = BotConfig.GetToken();

      m_Client = DiscordClientBuilder
        .CreateDefault(token, DiscordIntents.All)
        .ConfigureServices(services =>
        {
          services.AddSingleton(_ => this);
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
    }


    public Task StartAsync(CancellationToken cancellationToken)
    {
      var status = new DiscordActivity("Ligma", DiscordActivityType.Playing);
      return m_Client.ConnectAsync(status, DiscordUserStatus.Online);
    }


    public Task StopAsync(CancellationToken cancellationToken)
    {
      return m_Client.DisconnectAsync();
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
      DiscordGuild? guild;

      if (!e.Guilds.TryGetValue(BotConfig.GetDebugGuildId(), out guild)) return;

      m_DebugGuild = guild;
      var botTestingChannel = guild.Channels.Values.Where(
        c => c.Name == m_BotTestingChannelName).FirstOrDefault();
      
      if (botTestingChannel is null) return;

      m_BotTestingChannelId = botTestingChannel.Id;
    }


    private async Task OnMessageCreated(DiscordClient client,
      MessageCreatedEventArgs e)
    {
      if (e.Author == client.CurrentUser) return;

      await HandleUrls(client, e);
    }


    private async Task HandleUrls(DiscordClient client,
      MessageCreatedEventArgs e)
    {
      var urlInfos = CreateUrlInfoList(e.Message.Content);
      var count = urlInfos.Count;

      if (count <= 0) return;

      foreach (var handler in m_UrlHandlers)
        await handler.HandleAsync(urlInfos, e);
    }


    private List<string> ExtractUrls(string input)
    {
      var output = new List<string>();
      var matches = Regex.Matches(input, m_UrlMatchPattern).Cast<Match>();

      foreach (var match in matches)
        output.Add(match.ToString());
      
      return output;
    }


    private List<UrlInfo> CreateUrlInfoList(string input)
    {
      var output = new List<UrlInfo>();
      var matches = Regex.Matches(input, m_UrlMatchPattern).Cast<Match>();

      foreach (var match in matches)
      {
        var urlInfo = new UrlInfo(match.Value);
        if (!urlInfo.IsValid) continue;
        output.Add(urlInfo);
      }
      
      return output;
    }


    private async Task HandleTwitterUrls(DiscordClient client,
      MessageCreatedEventArgs e, List<string> urls)
    {
      var fixedUrls = new List<string>();

      foreach (var url in urls)
      {
        var fixedUrl = FixTwitterLink(url);

        if (!string.IsNullOrEmpty(fixedUrl))
          fixedUrls.Add(fixedUrl);
      }

      if (fixedUrls.Count <= 0) return;

      var replySb = new StringBuilder();

      foreach (var fixedUrl in fixedUrls)
        replySb.AppendLine(fixedUrl);

      await e.Message.ModifyEmbedSuppressionAsync(true);
      await e.Message.RespondAsync(replySb.ToString());
    }


    private async Task GetTweetInfoAsync(string url)
    {
      
    }


    private string FixTwitterLink(string url)
    {
      if (!(url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
        url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)))
        url = "https://" + url;
      
      if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        return string.Empty;
      
      var host = uri.Host;
      
      if (host.Equals("vxtwitter.com", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".vxtwitter.com", StringComparison.OrdinalIgnoreCase)) 
        return string.Empty;
      if (!(host.Equals("twitter.com", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".twitter.com", StringComparison.OrdinalIgnoreCase) ||
        host.Equals("x.com", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".x.com", StringComparison.OrdinalIgnoreCase)))
        return string.Empty;

      var leftSide = "https://vxtwitter.com";
      var pathAndQuery = uri.PathAndQuery;
      var fragment = uri.Fragment;

      return leftSide + pathAndQuery + fragment;
    }
  }
}
