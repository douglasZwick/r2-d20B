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
using Lavalink4NET.Tracks;
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


  private enum ErrorStatus
  {
    Success,
    NoUrlReceived,
    NoYouTubeQueryReceived,
    YouTubeUrlNotFound,
    NoYouTubeSearchResults,
    PlayerRetrievalFailure,

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
    // Should I have this? The sample code has it
    await ctx.DeferResponseAsync().ConfigureAwait(false);

    if (ctx.Guild is null)
    {
      var errorResponse = new DiscordFollowupMessageBuilder()
        .WithContent("[ Zeep. ] This command only works in a Discord server. [ Morp. ]")
        .AsEphemeral();
      
      await ctx.EditResponseAsync(errorResponse).ConfigureAwait(false);
      return;
    }

    var result = await RetrievePlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);
    if (result is null)
    {
      var errorResponse = new DiscordFollowupMessageBuilder()
        .WithContent("[ Zeep. ] GetPlayerAsync result null. [ Morp. ]")
        .AsEphemeral();
      
      await ctx.EditResponseAsync(errorResponse).ConfigureAwait(false);
      return;
    }

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
      return;
    }

    await ctx.EditResponseAsync(new DiscordFollowupMessageBuilder()
      .WithContent($"[ Boop. ] Okay, I've joined you. [ Meep. ]")).ConfigureAwait(false);
  }


  [Command("leave")]
  [Description("Makes me leave the voice channel I'm in.")]
  public async ValueTask Leave(CommandContext ctx)
  {
    // Should I have this? The sample code has it
    await ctx.DeferResponseAsync().ConfigureAwait(false);

    if (ctx.Guild is null)
    {
      var errorResponse = new DiscordFollowupMessageBuilder()
        .WithContent("[ Zeep. ] This command only works in a Discord server. [ Morp. ]")
        .AsEphemeral();
      
      await ctx.EditResponseAsync(errorResponse).ConfigureAwait(false);
      return;
    }

    var result = await RetrievePlayerAsync(ctx, connectToVoiceChannel: false).ConfigureAwait(false);
    if (result is null)
    {
      var errorResponse = new DiscordFollowupMessageBuilder()
        .WithContent("[ Zeep. ] GetPlayerAsync result null. [ Morp. ]")
        .AsEphemeral();
      
      await ctx.EditResponseAsync(errorResponse).ConfigureAwait(false);
      return;
    }

    if (!result.IsSuccess)
    {
      var errorMessage = result.Status switch
      {
        PlayerRetrieveStatus.BotNotConnected =>
          "[ Zeep. ] I'm not currently connected. [ Morp. ]",
        _ => $"[ Zeep. ] Unknown error. Result status: {result.Status} [ Morp. ]",
      };

      var errorResponse = new DiscordFollowupMessageBuilder()
        .WithContent(errorMessage)
        .AsEphemeral();
      
      await ctx.EditResponseAsync(errorResponse).ConfigureAwait(false);
      return;
    }

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
    // Should I have this? The sample code has it
    await ctx.DeferResponseAsync().ConfigureAwait(false);

    if (string.IsNullOrWhiteSpace(url)) // TODO: Maybe also validate URLs with a Uri or something
    {
      var errorResponse = new DiscordFollowupMessageBuilder()
        .WithContent("[ Zeep. ] This command expects a YouTube URL as an argument, "
          + $"like this: `!play {s_ExampleUrl}` [ Morp. ]")
        .AsEphemeral();
      
      await ctx.EditResponseAsync(errorResponse).ConfigureAwait(false);
      return;
    }

    if (ctx.Guild is null)
    {
      var errorResponse = new DiscordFollowupMessageBuilder()
        .WithContent("[ Zeep. ] This command only works in a Discord server. [ Morp. ]")
        .AsEphemeral();
      
      await ctx.EditResponseAsync(errorResponse).ConfigureAwait(false);
      return;
    }

    var result = await RetrievePlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);
    if (result is null)
    {
      var errorResponse = new DiscordFollowupMessageBuilder()
        .WithContent("[ Zeep. ] GetPlayerAsync result null. [ Morp. ]")
        .AsEphemeral();
      
      await ctx.EditResponseAsync(errorResponse).ConfigureAwait(false);
      return;
    }

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
      return;
    }

    var track = await AudioService.Tracks
      .LoadTrackAsync(url, TrackSearchMode.None)
      .ConfigureAwait(false);
    
    // I guess this might happen if the URL is bad
    if (track is null)
    {
      var errorResponse = new DiscordFollowupMessageBuilder()
        .WithContent("[ Zeep. ] I couldn't find that video. [ Morp. ]")
        .AsEphemeral();
      
      await ctx.EditResponseAsync(errorResponse).ConfigureAwait(false);

      return;
    }

    var position = await result.Player.PlayAsync(track).ConfigureAwait(false);
    var name = track.SourceName ?? track.Uri?.ToString();

    if (position is 0)
    {
      await ctx.EditResponseAsync(new DiscordFollowupMessageBuilder()
        .WithContent($"[ Beep. ] Added to queue: {name} [ Boop. ]")).ConfigureAwait(false);
    }
    else
    {
      await ctx.EditResponseAsync(new DiscordFollowupMessageBuilder()
        .WithContent($"[ Beep. ] Now playing: {name} [ Boop. ]")).ConfigureAwait(false);
    }
  }


  [Command("playyoutubesearch")]
  [TextAlias("playytsearch")]
  [Description("Makes me play audio from a YouTube search.")]
  public async ValueTask PlayFromYouTubeQuery(CommandContext ctx,
    [Description("The query string to use the video to play.")]
    [Parameter("query")][RemainingText]
    string query = "")
  {
    // Should I have this? The sample code has it
    await ctx.DeferResponseAsync().ConfigureAwait(false);

    if (string.IsNullOrWhiteSpace(query)) // TODO: Maybe also validate URLs with a Uri or something
    {
      var errorResponse = new DiscordFollowupMessageBuilder()
        .WithContent("[ Zeep. ] This command expects a YouTube search query as an argument, "
          + $"like this: `!play {s_ExampleQuery}` [ Morp. ]")
        .AsEphemeral();
      
      await ctx.EditResponseAsync(errorResponse).ConfigureAwait(false);
      return;
    }

    if (ctx.Guild is null)
    {
      var errorResponse = new DiscordFollowupMessageBuilder()
        .WithContent("[ Zeep. ] This command only works in a Discord server. [ Morp. ]")
        .AsEphemeral();
      
      await ctx.EditResponseAsync(errorResponse).ConfigureAwait(false);
      return;
    }

    var result = await RetrievePlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);

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
      return;
    }

    var track = await AudioService.Tracks
      .LoadTrackAsync(query, TrackSearchMode.YouTube)
      .ConfigureAwait(false);
    
    if (track is null)
    {
      var errorResponse = new DiscordFollowupMessageBuilder()
        .WithContent($"[ Zeep. ] No search results for `{query}`. [ Morp. ]")
        .AsEphemeral();
      
      await ctx.EditResponseAsync(errorResponse).ConfigureAwait(false);

      return;
    }

    var position = await result.Player.PlayAsync(track).ConfigureAwait(false);
    var name = track.SourceName ?? track.Uri?.ToString();

    if (position is 0)
    {
      await ctx.EditResponseAsync(new DiscordFollowupMessageBuilder()
        .WithContent($"[ Beep. ] Added to queue: {name} [ Boop. ]")).ConfigureAwait(false);
    }
    else
    {
      await ctx.EditResponseAsync(new DiscordFollowupMessageBuilder()
        .WithContent($"[ Beep. ] Now playing: {name} [ Boop. ]")).ConfigureAwait(false);
    }
  }


  private async ValueTask SetupHelper(CommandContext ctx)
  {
    await ctx.DeferResponseAsync().ConfigureAwait(false);
  }


  private async ValueTask ErrorResponseHelper(CommandContext ctx)
  {
    if (ctx.Guild is null)
    {
      var errorResponse = new DiscordFollowupMessageBuilder()
        .WithContent("[ Zeep. ] This command only works in a Discord server. [ Morp. ]")
        .AsEphemeral();
      
      await ctx.EditResponseAsync(errorResponse).ConfigureAwait(false);
      return;
    }

    var result = await RetrievePlayerAsync(ctx, connectToVoiceChannel: true).ConfigureAwait(false);
    if (result is null)
    {
      var errorResponse = new DiscordFollowupMessageBuilder()
        .WithContent("[ Zeep. ] GetPlayerAsync result null. [ Morp. ]")
        .AsEphemeral();
      
      await ctx.EditResponseAsync(errorResponse).ConfigureAwait(false);
      return;
    }
  }


  private async ValueTask PlayHelper(
    CommandContext ctx, string audioString,
    TrackSearchMode searchMode, PlayerRetrieveResult result)
  {
    var track = await AudioService.Tracks
      .LoadTrackAsync(audioString, searchMode)
      .ConfigureAwait(false);
    
    if (track is null)
    {
      var errorStatus = searchMode == TrackSearchMode.None
        ? ErrorStatus.YouTubeUrlNotFound : ErrorStatus.NoYouTubeSearchResults;
      var errorMessage = string.Format(GetStatusMessage(errorStatus), audioString);
      var errorResponse = new DiscordFollowupMessageBuilder()
        .WithContent(errorMessage)
        .AsEphemeral();
      
      await ctx.EditResponseAsync(errorResponse).ConfigureAwait(false);

      return;
    }

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
      return;
    }

    var position = await result.Player.PlayAsync(track).ConfigureAwait(false);
    var name = track.Uri?.ToString();
    var successMessage = position is 0
      ? $"[ Beep. ] Added to queue: {name} [ Boop. ]" : $"[ Beep. ] Now playing: {name} [ Boop. ]";

    await ctx.EditResponseAsync(new DiscordFollowupMessageBuilder()
      .WithContent(successMessage)).ConfigureAwait(false);
  }


  private async ValueTask<PlayerRetrieveResult> RetrievePlayerAsync
    (CommandContext ctx, bool connectToVoiceChannel = true)
  {
    ArgumentNullException.ThrowIfNull(ctx);

    var retrieveOptions = new PlayerRetrieveOptions(ChannelBehavior:
      connectToVoiceChannel ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None);
    var playerOptions = new QueuedLavalinkPlayerOptions { HistoryCapacity = 10000, };

    var result = await AudioService.Players
      .RetrieveAsync(ctx.Guild!.Id, ctx.Member?.VoiceState.ChannelId,
        playerFactory: PlayerFactory.Queued, Options.Create(playerOptions), retrieveOptions)
      .ConfigureAwait(false);
    
    return new PlayerRetrieveResult(
      isSuccess: result.IsSuccess,
      status: result.Status,
      player: result.Player);
  }


  private static string GetStatusMessage(ErrorStatus status)
  {
    return status switch
    {
      ErrorStatus.NoUrlReceived =>
        "[ Zeep. ] This command expects a YouTube URL as an argument, "
          + $"like this: `!play {s_ExampleUrl}` [ Morp. ]",
      ErrorStatus.NoYouTubeQueryReceived =>
        "[ Zeep. ] This command expects a YouTube search query as an argument, "
          + $"like this: `!play {s_ExampleQuery}` [ Morp. ]",
      ErrorStatus.YouTubeUrlNotFound =>
        "[ Zeep. ] I couldn't find a video with URL {0}. [ Morp. ]",
      ErrorStatus.NoYouTubeSearchResults =>
        "[ Zeep. ] No search results for `{0}`. [ Morp. ]",

      _ => $"[ Zeep. ] Unknown error. Error status: {status} [ Morp. ]",
    };
  }


  private static string GetPlayerRetrieveErrorMessage(PlayerRetrieveStatus status)
  {
    return status switch
    {
      PlayerRetrieveStatus.UserNotInVoiceChannel =>
        "[ Zeep. ] User not in voice channel. [ Morp. ]",
      PlayerRetrieveStatus.BotNotConnected =>
        "[ Zeep. ] I'm not currently connected. [ Morp. ]",

      _ => $"[ Zeep. ] Unknown error. Result status: {status} [ Morp. ]",
    };
  }
}
