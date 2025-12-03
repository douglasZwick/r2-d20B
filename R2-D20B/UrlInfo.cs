namespace R2D20B
{
  internal class UrlInfo
  {
    public string m_OriginalString;
    public string m_NormalizedString;

    private readonly Uri? m_Uri;

    public Uri Uri => m_Uri!;
    public string Scheme => Uri.Scheme;
    public string Host => Uri.Host!;
    public string PathAndQuery => Uri.PathAndQuery!;
    public string Fragment => Uri.Fragment!;
    public bool IsValid { get => m_Uri is not null; }


    public UrlInfo(string url)
    {
      m_OriginalString = url;

      // TODO:
      //   Do a more robust job of detecting a scheme so I can preserve
      //   more of them than just these two
      if (!(url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
        url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)))
        url = "https://" + url;
      
      m_NormalizedString = url;
      
      Uri.TryCreate(url, UriKind.Absolute, out m_Uri);
    }


    public string ReplaceHost(string newHost)
    {
      return Scheme + "://" + newHost + PathAndQuery + Fragment;
    }
  }
}
