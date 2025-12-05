using System.Text;
using DSharpPlus.EventArgs;

namespace R2D20B.Handlers
{
  /// <summary>
  /// Handles Instagram URLs found inside messages.
  /// </summary>
  internal class InstagramUrlHandler : IUrlHandler
  {
    public async Task HandleAsync(List<UrlInfo> urlInfos, MessageCreatedEventArgs e)
    {
      // Filter down to just the URLs that this handler can handle
      var relevantUrlInfos = urlInfos.Where(CanHandle);
      if (!relevantUrlInfos.Any()) return;

      // Construct a StringBuilder that will contain the reply
      var replySb = new StringBuilder();

      foreach (var urlInfo in relevantUrlInfos)
        replySb.AppendLine(urlInfo.ReplaceHost("kkinstagram.com"));

      if (replySb.Length <= 0) return;

      await e.Message.RespondAsync(replySb.ToString());
    }


    public bool CanHandle(UrlInfo urlInfo)
    {
      // Reject URLs that have already been handled
      if (urlInfo.Host.Equals("kkinstagram.com", StringComparison.OrdinalIgnoreCase) ||
        urlInfo.Host.EndsWith(".kkinstagram.com", StringComparison.OrdinalIgnoreCase)) 
        return false;

      // Reject non-Insta URLs
      if (!(urlInfo.Host.Equals("instagram.com", StringComparison.OrdinalIgnoreCase) ||
        urlInfo.Host.EndsWith(".instagram.com", StringComparison.OrdinalIgnoreCase)))
        return false;
      
      // If you've made it this far, you're good 👍
      return true;
    }
  }
}
