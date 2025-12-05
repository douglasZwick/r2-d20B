using System.Text;
using DSharpPlus.EventArgs;

namespace R2D20B.Handlers
{
  internal class InstagramUrlHandler : IUrlHandler
  {
    public async Task HandleAsync(List<UrlInfo> urlInfos, MessageCreatedEventArgs e)
    {
      var relevantUrlInfos = urlInfos.Where(CanHandle);
      if (relevantUrlInfos.Count() <= 0) return;

      var replySb = new StringBuilder();

      foreach (var urlInfo in relevantUrlInfos)
        replySb.AppendLine(urlInfo.ReplaceHost("kkinstagram.com"));

      if (replySb.Length <= 0) return;

      await e.Message.RespondAsync(replySb.ToString());
    }


    public bool CanHandle(UrlInfo urlInfo)
    {
      if (urlInfo.Host.Equals("kkinstagram.com", StringComparison.OrdinalIgnoreCase) ||
        urlInfo.Host.EndsWith(".kkinstagram.com", StringComparison.OrdinalIgnoreCase)) 
        return false;
      if (!(urlInfo.Host.Equals("instagram.com", StringComparison.OrdinalIgnoreCase) ||
        urlInfo.Host.EndsWith(".instagram.com", StringComparison.OrdinalIgnoreCase)))
        return false;
      
      return true;
    }
  }
}
