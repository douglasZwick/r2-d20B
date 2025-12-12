using System.Text;
using DSharpPlus.EventArgs;

namespace R2D20B.Handlers
{
  /// <summary>
  /// Handles Instagram URLs found inside messages.
  /// </summary>
  internal class InstagramUrlHandler : UrlSwapHandler
  {
    /// <summary>
    /// The hosts to look for and swap out.
    /// </summary>
    override protected string[] TargetHosts { get; } = [ "instagram.com" ];
    
    /// <summary>
    /// The host to swap in.
    /// </summary>
    private static readonly string s_ReplacementHost = "kkinstagram.com";


    override public async Task HandleAsync(List<UrlInfo> urlInfos, MessageCreatedEventArgs e)
    {
      // Filter down to just the URLs that this handler can handle
      var relevantUrlInfos = urlInfos.Where(CanHandle);
      if (!relevantUrlInfos.Any()) return;

      // Construct a StringBuilder that will contain the reply
      var replySb = new StringBuilder();

      foreach (var urlInfo in relevantUrlInfos)
        replySb.AppendLine(urlInfo.ReplaceHost(s_ReplacementHost));

      if (replySb.Length <= 0) return;

      // Reply with the StringBuilder
      try
      {
        await e.Message.RespondAsync(replySb.ToString());
      }
      catch // (Exception ex)
      {
        // Consider logging the exception
        return;
      }
      
      // After we're sure the reply was sent without issue, hide OP's embed
      try
      {
        await e.Message.ModifyEmbedSuppressionAsync(true);
      }
      catch // (Exception ex)
      {
        // Consider logging the exception
        return;
      }
    }
  }
}
