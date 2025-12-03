using System.Text;
using DSharpPlus.EventArgs;

namespace R2D20B.Handlers
{
  internal class TwitterUrlHandler : IUrlHandler
  {
    public async Task HandleAsync(List<UrlInfo> urlInfos, MessageCreatedEventArgs e)
    {
      var relevantUrls = urlInfos.Where(CanHandle);

      if (relevantUrls.Count() <= 0) return;

      var replySb = new StringBuilder();

      foreach (var urlInfo in relevantUrls)
      {
        var newUrl = urlInfo.ReplaceHost("vxtwitter.com");
        replySb.AppendLine(newUrl);
      }

      await e.Message.ModifyEmbedSuppressionAsync(true);
      await e.Message.RespondAsync(replySb.ToString());
    }


    public bool CanHandle(UrlInfo urlInfo)
    {
      if (urlInfo.Host.Equals("vxtwitter.com", StringComparison.OrdinalIgnoreCase) ||
        urlInfo.Host.EndsWith(".vxtwitter.com", StringComparison.OrdinalIgnoreCase)) 
        return false;
      if (!(urlInfo.Host.Equals("twitter.com", StringComparison.OrdinalIgnoreCase) ||
        urlInfo.Host.EndsWith(".twitter.com", StringComparison.OrdinalIgnoreCase) ||
        urlInfo.Host.Equals("x.com", StringComparison.OrdinalIgnoreCase) ||
        urlInfo.Host.EndsWith(".x.com", StringComparison.OrdinalIgnoreCase)))
        return false;
      
      return true;
    }
  }
}
