using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Hosting;


namespace R2D20B;


internal class Bot(
  DiscordClient client,
  IHostEnvironment hostEnvironment,
  SoundCatalog soundCatalog,
  UptimeService uptimeService) : IHostedService
{
  private DiscordClient Client { get; } = client;
  private IHostEnvironment HostEnvironment { get; } = hostEnvironment;
  private readonly SoundCatalog _sc = soundCatalog;
  private readonly UptimeService _us = uptimeService;


  public Task StartAsync(CancellationToken cancellationToken)
  {
    var statusStr = HostEnvironment.IsDevelopment() ? "Dev" : "Ligma";
    var status = new DiscordActivity(statusStr, DiscordActivityType.Playing);
    return Client.ConnectAsync(status, DiscordUserStatus.Online);
  }


  public Task StopAsync(CancellationToken cancellationToken)
  {
    return Client.DisconnectAsync();
  }
}
