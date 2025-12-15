namespace R2D20B.Tests;

public class UrlInfoTests
{
  [Fact]
  public void UrlInfo_Recognizes_A_Basic_Https_Url_As_Valid()
  {
    var info = new UrlInfo("http://example.com/");

    Assert.True(info.IsValid);
  }


  [Fact(Skip = "TODO: Trailing punctuation not yet handled")]
  public void UrlInfo_Handles_Trailing_Punctuation()
  {
    var info = new UrlInfo("https://example.com,");

    Assert.Equal("https://example.com", info.m_NormalizedString);
    Assert.True(info.IsValid);
  }
}
