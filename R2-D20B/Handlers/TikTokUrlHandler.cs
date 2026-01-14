using System.Text;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;


namespace R2D20B.Handlers;


/// <summary>
/// Handles TikTok URLs found inside messages.
/// </summary>
internal class TikTokUrlHandler(ILogger<TikTokUrlHandler> logger)
  : UrlSwapHandler(logger)
{
  /// <summary>
  /// The hosts to look for and swap out.
  /// </summary>
  override protected string[] TargetHosts { get; } = [ "tiktok.com" ];
  
  /// <summary>
  /// The host to swap in.
  /// </summary>
  private static readonly string s_ReplacementHost = "tiktokez.com";


  override protected async Task HandleHelperAsync(IEnumerable<UrlInfo> urlInfos,
    MessageCreatedEventArgs e)
  {
    // Construct a StringBuilder that will contain the reply
    var replySb = new StringBuilder();

    foreach (var urlInfo in urlInfos)
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
