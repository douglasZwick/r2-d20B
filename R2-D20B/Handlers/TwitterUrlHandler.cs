using System.Text;
using System.Text.Json;
using DSharpPlus.EventArgs;

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
      public string? Url { get; set; }
      public string? Id { get; set; }
      public string? Text { get; set; }
      public MediaData? Media { get; set; }
    }


    internal class MediaData
    {
      // public ApiPhotoData[]? Photos { get; set; }
      // public ApiVideoData[]? Videos { get; set; }
      public ApiGenericMediaData[]? Photos { get; set; }
      public ApiGenericMediaData[]? Videos { get; set; }
    }


    internal class ApiGenericMediaData
    {
      public string? Url { get; set; }
    }


    // internal class ApiPhotoData
    // {
    //   public string? Url { get; set; }
    // }


    // internal class ApiVideoData
    // {
    //   public string? Url { get; set; }
    // }

    
    private readonly HttpClient m_HttpClient;


    public TwitterUrlHandler(HttpClient httpClient)
    {
      m_HttpClient = httpClient;
    }

    
    public async Task HandleAsync(List<UrlInfo> urlInfos, MessageCreatedEventArgs e)
    {
      var relevantUrlInfos = urlInfos.Where(CanHandle);

      if (relevantUrlInfos.Count() <= 0) return;

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

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<FixTweetResponseData>(json);

        if (data?.Code != 200 || data.Tweet is null) continue;

        var tweet = data.Tweet;

      }

      var replySb = new StringBuilder();

      foreach (var urlInfo in relevantUrlInfos)
      {
        var newUrl = urlInfo.ReplaceHost("vxtwitter.com");
        replySb.AppendLine(newUrl);
      }

      await e.Message.ModifyEmbedSuppressionAsync(true);
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


    private bool IsTweet(UrlInfo urlInfo)
    {
      return urlInfo.Uri.AbsolutePath.Contains("/status/", StringComparison.OrdinalIgnoreCase);
    }


    private async Task HandleMediaAsync(ApiGenericMediaData[] mediaData, MessageCreatedEventArgs e)
    {
      if (mediaData.Length <= 0) return;

      var replySb = new StringBuilder();

      foreach (var mediaObject in mediaData)
        replySb.AppendLine(mediaObject.Url);
      
      await e.Message.RespondAsync(replySb.ToString());
    }


    // private async Task HandlePhotosAsync(TweetData tweet, MessageCreatedEventArgs e)
    // {
    //   if (tweet.Media?.Photos is null) return;
    //   if (tweet.Media.Photos.Length <= 0) return; // This probably can't happen after the above

    //   var replySb = new StringBuilder();

    //   foreach (var imageDatum in tweet.Media.Photos)
    //    replySb.AppendLine(imageDatum.Url);
      
    //   await e.Message.RespondAsync(replySb.ToString());
    // }


    // private async Task HandleVideosAsync(TweetData tweet, MessageCreatedEventArgs e)
    // {
    //   if (tweet.Media?.Videos is null) return;
    //   if (tweet.Media.Videos.Length <= 0) return; // This probably can't happen

    //   var replySb = new StringBuilder();

    //   foreach (var videoDatum in tweet.Media.Videos)
    //     replySb.AppendLine(videoDatum.Url);
      
    //   await e.Message.RespondAsync(replySb.ToString());
    // }
  }
}
