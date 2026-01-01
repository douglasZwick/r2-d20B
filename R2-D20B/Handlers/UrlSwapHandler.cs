using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace R2D20B.Handlers;


/// <summary>
/// Handles URLs by swapping the host for a fixer host that does better Discord embedding.
/// </summary>
internal abstract class UrlSwapHandler(ILogger logger) : IUrlHandler
{
  /// <summary>
  /// The hosts to look for and swap out.
  /// </summary>
  protected abstract string[] TargetHosts { get; }

  protected ILogger Logger { get; set; } = logger;


  public async Task HandleAsync(List<UrlInfo> urlInfos, MessageCreatedEventArgs e)
  {
    // Filter down to just the URLs that this handler can handle
    var relevantUrlInfos = urlInfos.Where(CanHandle);
    if (!relevantUrlInfos.Any()) return;

    await e.Channel.TriggerTypingAsync();

    await HandleHelperAsync(relevantUrlInfos, e);
  }

  abstract protected Task HandleHelperAsync(IEnumerable<UrlInfo> urlInfos,
    MessageCreatedEventArgs e);


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
