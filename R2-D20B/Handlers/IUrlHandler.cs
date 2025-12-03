using DSharpPlus.EventArgs;

namespace R2D20B.Handlers
{
  internal interface IUrlHandler
  {
    public Task HandleAsync(List<UrlInfo> urlInfos, MessageCreatedEventArgs e);
    public bool CanHandle(UrlInfo urlInfo);
  }
}
