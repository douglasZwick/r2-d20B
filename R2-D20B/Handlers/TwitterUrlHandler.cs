using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DSharpPlus.EventArgs;

namespace R2D20B.Handlers
{
  /// <summary>
  /// Handles Twitter URLs found inside messages.
  /// </summary>
  internal class TwitterUrlHandler : IUrlHandler
  {
    #region Data Transfer Type Definitions
    internal class FixTweetResponseData
    {
      public int Code { get; set; }
      public string? Message { get; set; }
      public TweetData? Tweet { get; set; }
    }


    internal class TweetData
    {
      public string? Id { get; set; }
      public string? Url { get; set; }
      public string? Text { get; set; }
      public string? Created_At { get; set; }
      public AuthorData? Author { get; set; }
      public MediaData? Media { get; set; }
      public int Likes { get; set; }
      public int Retweets { get; set; }
      public int Replies { get; set; }
      public int? Views { get; set; }
    }


    internal class AuthorData
    {
      public string? Name { get; set; }
      public string? Screen_Name { get; set; }
    }


    internal class MediaData
    {
      public ApiPhotoData[]? Photos { get; set; }
      public ApiVideoData[]? Videos { get; set; }
    }


    internal class ApiPhotoData
    {
      public string? Url { get; set; }
    }


    internal class ApiVideoData
    {
      public string? Url { get; set; }
    }
    #endregion

    
    /// <summary>
    /// The host to swap in for single-video tweet links. Specified by FixTweet.
    /// </summary>
    private static readonly string s_ReplacementHost = "api.fxtwitter.com";

    private readonly HttpClient m_HttpClient;
    private readonly JsonSerializerOptions m_JsonOptions = new()
      { PropertyNameCaseInsensitive = true, };


    public TwitterUrlHandler(HttpClient httpClient)
    {
      m_HttpClient = httpClient;
    }

    
    public async Task HandleAsync(List<UrlInfo> urlInfos, MessageCreatedEventArgs e)
    {
      // Filter down to just the URLs that this handler can handle
      var relevantUrlInfos = urlInfos.Where(CanHandle);
      if (!relevantUrlInfos.Any()) return;

      // This StringBuilder will be passed around through the pipeline, where various methods
      //   have a chance to append to it
      var replySb = new StringBuilder();

      // Handle each URL
      foreach (var urlInfo in relevantUrlInfos)
        HandleUrl(urlInfo, replySb);

      // If the StringBuilder is empty, it means no URLs that came through here had anything
      //   to add to it, which means there's nothing to say and we should just return
      if (replySb.Length <= 0) return;
      
      // await e.Message.ModifyEmbedSuppressionAsync(true);
      await e.Message.RespondAsync(replySb.ToString());
    }


    public bool CanHandle(UrlInfo urlInfo)
    {
      // Don't take URLs that aren't tweets
      if (!IsTweet(urlInfo)) return false;

      // Don't take URLs that are already using vxtwitter.
      // Technically it's possible that the user might post a link to a tweet that contains
      //   multple images, etc., that might benefit from going through the full pipeline, but
      //   the chances that someone will go out of their way to manually replace the URL
      //   for a link that would be better served without the replacement are slim enough
      //   that that case isn't worth considering.
      if (urlInfo.Host.Equals("vxtwitter.com", StringComparison.OrdinalIgnoreCase) ||
        urlInfo.Host.EndsWith(".vxtwitter.com", StringComparison.OrdinalIgnoreCase)) 
        return false;

      // If you've made it this far, you're good 👍
      return true;
    }


    /// <summary>
    /// Returns whether the given URL is the URL of a tweet.
    /// </summary>
    /// <param name="urlInfo">The URL data being considered.</param>
    /// <returns>True if it's a tweet, false if not.</returns>
    private static bool IsTweet(UrlInfo urlInfo)
    {
      // Don't take URLs that aren't for Twitter. The use of both Equals and EndsWith is to
      //   catch links like "https://twitter.com/..." and those with subdomains. The dot in
      //   front of the string passed into EndsWith is to prevent something like
      //   "notactuallytwitter.com" from matching.
      if (!(urlInfo.Host.Equals("twitter.com", StringComparison.OrdinalIgnoreCase) ||
        urlInfo.Host.EndsWith(".twitter.com", StringComparison.OrdinalIgnoreCase) ||
        urlInfo.Host.Equals("x.com", StringComparison.OrdinalIgnoreCase) ||
        urlInfo.Host.EndsWith(".x.com", StringComparison.OrdinalIgnoreCase)))
        return false;
      
      // All tweets have this string in their path
      return urlInfo.Uri.AbsolutePath.Contains("/status/", StringComparison.OrdinalIgnoreCase);
    }

    
    /// <summary>
    /// Handles media embedding for an individual tweet URL.
    /// </summary>
    /// <param name="urlInfo">The tweet's URL data.</param>
    /// <param name="replySb">The StringBuilder that will be used for the reply.</param>
    private async void HandleUrl(UrlInfo urlInfo, StringBuilder replySb)
    {
      // First we replace the host in the original URL with the FixTweet API URL
      var fixTweetUrl = urlInfo.ReplaceHost(s_ReplacementHost);
      
      // Then we send an HTTP request to the new URL
      using var response = await m_HttpClient.GetAsync(fixTweetUrl);

      // Consider logging or falling back to dumbly using vxtwitter, etc.
      if (!response.IsSuccessStatusCode) return;

      var content = await response.Content.ReadAsStringAsync();
      var data = JsonSerializer.Deserialize<FixTweetResponseData>(content, m_JsonOptions);

      // Skip tweets that don't contain media
      if (data?.Tweet?.Media is null) return;
      
      // Finally, handle any media present in this tweet
      HandleMedia(data.Tweet.Media, replySb);
    }


    /// <summary>
    /// Handles all media found in this tweet.
    /// </summary>
    /// <param name="media">The media to handle.</param>
    /// <param name="replySb">The StringBuilder to which to append reply text.</param>
    private static void HandleMedia(MediaData media, StringBuilder replySb)
    {
      HandlePhotos(media, replySb);
      HandleVideos(media, replySb);
    }


    /// <summary>
    /// Handles all still images found in this tweet.
    /// </summary>
    /// <param name="media">The media object containig the images to handle.</param>
    /// <param name="replySb">The StringBuilder to which to append reply text.</param>
    private static void HandlePhotos(MediaData media, StringBuilder replySb)
    {
      // Bail early if the tweet has no still images
      if (media.Photos is null) return;
      if (media.Photos.Length <= 0) return; // This probably can't happen after the above

      // Directly append the deep-link URL of the image to the reply
      foreach (var imageDatum in media.Photos)
        replySb.AppendLine(imageDatum.Url);
    }


    /// <summary>
    /// Handles all videos (including animated GIFs) found in this tweet.
    /// </summary>
    /// <param name="media">The media object containig the videos to handle.</param>
    /// <param name="replySb">The StringBuilder to which to append reply text.</param>
    private static void HandleVideos(MediaData media, StringBuilder replySb)
    {
      // Bail early if the tweet has no videos
      if (media.Videos is null) return;
      if (media.Videos.Length <= 0) return; // This probably can't happen after the above

      // Directly append the deep-link URL of the video to the reply
      foreach (var videoDatum in media.Videos)
        replySb.AppendLine(videoDatum.Url);
    }
  }
}
