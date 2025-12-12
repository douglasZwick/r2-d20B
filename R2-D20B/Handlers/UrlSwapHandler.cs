using DSharpPlus.EventArgs;

namespace R2D20B.Handlers
{
  /// <summary>
  /// Handles URLs by swapping the host for a fixer host that does better Discord embedding.
  /// </summary>
  internal abstract class UrlSwapHandler : IUrlHandler
  {
    /// <summary>
    /// The hosts to look for and swap out.
    /// </summary>
    protected abstract string[] TargetHosts { get; }


    abstract public Task HandleAsync(List<UrlInfo> urlInfos, MessageCreatedEventArgs e);


    virtual public bool CanHandle(UrlInfo urlInfo)
    {
      return HasTargetHost(urlInfo);
    }


    /// <summary>
    /// Checks whether the host in the given URL is one that this handler can handle.
    /// </summary>
    /// <param name="urlInfo">The host to check.</param>
    /// <returns>True if this URL should be handled, false otherwise.</returns>
    protected bool HasTargetHost(UrlInfo urlInfo)
    {
      return TargetHosts.Any(host =>
        urlInfo.Host.Equals(host, StringComparison.OrdinalIgnoreCase) ||
        urlInfo.Host.EndsWith($".{host}", StringComparison.OrdinalIgnoreCase));
    }
  }
}
