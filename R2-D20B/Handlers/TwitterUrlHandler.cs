using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Formatter = DSharpPlus.Formatter;

namespace R2D20B.Handlers
{
  /// <summary>
  /// Handles Twitter URLs found inside messages.
  /// </summary>
  internal class TwitterUrlHandler(HttpClient httpClient) : IUrlHandler
  {
    /// <summary>
    /// The host to swap in for single-video tweet links.
    /// </summary>
    private static readonly string s_SingleVideoHost = "vxtwitter.com";
    /// <summary>
    /// The host to swap in for multi-media tweet links. Specified by FixTweet.
    /// </summary>
    private static readonly string s_MultiMediaHost = "api.fxtwitter.com";
    // private static readonly string s_DateTimeFormat = "d-MMM-yy h:mm tt";
    private static readonly string s_FixTweetSignature = "FixTweet \u2715 R2-D20";

    private readonly HttpClient m_HttpClient = httpClient;
    private readonly JsonSerializerOptions m_JsonOptions = new()
      { PropertyNameCaseInsensitive = true, };


    #region Data Transfer Type Definitions
    private class FixTweetResponseData(int code, string message, ApiTweet tweet)
    {
      public int Code { get; set; } = code;
      public string Message { get; set; } = message;
      public ApiTweet Tweet { get; set; } = tweet;
    }


    private class ApiTweet(string id, string url, string text, string createdAt,
      ApiAuthor author, int likes, int retweets, int replies,
      int? views = null, ApiMedia? media = null)
    {
      public string Id { get; set; } = id;
      public string Url { get; set; } = url;
      public string Text { get; set; } = text;
      [JsonPropertyName("created_at")]
      public string CreatedAt { get; set; } = createdAt;
      public ApiAuthor Author { get; set; } = author;
      public ApiMedia? Media { get; set; } = media;
      public int Likes { get; set; } = likes;
      public int Retweets { get; set; } = retweets;
      public int Replies { get; set; } = replies;
      public int? Views { get; set; } = views;
    }


    private class ApiAuthor(string name, string screenName, string avatarUrl)
    {
      private static readonly string s_DefaultAvatarUrl =
        "https://abs.twimg.com/sticky/default_profile_images/default_profile_normal.png";

      public string Name { get; set; } = name;
      [JsonPropertyName("screen_name")]
      public string ScreenName { get; set; } = screenName;
      [JsonPropertyName("avatar_url")]
      public string AvatarUrl { get; set; } = avatarUrl ?? s_DefaultAvatarUrl;
    }


    private class ApiMedia(ApiPhotoData[]? photos = null, ApiVideoData[]? videos = null)
    {
      public ApiPhotoData[]? Photos { get; set; } = photos;
      public ApiVideoData[]? Videos { get; set; } = videos;
    }


    private class ApiPhotoData(string url)
    {
      public string Url { get; set; } = url;
    }


    private class ApiVideoData(string url)
    {
      public string Url { get; set; } = url;
    }
    #endregion


