using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Extensions;
using Lavalink4NET.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using R2D20B.Commands;
using R2D20B.Handlers;


namespace R2D20B;


internal class Program
{
  static async Task Main(string[] args)
  {
    Console.WriteLine("Bootstrapping R2-D20...");

    var builder = Host.CreateApplicationBuilder(args);

    //////////////////// DEPENDENCY INJECTION ////////////////////
    
    //////
    /// 
    /// App Services / Settings
    /// 
    //////
    
    builder.Services.AddSingleton(sp =>
    {
      var httpClient = new HttpClient();
      httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
        "R2-D20B/0.1 (+https://github.com/douglasZwick/r2-d20B)"
      );
      return httpClient;
    });
    builder.Services.AddSingleton<GatewayEventHandlers>();
    builder.Services.AddSingleton<UrlHandlingDispatcher>();
    builder.Services.AddSingleton<IUrlHandler, TwitterUrlHandler>();
    builder.Services.AddSingleton<IUrlHandler, InstagramUrlHandler>();
    builder.Services.AddSingleton<IUrlHandler, TikTokUrlHandler>();
    builder.Services.AddSingleton<CommandRegistry>();
    builder.Services.AddSingleton<UptimeService>();
    builder.Services.AddSingleton<EmojiCatalog>();
    builder.Services.AddSingleton<BotSettings>();

    //////
    /// 
    /// Discord Client / Commands / Event Connections
    /// 
    //////

    builder.Services.AddDiscordClient(EnvironmentInterface.GetToken(), DiscordIntents.All);
    builder.Services.AddCommandsExtension((sp, commands) =>
    {
      sp.GetRequiredService<CommandRegistry>().Initialize(commands);
      // commands.AddCommands(typeof(BasicCommands).Assembly);
      commands.AddCommands(typeof(BasicCommands));
    });
    builder.Services.ConfigureEventHandlers(b =>
    {
      b.HandleMessageCreated((client, e) =>
        client.ServiceProvider.GetRequiredService<GatewayEventHandlers>()
          .OnMessageCreated(client, e));
      b.HandleGuildDownloadCompleted((client, e) =>
        client.ServiceProvider.GetRequiredService<GatewayEventHandlers>()
          .OnGuildDownloadCompleted(client, e));
    });

    //////
    /// 
    /// Hosted Services (Lavalink, etc.)
    /// 
    //////
    
    // builder.Services.ConfigureLavalink(config =>
    // {
    //   var section = builder.Configuration.GetRequiredSection("Lavalink");

    //   static string Require(IConfigurationSection s, string key) =>
    //     s[key] ?? throw new InvalidOperationException(
    //       $"Lavalink setting '{key}' is missing from appsettings.json.");

    //   config.BaseAddress = new Uri(Require(section, "BaseAddress"));
    //   config.Passphrase = EnvironmentInterface.GetLavalinkServerPassword();
    //   config.ReadyTimeout = TimeSpan.FromSeconds(30);
    // });
    builder.Services.AddOptions<SoundOptions>()
      .Bind(builder.Configuration.GetRequiredSection("Sounds"))
      .Validate(o => !string.IsNullOrWhiteSpace(o.RootPath),
        "'Sounds: RootPath' is missing or empty.");
    builder.Services.PostConfigure<SoundOptions>(o =>
    {
      if (string.IsNullOrWhiteSpace(o.RootPath)) return;

      var baseDir = AppContext.BaseDirectory;
      o.RootPath = Path.GetFullPath(
        Path.IsPathRooted(o.RootPath) ? o.RootPath : Path.Combine(baseDir, o.RootPath));
    });
    builder.Services.AddSingleton<SoundCatalog>();
    // builder.Services.AddLavalink();

    //////
    /// 
    /// The Bot class itself
    /// 
    //////
    
    builder.Services.AddHostedService<Bot>();

    //////////////////// DI COMPLETE ////////////////////
    
    // With all DI setup finished, we can finally build the host and run it.
    var host = builder.Build();
    // TODO: Look into whether I can hook the events via the client somewhere around here,
    //   rather than via the lambda-within-lambda approach above

    // var hostEnvironment = host.Services.GetRequiredService<IHostEnvironment>();
    // Console.WriteLine($"Running in {hostEnvironment.EnvironmentName}...");

    await host.RunAsync();
  }
}
