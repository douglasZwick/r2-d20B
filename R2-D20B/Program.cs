using DSharpPlus;
using DSharpPlus.Commands;
using Lavalink4NET;
using Lavalink4NET.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using R2D20B.Commands;
using R2D20B.Handlers;


namespace R2D20B
{
  internal class Program
  {
    static async Task Main(string[] args)
    {
      var builder = Host.CreateApplicationBuilder(args);

      builder.Services.AddSingleton(sp =>
        CreateDiscordClient(sp, sp.GetRequiredService<HttpClient>()));
      
      builder.Services.AddSingleton(sp =>
      {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
          "R2-D20B/0.1 (+https://github.com/douglasZwick/r2-d20B)"
        );
        return httpClient;
      });
      
      builder.Services.AddHostedService<Bot>();
      
      builder.Services.ConfigureLavalink(config =>
      {
        var section = builder.Configuration.GetRequiredSection("Lavalink");

        static string Require(IConfigurationSection s, string key) =>
          s[key] ?? throw new InvalidOperationException(
            $"Lavalink setting '{key}' is missing from appsettings.json.");

        config.BaseAddress = new Uri(Require(section, "BaseAddress"));
        config.Passphrase = Require(section, "Passphrase");
        config.ReadyTimeout = TimeSpan.FromSeconds(30);
      });
      builder.Services.AddLavalink();

      builder.Services.AddSingleton<GatewayEventHandlers>();
      builder.Services.AddSingleton<UrlHandlingDispatcher>();
      builder.Services.AddSingleton<IUrlHandler, TwitterUrlHandler>();
      builder.Services.AddSingleton<IUrlHandler, InstagramUrlHandler>();
      builder.Services.AddSingleton<CommandRegistry>();

      var host = builder.Build();
      await host.RunAsync();
    }


    internal static DiscordClient CreateDiscordClient(
      IServiceProvider services,
      HttpClient httpClient)
    {
      var token = BotConfig.GetToken();

      return DiscordClientBuilder
        .CreateDefault(token, DiscordIntents.All)
        .ConfigureServices(s =>
        {
          s.AddSingleton(httpClient);
          s.AddSingleton(_ => services.GetRequiredService<IAudioService>());
          s.AddSingleton(_ => services.GetRequiredService<CommandRegistry>());
          s.AddSingleton(_ => services.GetRequiredService<ILoggerFactory>());
        })
        .UseCommands((s, extension) =>
        {
          services.GetRequiredService<CommandRegistry>().Initialize(extension);
          extension.AddCommands(typeof(BasicCommands).Assembly);
        },
        new CommandsConfiguration
        {
          CommandExecutor = new AsyncCommandExecutor(),
        })
        .ConfigureEventHandlers(eventHandlingBuilder =>
        {
          var handlers = services.GetRequiredService<GatewayEventHandlers>();

          eventHandlingBuilder
          .HandleMessageCreated(handlers.OnMessageCreated)
          .HandleGuildDownloadCompleted(handlers.OnGuildDownloadCompleted);
        })
        .Build();
    }
  }
}