    private class TweetContext(UrlInfo urlInfo, ApiTweet tweet,
      ApiPhotoData[]? photos, ApiVideoData[]? videos)
    {
      private static readonly string s_DateTimeFormat = "ddd MMM dd HH:mm:ss K yyyy";

      public UrlInfo UrlInfo { get; set; } = urlInfo;
      public ApiTweet Tweet { get; set; } = tweet;
      public ApiPhotoData[] Photos { get; set; } = photos ?? [];
      public ApiVideoData[] Videos { get; set; } = videos ?? [];

      public int PhotoCount => Photos.Length;
      public int VideoCount => Videos.Length;
      public bool HasPhotos => PhotoCount > 0;
      public bool HasVideos => VideoCount > 0;
      public bool HasMedia => Tweet.Media is not null && (HasPhotos || HasVideos);
      public bool IsSingleImageOnly => PhotoCount == 1 && !HasVideos;
      public bool IsSingleVideoOnly => !HasPhotos && VideoCount == 1;

      public DateTimeOffset? Timestamp
      {
        get
        {
          try
          {
            return DateTimeOffset.ParseExact(Tweet.CreatedAt, s_DateTimeFormat,
              System.Globalization.CultureInfo.InvariantCulture);
          }
          catch (Exception e)
          {
            Console.Error.WriteLine($"Invalid timestamp in tweet from {UrlInfo.ToString()}. " +
              $"Exception: {e.Message}");
          }

          return null;
        }
      }
    }

    
    public async Task HandleAsync(List<UrlInfo> urlInfos, MessageCreatedEventArgs e)
    {
      // Filter down to just the URLs that this handler can handle
      var relevantUrlInfos = urlInfos.Where(CanHandle);
      if (!relevantUrlInfos.Any()) return;

      // Handle each URL
      foreach (var urlInfo in relevantUrlInfos)
      {
        var tweetContext = await TryGetTweetContextAsync(urlInfo);
        if (tweetContext is null) continue;

        if (!tweetContext.HasMedia || tweetContext.IsSingleImageOnly) continue;

        if (tweetContext.IsSingleVideoOnly)
        {
          await HandleSingleVideoAsync(tweetContext, e);
          continue;
        }
        
        await HandleMultiMediaAsync(tweetContext, e);
      }
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


    private async Task<TweetContext?> TryGetTweetContextAsync(UrlInfo urlInfo)
    {
      // First we replace the host in the original URL with the FixTweet API URL
      var fixTweetUrl = urlInfo.ReplaceHost(s_MultiMediaHost);
      
      // Then we send an HTTP request to the new URL
      using var response = await m_HttpClient.GetAsync(fixTweetUrl);

      // Consider logging or falling back to dumbly using vxtwitter, etc.
      if (!response.IsSuccessStatusCode) return null;

      var content = await response.Content.ReadAsStringAsync();
      var data = JsonSerializer.Deserialize<FixTweetResponseData>(content, m_JsonOptions);

      // Skip tweets that don't contain media
      if (data?.Tweet.Media is null) return null;

      return new TweetContext
      (
        urlInfo:  urlInfo,
        tweet:    data.Tweet,
        photos:   data.Tweet.Media?.Photos,
        videos:   data.Tweet.Media?.Videos
      );
    }


    private static async Task HandleSingleVideoAsync(TweetContext tweetContext,
      MessageCreatedEventArgs e)
    {
      var vxtwitterUrl = tweetContext.UrlInfo.ReplaceHost(s_SingleVideoHost);

      try
      {
        await e.Message.RespondAsync(vxtwitterUrl);
      }
      catch // (Exception ex)
      {
        // Consider logging the exception
        return;
      }

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


    private static async Task HandleMultiMediaAsync(TweetContext tweetContext,
      MessageCreatedEventArgs e)
    {
      var v2Builder = new DiscordMessageBuilder().EnableV2Components();
      var pseudoEmbed = CreateTweetPseudoEmbed(v2Builder, tweetContext);
      v2Builder.AddContainerComponent(pseudoEmbed);

      try
      {
        // await e.Channel.SendMessageAsync(videoBuilder);
        await e.Channel.SendMessageAsync(v2Builder);
      }
      catch // (Exception ex)
      {
        // Consider logging the exception
        return;
      }

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


    private static DiscordContainerComponent CreateTweetPseudoEmbed(
      DiscordMessageBuilder builder, TweetContext tweetContext)
    {
      var tweet = tweetContext.Tweet;
      var authorString = $"### {tweet.Author.Name} (@{tweet.Author.ScreenName})";
      var link = $"-# {tweet.Url}";
      
      var containerContents = new List<DiscordComponent>();

      var authorText = new DiscordTextDisplayComponent(authorString);
      var linkText = new DiscordTextDisplayComponent(link);
      var mainText = new DiscordTextDisplayComponent(tweet.Text);
      var authorThumbnail = new DiscordThumbnailComponent(tweet.Author.AvatarUrl);
      containerContents.Add(new DiscordSectionComponent(
        [authorText, linkText, mainText], authorThumbnail));
      containerContents.Add(new DiscordSeparatorComponent());

      containerContents.Add(CreateMediaGallery(tweetContext));
      
      // " \u2022 "
      var signature = $"-# {s_FixTweetSignature}";

      if (tweetContext.Timestamp is DateTimeOffset timestamp)
      {
        // var timestampStr = timestamp.ToLocalTime().ToString(s_DateTimeFormat,
        //   CultureInfo.InvariantCulture);
        var timestampStr = Formatter.Timestamp(timestamp, DSharpPlus.TimestampFormat.ShortDateTime);
        signature = $"{signature} \u2022 {timestampStr}";
      }

      containerContents.Add(new DiscordTextDisplayComponent(signature));

      return new DiscordContainerComponent(containerContents);
    }


    private static DiscordMediaGalleryComponent CreateMediaGallery(TweetContext tweetContext)
    {
      var galleryItems = new List<DiscordMediaGalleryItem>();

      foreach (var video in tweetContext.Videos)
        galleryItems.Add(new(video.Url));

      if (!tweetContext.HasPhotos)
        return new(galleryItems);

      foreach (var photo in tweetContext.Photos)
        galleryItems.Add(new(photo.Url));

      return new(galleryItems);
    }
  }
}
