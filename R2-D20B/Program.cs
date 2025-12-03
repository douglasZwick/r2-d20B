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

      builder.Services.AddSingleton<IUrlHandler, TwitterUrlHandler>()
        .AddHttpClient()
        .AddHostedService<Bot>();

      var host = builder.Build();
      await host.RunAsync();
    }
  }
}
