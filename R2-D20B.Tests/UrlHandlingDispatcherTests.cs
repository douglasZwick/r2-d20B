using System.Net.Http;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging.Abstractions;
using R2D20B;
using R2D20B.Handlers;
using Xunit;


namespace R2D20B.Tests;


public sealed class UrlHandlingDispatcherTests
{
  private sealed class CountingHandler : IUrlHandler
  {
    public int CallCount { get; private set; }

    
    public Task HandleAsync(List<UrlInfo> urlInfos, MessageCreatedEventArgs e)
    {
      ++CallCount;

      return Task.CompletedTask;
    }


    public bool CanHandle(UrlInfo urlInfo) => true;
  }


  // [Fact]
  // public async Test HandleMessageCreatedAsync_Calls_Handlers_When_Url_Is_Present()
  // {
  //   // Arrange
  //   var handler = new CountingHandler();
  //   var dispatcher = new UrlHandlingDispatcher(
  //     [handler],
  //     new HttpClient(),
  //     NullLogger<UrlHandlingDispatcher>.Instance);
  //   var e = TestEventArgs.MessageCreated("Check this URL: https://example.com/");

  //   // Act
  //   await dispatcher.HandleMessageCreatedAsync(client: null, e);

  //   // Assert
  //   Assert.Equal(1, handler.CallCount);
  // }
}
