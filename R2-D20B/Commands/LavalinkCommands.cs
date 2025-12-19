using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using Lavalink4NET;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace R2D20B.Commands;


internal class LavalinkCommands(
  IAudioService audioService,
  ILogger<LavalinkCommands> logger)
{
  private static readonly string s_ExampleUrl = "https://www.youtube.com/watch?v=9FLRHejWAo8";
  private static readonly string s_ExampleQuery = "reverb fart";

  private IAudioService AudioService { get; init; } = audioService;
  private ILogger<LavalinkCommands> Logger { get; init; } = logger;

  
  private class Guards
  {
    public static async ValueTask<bool> RequireGuildAsync(CommandContext ctx)
    {
      if (ctx.Guild is null)
      {
        var errorResponse = new DiscordFollowupMessageBuilder()
          .WithContent("[ Zeep. ] This command only works in a Discord server. [ Morp. ]")
          .AsEphemeral();
        
        await ctx.EditResponseAsync(errorResponse).ConfigureAwait(false);
        return false;
      }

      return true;
    }


    public static async ValueTask<bool> RequireUrlOrQueryAsync(
      CommandContext ctx, string? audioString, TrackSearchMode searchMode)
    {
      if (string.IsNullOrWhiteSpace(audioString))
      {
        var errorMessage = searchMode == TrackSearchMode.None
          ? "This command expects a YouTube URL as an argument, "
            + $"like this: `!play {s_ExampleUrl}`"
          : "This command expects a YouTube search query as an argument, "
            + $"like this: `!play {s_ExampleQuery}`";
        errorMessage = $"[ Zeep. ] {errorMessage} [ Morp. ]";
        var errorResponse = new DiscordFollowupMessageBuilder()
          .WithContent(errorMessage)
          .AsEphemeral();
        
        await ctx.EditResponseAsync(errorResponse).ConfigureAwait(false);

        return false;
      }

      return true;
    }


    public static async ValueTask<bool> RequirePlayerAsync(
      CommandContext ctx, PlayerRetrieveResult result)
    {
      if (!result.IsSuccess)
      {
        var errorMessage = result.Status switch
        {
          PlayerRetrieveStatus.UserNotInVoiceChannel =>
            "[ Zeep. ] User not in voice channel. [ Morp. ]",
          PlayerRetrieveStatus.BotNotConnected =>
            "[ Zeep. ] I'm not currently connected. [ Morp. ]",
          _ => $"[ Zeep. ] Unknown error. Result status: {result.Status} [ Morp. ]",
        };

        var errorResponse = new DiscordFollowupMessageBuilder()
          .WithContent(errorMessage)
          .AsEphemeral();
        
        await ctx.EditResponseAsync(errorResponse).ConfigureAwait(false);

        return false;
      }

      return true;
    }
  }


  private class PlayerRetrieveResult
  {
    [MemberNotNullWhen(true, nameof(Player))]
    public bool IsSuccess { get; init; }
    public PlayerRetrieveStatus? Status { get; init; }
    public QueuedLavalinkPlayer? Player { get; init; }


    public PlayerRetrieveResult(
      bool isSuccess,
      PlayerRetrieveStatus? status,
      QueuedLavalinkPlayer? player)
    {
      IsSuccess = isSuccess;
      Status = status;
      Player = player;

      if (isSuccess && player is null) throw new InvalidOperationException(
        $"GetPlayerResult with IsSuccess: {isSuccess} and Status: {status} has null Player.");
    }
  }


  [Command("join")]
  [Description("Joins the voice channel you're in.")]
  public async ValueTask Join(CommandContext ctx)
  {
    await SetupHelper(ctx);
    if (!await Guards.RequireGuildAsync(ctx)) return;
    if (ctx.Guild is not DiscordGuild guild)
      throw new InvalidOperationException(
        $"Expected {ctx.GetType().Name}.{nameof(ctx.Guild)} not to be null, but it was.");

    var result = await RetrievePlayerAsync(
      ctx, guild, connectToVoiceChannel: true, requireUserInVoice: true);

    if (!await Guards.RequirePlayerAsync(ctx, result)) return;

    await ctx.EditResponseAsync(new DiscordFollowupMessageBuilder()
      .WithContent($"[ Boop. ] Okay, I've joined you. [ Meep. ]")).ConfigureAwait(false);
  }


  [Command("leave")]
  [Description("Makes me leave the voice channel I'm in.")]
  public async ValueTask Leave(CommandContext ctx)
  {
    await SetupHelper(ctx);
    if (!await Guards.RequireGuildAsync(ctx)) return;
    if (ctx.Guild is not DiscordGuild guild)
      throw new InvalidOperationException(
        $"Expected {ctx.GetType().Name}.{nameof(ctx.Guild)} not to be null, but it was.");

    var result = await RetrievePlayerAsync(
      ctx, guild, connectToVoiceChannel: false, requireUserInVoice: false);

    if (!await Guards.RequirePlayerAsync(ctx, result)) return;
    if (result.Player is null) throw new InvalidOperationException(
      $"Expected {result.GetType().Name}.{nameof(result.Player)} not to be null, but it was. "
        + $"Result status: {result.Status}");

    await result.Player.DisposeAsync();
    await ctx.EditResponseAsync(new DiscordFollowupMessageBuilder()
      .WithContent($"[ Boop. ] Okay, I've left. [ Meep. ]")).ConfigureAwait(false);
  }


  [Command("play")]
  [Description("Makes me play the audio from a YouTube video in voice.")]
  public async ValueTask PlayYouTubeUrl(CommandContext ctx,
    [Description("The URL of the video to play.")]
    [Parameter("url")]
    string? url)
  {
    await SetupHelper(ctx);
    var searchMode = TrackSearchMode.None;

    if (!await Guards.RequireUrlOrQueryAsync(ctx, url, searchMode)) return;
    if (url is null) throw new InvalidOperationException(
      $"Expected URL not to be null, but it was.");
    if (!await Guards.RequireGuildAsync(ctx)) return;
    if (ctx.Guild is not DiscordGuild guild)
      throw new InvalidOperationException(
        $"Expected {ctx.GetType().Name}.{nameof(ctx.Guild)} not to be null, but it was.");

    var result = await RetrievePlayerAsync(
      ctx, guild, connectToVoiceChannel: true, requireUserInVoice: true);

    if (!await Guards.RequirePlayerAsync(ctx, result)) return;
    if (result.Player is null) throw new InvalidOperationException(
      $"Expected {result.GetType().Name}.{nameof(result.Player)} not to be null, but it was. "
        + $"Result status: {result.Status}");
    
    await PlayHelper(ctx, url, searchMode, result);
  }


  [Command("playyoutubesearch")]
  [TextAlias("playytsearch")]
  [Description("Makes me play audio from a YouTube search.")]
  public async ValueTask PlayFromYouTubeQuery(CommandContext ctx,
    [Description("The query string to use the video to play.")]
    [Parameter("query")][RemainingText]
    string query = "")
  {
    await SetupHelper(ctx);
    var searchMode = TrackSearchMode.YouTube;

    if (!await Guards.RequireUrlOrQueryAsync(ctx, query, searchMode)) return;
    if (!await Guards.RequireGuildAsync(ctx)) return;
    if (ctx.Guild is not DiscordGuild guild)
      throw new InvalidOperationException(
        $"Expected {ctx.GetType().Name}.{nameof(ctx.Guild)} not to be null, but it was.");

    var result = await RetrievePlayerAsync(
      ctx, guild, connectToVoiceChannel: true, requireUserInVoice: true);

    if (!await Guards.RequirePlayerAsync(ctx, result)) return;
    if (result.Player is null) throw new InvalidOperationException(
      $"Expected {result.GetType().Name}.{nameof(result.Player)} not to be null, but it was. "
        + $"Result status: {result.Status}");
    
    await PlayHelper(ctx, query, searchMode, result);
  }


  [Command("stop")]
  [Description("Makes me stop playing whatever I'm currently playing.")]
  public async ValueTask Stop(CommandContext ctx)
  {
    await SetupHelper(ctx);

    if (!await Guards.RequireGuildAsync(ctx)) return;
    if (ctx.Guild is not DiscordGuild guild)
      throw new InvalidOperationException(
        $"Expected {ctx.GetType().Name}.{nameof(ctx.Guild)} not to be null, but it was.");

    var result = await RetrievePlayerAsync(
      ctx, guild, connectToVoiceChannel: false, requireUserInVoice: false);
    
    if (!await Guards.RequirePlayerAsync(ctx, result)) return;
    if (result.Player is null) throw new InvalidOperationException(
      $"Expected {result.GetType().Name}.{nameof(result.Player)} not to be null, but it was. "
        + $"Result status: {result.Status}");
    
    await StopHelper(ctx, result);
  }


  private static async ValueTask SetupHelper(CommandContext ctx)
  {
    await ctx.DeferResponseAsync().ConfigureAwait(false);
  }


  private async ValueTask PlayHelper(
    CommandContext ctx, string audioString,
    TrackSearchMode searchMode, PlayerRetrieveResult result)
  {
    if (result.Player is null) throw new InvalidOperationException(
      $"Expected {result.GetType().Name}.{nameof(result.Player)} not to be null, but it was. "
        + $"Result status: {result.Status}");

    var track = await AudioService.Tracks
      .LoadTrackAsync(audioString, searchMode)
      .ConfigureAwait(false);
    
    if (track is null)
    {
      var errorMessage = searchMode == TrackSearchMode.None
        ? $"[ Zeep. ] I couldn't find a video with URL {audioString}. [ Morp. ]"
        : $"[ Zeep. ] No search results for `{audioString}`. [ Morp. ]";
      var errorResponse = new DiscordFollowupMessageBuilder()
        .WithContent(errorMessage)
        .AsEphemeral();
      
      await ctx.EditResponseAsync(errorResponse).ConfigureAwait(false);

      return;
    }

    var position = await result.Player.PlayAsync(track).ConfigureAwait(false);
    var name = track.Uri?.ToString();
    var successMessage = position is 0
      ? $"[ Beep. ] Added to queue: {name} [ Boop. ]"
      : $"[ Beep. ] Now playing: {name} [ Boop. ]";

    await ctx.EditResponseAsync(new DiscordFollowupMessageBuilder()
      .WithContent(successMessage)).ConfigureAwait(false);
  }


  private async ValueTask StopHelper(CommandContext ctx, PlayerRetrieveResult result)
  {
    if (result.Player is null) throw new InvalidOperationException(
      $"Expected {result.GetType().Name}.{nameof(result.Player)} not to be null, but it was. "
        + $"Result status: {result.Status}");
    
    await result.Player.StopAsync();
    await ctx.EditResponseAsync(new DiscordFollowupMessageBuilder()
      .WithContent("[ Borp. ] Audio stopped. [ Boople. ]")).ConfigureAwait(false);
  }


  private async ValueTask<PlayerRetrieveResult> RetrievePlayerAsync(
    CommandContext ctx,
    DiscordGuild guild,
    bool connectToVoiceChannel = true,
    bool requireUserInVoice = true)
  {
    ArgumentNullException.ThrowIfNull(ctx);

    var retrieveOptions = new PlayerRetrieveOptions(ChannelBehavior:
      connectToVoiceChannel ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None);
    var playerOptions = new QueuedLavalinkPlayerOptions { HistoryCapacity = 10000, };

    var channelId = requireUserInVoice ? ctx.Member?.VoiceState?.ChannelId : null;
    var result = await AudioService.Players
      .RetrieveAsync(guild.Id, channelId,
        playerFactory: PlayerFactory.Queued, Options.Create(playerOptions), retrieveOptions)
      .ConfigureAwait(false);
    
    return new PlayerRetrieveResult(
      isSuccess: result.IsSuccess,
      status: result.Status,
      player: result.Player);
  }
}
