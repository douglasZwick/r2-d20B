using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using R2D20B.Handlers;

namespace R2D20B
{
  internal class Program
  {
    static async Task Main(string[] args)
    {
      var builder = Host.CreateApplicationBuilder(args);

      builder.Services.AddSingleton(sp =>
      {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
          "R2-D20B/0.1 (+https://github.com/douglasZwick/r2-d20B)"
        );
        return httpClient;
      });
      
      builder.Services.AddSingleton<IUrlHandler, TwitterUrlHandler>();
      builder.Services.AddSingleton<IUrlHandler, InstagramUrlHandler>();
      builder.Services.AddHostedService<Bot>();
      
      var host = builder.Build();
      await host.RunAsync();
    }
  }
}
