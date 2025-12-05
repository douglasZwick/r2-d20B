using DSharpPlus.EventArgs;

namespace R2D20B.Handlers
{
  /// <summary>
  /// Handles URLs found inside messages.
  /// </summary>
  internal interface IUrlHandler
  {
    /// <summary>
    /// Handles the given list of URLs, deciding what to do with each, which often involves 
    /// replying to the message where they were found.
    /// </summary>
    /// <param name="urlInfos">The list of URL data.</param>
    /// <param name="e">The event data from the message that contained the URLs.</param>
    /// <returns></returns>
    public Task HandleAsync(List<UrlInfo> urlInfos, MessageCreatedEventArgs e);
    /// <summary>
    /// Returns whether a given URL is relevant to this handler.
    /// </summary>
    /// <param name="urlInfo">The URL data being considered.</param>
    /// <returns></returns>
    public bool CanHandle(UrlInfo urlInfo);
  }
}
