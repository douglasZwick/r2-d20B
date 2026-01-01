using System.Text.Json;
using System.Text.Json.Serialization;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Formatter = DSharpPlus.Formatter;


namespace R2D20B.Handlers;


/// <summary>
/// Handles Twitter URLs found inside messages.
/// </summary>
internal class TwitterUrlHandler(HttpClient httpClient, ILogger<TwitterUrlHandler> logger)
  : UrlSwapHandler(logger)
{
  /// <summary>
  /// The hosts to look for and swap out.
  /// </summary>
  override protected string[] TargetHosts { get; } = [ "x.com", "twitter.com" ];

  /// <summary>
  /// The host to swap in for single-video tweet links.
  /// </summary>
  private static readonly string s_SingleMediaHost = "vxtwitter.com";
  /// <summary>
  /// The host to swap in for multi-media tweet links. Specified by FixTweet.
  /// </summary>
  private static readonly string s_MultipleMediaHost = "api.fxtwitter.com";
  /// <summary>
  /// Signature to display in the footer for galleries, just before the timestamp.
  /// \u00d7 is the BMP code point for the multiplication sign, ×
  /// </summary>
  private static readonly string s_FixTweetSignature = "FixTweet \u00d7 R2-D20";
  private static readonly int s_QuoteDepthLimit = 1;

  private readonly HttpClient m_HttpClient = httpClient;
  private readonly JsonSerializerOptions m_JsonOptions = new()
    { PropertyNameCaseInsensitive = true, };
  private int m_QuoteDepth = 0;
  private bool QuoteDepthExceeded => m_QuoteDepth > s_QuoteDepthLimit;
  private void QuoteReset() { m_QuoteDepth = 0; }
  private void QuoteIncrement() { ++m_QuoteDepth; }


  #region Data Transfer Type Definitions
  private class FixTweetResponseData(int code, string message, ApiTweet tweet)
  {
    public int Code { get; set; } = code;
    public string Message { get; set; } = message;
    public ApiTweet Tweet { get; set; } = tweet;
  }


  private class ApiTweet(string id, string url, string text, string createdAt,
    string? replyingTo, string? replyingToStatus, ApiAuthor author, int likes, int retweets,
    int replies, int? views = null, ApiMedia? media = null, ApiTweet? quote = null)
  {
    public string Id { get; set; } = id;
    public string Url { get; set; } = url;
    public string Text { get; set; } = text;
    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = createdAt;
    [JsonPropertyName("replying_to")]
    public string? ReplyingTo { get; set; } = replyingTo;
    [JsonPropertyName("replying_to_status")]
    public string? ReplyingToStatus { get; set; } = replyingToStatus;
    public ApiAuthor Author { get; set; } = author;
    public ApiMedia? Media { get; set; } = media;
    public int Likes { get; set; } = likes;
    public int Retweets { get; set; } = retweets;
    public int Replies { get; set; } = replies;
    public int? Views { get; set; } = views;
    public ApiTweet? Quote { get; set; } = quote;
  }


  private class ApiAuthor(string name, string screenName, string avatarUrl)
  {
    private static readonly string s_DefaultAvatarUrl =
      "https://abs.twimg.com/sticky/default_profile_images/default_profile_400x400.png";

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
    public bool HasMultipleMedia => PhotoCount + VideoCount > 1;

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

  
  override protected async Task HandleHelperAsync(IEnumerable<UrlInfo> urlInfos,
    MessageCreatedEventArgs e)
  {
    // Handle each URL
    foreach (var urlInfo in urlInfos)
    {
      QuoteReset();

      await HandleUrl(urlInfo, e);
    }
  }


  override public bool CanHandle(UrlInfo urlInfo)
  {
    // Don't take URLs that aren't tweets
    return base.CanHandle(urlInfo) && IsTweet(urlInfo);
  }


  /// <summary>
  /// Returns whether the given URL is the URL of a tweet.
  /// </summary>
  /// <param name="urlInfo">The URL data being considered.</param>
  /// <returns>True if it's a tweet, false if not.</returns>
  private bool IsTweet(UrlInfo urlInfo)
  {
    // Don't take URLs that aren't for Twitter. The use of both Equals and EndsWith is to
    //   catch links like "https://twitter.com/..." and those with subdomains. The dot in
    //   front of the string passed into EndsWith is to prevent something like
    //   "notactuallytwitter.com" from matching.
    if (!HasTargetHost(urlInfo)) return false;
    
    // All tweets have this string in their path
    return urlInfo.Uri.AbsolutePath.Contains("/status/", StringComparison.OrdinalIgnoreCase);
  }


  /// <summary>
  /// Handles the given URL based on its media content. See internal comments for details.
  /// </summary>
  /// <param name="urlInfo">The URL to handle.</param>
  /// <param name="e">The event data from the original message.</param>
  private async Task HandleUrl(UrlInfo urlInfo, MessageCreatedEventArgs e)
  {
    // First we get the data representing what the tweet looks like. Bail on fail (rare).
    var tweetContext = await TryGetTweetContextAsync(urlInfo);
    if (tweetContext is null) return;

    // At this point, we can't so easily return early because we have to check for a parent tweet
    //   regardless of whether this one contains any media to embed

    var quoting = m_QuoteDepth > 0;
    // Single-media embedding is done via URL swapping
    if (tweetContext.IsSingleVideoOnly || tweetContext.IsSingleImageOnly && quoting)
      await HandleSingleMediaAsync(tweetContext, e, quoting);
    // Multiple-media embedding is done via a media gallery component
    else if (tweetContext.HasMultipleMedia)
      await HandleMultipleMediaAsync(tweetContext, e, quoting);

    // Next, we assume there's a quote and go ahead with incrementing the depth, and then bail
    //   if this would put us too deep. This is so we can avoid having to needlessly compute
    //   ParentUrl (a tiny tiny tiny optimization) in cases where we'd be too deep anyway
    QuoteIncrement();
    if (QuoteDepthExceeded) return;

    // If there is a quoted tweet to worry about...
    if (tweetContext.Tweet.Quote is ApiTweet quote)
    {
      var quoteUrlInfo = new UrlInfo(quote.Url);
      
      // Return early if either the UrlInfo is invalid or if this handler can't handle it.
      //   (I don't actually think either of these situations can occur.)
      if (!quoteUrlInfo.IsValid) return;
      if (!CanHandle(quoteUrlInfo)) return;

      // If everything looks good, recurse
      await HandleUrl(quoteUrlInfo, e);
    }
  }

  
  /// <summary>
  /// Uses the FixTweet API to construct an object with all the information we need to know what
  /// to do with the given Twitter URL.
  /// </summary>
  /// <param name="urlInfo">The URL to handle.</param>
  private async Task<TweetContext?> TryGetTweetContextAsync(UrlInfo urlInfo)
  {
    // First we replace the host in the original URL with the FixTweet API URL
    var fixTweetUrl = urlInfo.ReplaceHost(s_MultipleMediaHost);
    
    // Then we send an HTTP request to the new URL
    using var response = await m_HttpClient.GetAsync(fixTweetUrl);

    // Consider logging or falling back to dumbly using vxtwitter, etc.
    if (!response.IsSuccessStatusCode) return null;

    var content = await response.Content.ReadAsStringAsync();
    var data = JsonSerializer.Deserialize<FixTweetResponseData>(content, m_JsonOptions);

    // Skip tweets that don't contain media and are not quoting another tweet
    if (data?.Tweet.Media is null && data?.Tweet.Quote is null) return null;

    // If we've made it this far, this is a tweet with media, so we build and return the
    //   context object for it.
    return new TweetContext
    (
      urlInfo:  urlInfo,
      tweet:    data.Tweet,
      photos:   data.Tweet.Media?.Photos,
      videos:   data.Tweet.Media?.Videos
    );
  }


  /// <summary>
  /// Handles embedding for tweets that contain exactly one video or image. Not called for
  /// single-image tweets except if it's a quoted tweet.
  /// </summary>
  /// <param name="tweetContext">The tweet to handle.</param>
  /// <param name="e">The event data from the original message.</param>
  private static async Task HandleSingleMediaAsync(TweetContext tweetContext,
    MessageCreatedEventArgs e, bool quoting = false)
  {
    // Swap the host for the one that we use for tweets like this
    var message = tweetContext.UrlInfo.ReplaceHost(s_SingleMediaHost);
    if (quoting)
      message = $"{Formatter.Bold("Quoting:")} {message}";

    // Reply with it
    try
    {
      await e.Message.RespondAsync(message);
    }
    catch // (Exception ex)
    {
      // Consider logging the exception
      return;
    }
    
    if (!quoting)
    {
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


  /// <summary>
  /// Handles embedding for tweets that contain two or more images and/or videos. Uses Discord 
  /// V2 Components.
  /// </summary>
  /// <param name="tweetContext">The tweet to handle.</param>
  /// <param name="e">The event data from the original message.</param>
  private static async Task HandleMultipleMediaAsync(TweetContext tweetContext,
    MessageCreatedEventArgs e, bool quoting = false)
  {
    // We need to create a multi-item media gallery to handle this correctly, which means we need
    //   to use V2 Components. This locks us out of a bunch of functionality, but we don't need
    //   that stuff in here.
    var v2Builder = new DiscordMessageBuilder().EnableV2Components();
    if (quoting)
      v2Builder.AddTextDisplayComponent(Formatter.Bold("Quoting:"));
    // Construct the "pseudo-embed" with its text and media gallery, etc.,
    //   then add it to the builder
    var pseudoEmbed = CreateTweetPseudoEmbed(tweetContext);
    v2Builder.AddContainerComponent(pseudoEmbed);

    // Reply with it
    try
    {
      // await e.Channel.SendMessageAsync(videoBuilder);
      await e.Message.RespondAsync(v2Builder);
    }
    catch // (Exception ex)
    {
      // Consider logging the exception
      return;
    }

    if (!quoting)
    {
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


  /// <summary>
  /// Creates a container component for the tweet that resembles a classic DiscordEmbed.
  /// </summary>
  /// <param name="tweetContext">The tweet to handle.</param>
  /// <returns>The pseudo-embed container.</returns>
  private static DiscordContainerComponent CreateTweetPseudoEmbed(TweetContext tweetContext)
  {
    //  Here's the structure of the pseudo-embed:
    //  - Container component (round rectangle with a colored background. Can
    //    contain any number of other components, but NOT other containers.)
    //    - Section component (can contain 1-3 text displays, and MUST contain a thumbnail.)
    //      - Text display component (for the author text, acting as the pseudo-embed's title)
    //      - Text display component (for the link text. We let Discord's auto parsing handle it.)
    //      - Text display component (for the main tweet text content)
    //      - Thumbnail component (for the tweet author's avatar)
    //    - Separator component (adds a little space between the text and the gallery)
    //    - Media gallery component (dynamic rectangular grid for images / videos)
    //    - Text display component (for the signature and timestamp)

    var tweet = tweetContext.Tweet;

    // E.g. "### Dougward Zwick (@douglaszwick)"
    var authorString = $"### {tweet.Author.Name} (@{tweet.Author.ScreenName})";
    // Uses the "small text" header to shrink the URL. Lets Discord's automatic link parsing turn
    //   this into a clickable link.
    var link = $"-# {tweet.Url}";
    
    // We need an IEnumerable<DiscordComponent> to pass into the section component's ctor.
    //   A section can contain up to three text displays, and MUST (for some reason) contain
    //   a thumbnail component.
    var containerContents = new List<DiscordComponent>();

    // The author text serves as the title of the pseudo-embed
    var authorText = new DiscordTextDisplayComponent(authorString);
    // The link is small, below the author text
    var linkText = new DiscordTextDisplayComponent(link);

    // The main text content from the tweet
    var mainText = new DiscordTextDisplayComponent(tweet.Text);
    // The author's Twitter avatar
    var authorThumbnail = new DiscordThumbnailComponent(tweet.Author.AvatarUrl);
    // Creates the title section. Must take a list of contained components at construction time
    containerContents.Add(new DiscordSectionComponent(
      [authorText, linkText, mainText], authorThumbnail));

    // A small separator before the gallery
    containerContents.Add(new DiscordSeparatorComponent());

    // Creates the gallery. By the time we get it here, it contains all the images / videos.
    containerContents.Add(CreateMediaGallery(tweetContext));
    
    // Uses small text again to shrink the signature
    var signature = $"-# {s_FixTweetSignature}";
    // Not sure what could cause the timestamp to be null, but we wrap it anyway
    if (tweetContext.Timestamp is DateTimeOffset timestamp)
    {
      // \u2022 is the BMP code point for a bullet, •
      var timestampStr = Formatter.Timestamp(timestamp, DSharpPlus.TimestampFormat.ShortDateTime);
      signature = $"{signature} \u2022 {timestampStr}";
    }

    // Adds the signature text display that we just made
    containerContents.Add(new DiscordTextDisplayComponent(signature));

    // Finally, creates and returns the container component
    return new DiscordContainerComponent(containerContents);
  }


  /// <summary>
  /// Creates a media gallery containing the tweet's images / videos.
  /// </summary>
  /// <param name="tweetContext">The tweet to handle.</param>
  /// <returns>The newly minted media gallery in all its glory.</returns>
  private static DiscordMediaGalleryComponent CreateMediaGallery(TweetContext tweetContext)
  {
    // The gallery's ctor requires an IEnumerable of the items up front
    var galleryItems = new List<DiscordMediaGalleryItem>();
    // Gallery item has a ctor that takes an "unfurled media object", but we can just use
    //   the one that takes a media URL, which is what we already have
    
    // We start with videos.
    // TODO:
    //   Someday it may be possible to ascertain the order of the media in a tweet. If that
    //   happens, consider coming back here so we can order the images and videos correctly
    foreach (var video in tweetContext.Videos)
      galleryItems.Add(new(video.Url));
    // Then we do the photos. Note that it really kinda irks me that the FixTweet API calls
    //   them "photos", when we know that they're images but we don't know that they're actually
    //   photographs
    foreach (var photo in tweetContext.Photos)
      galleryItems.Add(new(photo.Url));

    // Finally, we construct and return the new media gallery
    return new(galleryItems);
  }
}
