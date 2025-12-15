namespace R2D20B.Tests;

public class UrlInfoTests
{
  [Fact]
  public void UrlInfo_Recognizes_A_Basic_Https_Url_As_Valid()
  {
    var info = new UrlInfo("http://example.com/");

    Assert.True(info.IsValid);
  }
}
