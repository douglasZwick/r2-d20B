using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;
using Lavalink4NET;
using Lavalink4NET.Extensions;
using Microsoft.Extensions.Logging;
// using Lavalink4NET.Players;


namespace R2D20B.Commands;


internal class LavalinkCommands(
  IAudioService audioService,
  ILogger<LavalinkCommands> logger)
{
  private IAudioService AudioService { get; init; } = audioService;
  private ILogger<LavalinkCommands> Logger { get; init; } = logger;


  [Command("join")]
  [Description("Joins the voice channel you're in.")]
  public async ValueTask Join(CommandContext ctx)
  {
    if (ctx.Member is not DiscordMember member || ctx.Guild is not DiscordGuild guild)
    {
      await ctx.RespondAsync("[ Beep boop. ] This only works in a Discord server. [ Borp. ]");
      return;
    }

    if (member.VoiceState is not DiscordVoiceState voiceState || voiceState.ChannelId is null)
    {
      await ctx.RespondAsync("[ Beep boop. ] I can't join you if you aren't in voice. [ Borp. ]");
      return;
    }

    var channel = await voiceState.GetChannelAsync();
    if (channel?.Type != DiscordChannelType.Voice)
    {
      await ctx.RespondAsync("[ Beep boop. ] I don't know how you're in a voice channel that's not a voice channel, but I can't work with that right now. [ Borp. ]");
      return;
    }

    await ctx.RespondAsync(
      $"[ Boop . ] Okay, I'll join you in {channel.Name}. [ Meep. ]");
    await AudioService.Players.JoinAsync(guild.Id, channel.Id);
    await ctx.RespondAsync(
      $"[ Boop . ] Joined. [ Meep. ]");
  }


  [Command("leave")]
  [Description("Makes me leave the voice channel I'm in.")]
  public async ValueTask Leave(CommandContext ctx)
  {
    if (ctx.Member is null || ctx.Guild is not DiscordGuild guild)
    {
      await ctx.RespondAsync("[ Beep boop. ] This only works in a Discord server. [ Borp. ]");
      return;
    }
    
    var player = await AudioService.Players.GetPlayerAsync(guild.Id);

    if (player is null)
    {
      await ctx.RespondAsync("[ Beep boop. ] I'm not connected to a voice channel right now. [ Borp. ]");
      return;
    }

    await ctx.RespondAsync("[ Meep zorp. ] Okay, I'll leave. [ Boop. ]");
    await player.DisposeAsync();
    await ctx.RespondAsync("[ Meep zorp. ] Left. [ Boop. ]");
  }
}
