using System.Text;
using System.Text.Json;
using DSharpPlus.EventArgs;
using DSharpPlus.Net;

namespace R2D20B.Handlers
{
  internal class TwitterUrlHandler : IUrlHandler
  {
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

    
    private readonly HttpClient m_HttpClient;
    private readonly JsonSerializerOptions m_JsonOptions = new()
    {
      PropertyNameCaseInsensitive = true,
    };


    public TwitterUrlHandler(HttpClient httpClient)
    {
      m_HttpClient = httpClient;
    }

    
    public async Task HandleAsync(List<UrlInfo> urlInfos, MessageCreatedEventArgs e)
    {
      var relevantUrlInfos = urlInfos.Where(CanHandle);
      if (relevantUrlInfos.Count() <= 0) return;

      var replySb = new StringBuilder();

      foreach (var urlInfo in relevantUrlInfos)
      {
        if (!IsTweet(urlInfo)) continue;

        var fixTweetUrl = urlInfo.ReplaceHost("api.fxtwitter.com");
        
        using var response = await m_HttpClient.GetAsync(fixTweetUrl);

        if (!response.IsSuccessStatusCode)
        {
          // Consider logging or falling back to dumbly using vxtwitter, etc.
          continue;
        }

        var content = await response.Content.ReadAsStringAsync();

        var data = JsonSerializer.Deserialize<FixTweetResponseData>(content, m_JsonOptions);
        if (data?.Tweet is null) continue;
        if (data.Tweet.Media is null) continue;
        

        HandleMedia(data.Tweet, replySb);
      }

      if (replySb.Length <= 0) return;
      
      // await e.Message.ModifyEmbedSuppressionAsync(true);
      await e.Message.RespondAsync(replySb.ToString());
    }


    public bool CanHandle(UrlInfo urlInfo)
    {
      if (urlInfo.Host.Equals("vxtwitter.com", StringComparison.OrdinalIgnoreCase) ||
        urlInfo.Host.EndsWith(".vxtwitter.com", StringComparison.OrdinalIgnoreCase)) 
        return false;
      if (!(urlInfo.Host.Equals("twitter.com", StringComparison.OrdinalIgnoreCase) ||
        urlInfo.Host.EndsWith(".twitter.com", StringComparison.OrdinalIgnoreCase) ||
        urlInfo.Host.Equals("x.com", StringComparison.OrdinalIgnoreCase) ||
        urlInfo.Host.EndsWith(".x.com", StringComparison.OrdinalIgnoreCase)))
        return false;
      
      return true;
    }


    private static bool IsTweet(UrlInfo urlInfo)
    {
      return urlInfo.Uri.AbsolutePath.Contains("/status/", StringComparison.OrdinalIgnoreCase);
    }


    private static void HandleMedia(TweetData tweet, StringBuilder replySb)
    {
      if (tweet.Media is null) return;

      HandlePhotos(tweet.Media, replySb);
      HandleVideos(tweet.Media, replySb);
    }


    private static void HandlePhotos(MediaData media, StringBuilder replySb)
    {
      if (media.Photos is null) return;
      if (media.Photos.Length <= 0) return; // This probably can't happen after the above

      foreach (var imageDatum in media.Photos)
        replySb.AppendLine(imageDatum.Url);
    }


    private static void HandleVideos(MediaData media, StringBuilder replySb)
    {
      if (media.Videos is null) return;
      if (media.Videos.Length <= 0) return; // This probably can't happen after the above

      foreach (var videoDatum in media.Videos)
        replySb.AppendLine(videoDatum.Url);
    }
  }
}
