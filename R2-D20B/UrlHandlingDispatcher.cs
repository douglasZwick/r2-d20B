using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using R2D20B.Handlers;


namespace R2D20B
{
  internal sealed class UrlHandlingDispatcher(
    IEnumerable<IUrlHandler> urlHandlers,
    HttpClient httpClient,
    ILogger<UrlHandlingDispatcher> logger)
  {
    private static readonly string s_UrlMatchPattern = @"\b(?:(?:https?)://)?" +
      @"(?:www\.)?(?:[a-zA-Z0-9-]+\.)+[a-zA-Z]{2,}(?:/[^\s]*)?";
    
    private IEnumerable<IUrlHandler> UrlHandlers { get; init; } = urlHandlers;
    private HttpClient HttpClient { get; init; } = httpClient;
    private ILogger<UrlHandlingDispatcher> Logger { get; init; } = logger;


    public async Task HandleMessageCreatedAsync(DiscordClient client,
      MessageCreatedEventArgs e)
    {
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
