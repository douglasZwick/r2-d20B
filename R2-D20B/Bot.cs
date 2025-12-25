using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Hosting;


namespace R2D20B;


internal class Bot(
  DiscordClient client,
  SoundCatalog soundCatalog,
  UptimeService uptimeService) : IHostedService
{
  private DiscordClient Client { get; init; } = client;
  private readonly SoundCatalog _sc = soundCatalog;
  private readonly UptimeService _us = uptimeService;


  public Task StartAsync(CancellationToken cancellationToken)
  {
    var status = new DiscordActivity("Ligma", DiscordActivityType.Playing);
    return Client.ConnectAsync(status, DiscordUserStatus.Online);
  }


  public Task StopAsync(CancellationToken cancellationToken)
  {
    return Client.DisconnectAsync();
  }
}
