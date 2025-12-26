using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Processors.TextCommands;
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
using Formatter = DSharpPlus.Formatter;


namespace R2D20B.Commands;


internal class AudioCommands(
  IAudioService audioService,
  SoundCatalog soundCatalog,
  ILogger<AudioCommands> logger)
{
  private static readonly string s_ExampleUrl = "https://www.youtube.com/watch?v=9FLRHejWAo8";
  private static readonly string s_ExampleLocalSoundName = "reverbfart";
  private static readonly string s_ExampleQuery = "reverb fart";
  private static readonly string s_SoundListEmbedTitle =
    "Sound List (Play These with `!play [soundName]`)";
  private static readonly string s_SoundListEmbedDescription =
    "[ Birt. ] Here are the {0} sounds in my Sounds folder: [ Bip. ]";
  private static readonly string s_SoundListEmbedDescriptionPrefixed =
    "[ Birt. ] Here are the {0} sounds in my Sounds folder that start with... [ Bip. ]";
  private static readonly string s_DefaultBucketName = "Digits / Symbols";

  private IAudioService AudioService { get; init; } = audioService;
  private SoundCatalog SoundCatalog { get; init; } = soundCatalog;
  private ILogger<AudioCommands> Logger { get; init; } = logger;

  
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
      CommandContext ctx, string? audioString, TrackSearchMode? searchMode)
    {
      if (string.IsNullOrWhiteSpace(audioString))
      {
        var errorMessage = searchMode is null || searchMode == TrackSearchMode.None
          ? "This command expects a YouTube URL as an argument, "
            + $"like this: `!play {s_ExampleUrl}`, or a local sound name, "
            + $"like this: `!play {s_ExampleLocalSoundName}"
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
  [Description("Makes me play a local file, or audio from a URL, in voice.")]
  public async ValueTask PlayFromUrl(CommandContext ctx,
    [Description("The sound name or URL of the audio to play.")]
    [Parameter("url")][RemainingText]
    string soundNameOrUrl = "")
  {
    await SetupHelper(ctx);

    var userInput = soundNameOrUrl;
    var searchMode = null as TrackSearchMode?;

    var localFilePath = SoundCatalog.TryGetSoundPathByName(soundNameOrUrl);
    if (localFilePath is null)
      searchMode = TrackSearchMode.None;
    else
      soundNameOrUrl = localFilePath.ToString();

    if (!await Guards.RequireUrlOrQueryAsync(ctx, soundNameOrUrl, searchMode)) return;
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
    
    await PlayHelper(ctx, userInput, soundNameOrUrl, searchMode, result);
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
    
    await PlayHelper(ctx, query, query, searchMode, result);
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


  [Command("soundlist")]
  [TextAlias("sounds")]
  [Description("Asks me to recite an alphabetical list of the sounds I can play.")]
  public async ValueTask SoundList(CommandContext ctx,
    [Description("If specified, I'll just show you the sounds that start with this.")]
    [RemainingText]
    string prefix = "")
  {
    prefix = prefix.Trim();

    var prefixSpecified = prefix.Length > 0;

    var soundList = SoundCatalog.GetSortedList();

    if (prefixSpecified)
      soundList = [.. soundList.Where(path => path.StartsWith(prefix,
        StringComparison.OrdinalIgnoreCase))];
    
    if (soundList.Count <= 0)
    {
      var messageStr = prefixSpecified
        ? $"[ Merp. ] I don't have any sounds that start with {Formatter.InlineCode(prefix)}. [ Bink. ]"
        : "[ Merp. ] I don't have any sounds in my Sounds folder right now. [ Bink. ]";
      await ctx.RespondAsync(messageStr);
      return;
    }

    foreach (var message in CreateSoundListMessages(soundList, prefix))
      await ctx.RespondAsync(message);
  }


  private static List<DiscordMessageBuilder> CreateSoundListMessages(
    List<string> sortedSoundNames, string prefix = "")
  {
    const int MAX_CHARS_PER_MESSAGE = 6000;
    const int MAX_EMBEDS_PER_MESSAGE = 10;
    const int MAX_FIELDS_PER_EMBED = 25;
    const int MAX_CHARS_PER_FIELD = 1024;

    /// * INVARIANTS:
    /// * 1. There's always room for the current field in the current embed.
    /// * 2. There's always room for the current embed in the current message.
    
    var messages = new List<DiscordMessageBuilder>();
    var message = new DiscordMessageBuilder();
    var description = string.IsNullOrWhiteSpace(prefix)
      ? s_SoundListEmbedDescription : s_SoundListEmbedDescriptionPrefixed;
    var embed = new DiscordEmbedBuilder()
      .WithTitle(s_SoundListEmbedTitle)
      .WithDescription(string.Format(description, sortedSoundNames.Count()));
    var sb = new StringBuilder();

    var firstChar = sortedSoundNames.First().ToUpperInvariant()[0];
    // A key to identify the current alphabetic bucket. '_' denotes the default bucket.
    var bucketName =
      (firstChar >= 'A' && firstChar <= 'Z') ? firstChar : '_';
    // How many characters we need to increment charCount by whenever we start a new bucket.
    var bucketNameLength = firstChar == '_' ? s_DefaultBucketName.Length : 1;
    // The total characters to be written to the current message. Includes:
    //   - all field values: the strings we add via the string builder. charCount accounts for this
    //     just after a name is added.
    //   - all field names:
    //     - length of s_DefaultBucketName for just the very first field of the first bucket
    //     - 1 for all other fields
    //     charCount accounts for the length of the default bucket name on initialization. For all
    //     subsequent fields, charCount accounts for this just after a field is added to an embed.
    //   - all embed titles: the only one with a title is the very first embed. charCount accounts
    //     for the length of the first embed's title on initialization.
    //   - all embed descriptions: the only one with a description is the very first embed.
    //     charCount accounts for the length of the first embed's description on initialization.
    //   - all the characters currently in the string builder that haven't been put in a field yet.
    var charCount = 0;
    // The total embeds added to the current message.
    var embedCount = 0;
    // The total fields added to the current embed.
    var fieldCount = 0;

    var newBucket = true;

    // Enforces INVARIANT 1
    void FlushField(bool unconditionalFullFlush)
    {
      var fieldName = bucketName == '_' ? s_DefaultBucketName : bucketName.ToString();

      if (!newBucket)
      {
        fieldName = ".";
      }

      newBucket = false;

      embed.AddField(fieldName, sb.ToString());
      ++fieldCount;

      if (unconditionalFullFlush || fieldCount >= MAX_FIELDS_PER_EMBED)
        FlushEmbed(unconditionalFullFlush);
      
      sb.Clear();
      // Increment charCount to account for the field name
      ++charCount;
    }

    // Enforces INVARIANT 2
    void FlushEmbed(bool unconditionalFullFlush)
    {
      message.AddEmbed(embed);
      ++embedCount;

      if (unconditionalFullFlush || embedCount >= MAX_EMBEDS_PER_MESSAGE)
        FlushMessage();

      embed = new();
      fieldCount = 0;
    }

    void FlushMessage()
    {
      messages.Add(message);
      
      message = new();
      embedCount = 0;
      charCount = 0;
    }

    void NewBucket(char bucketKey)
    {
      bucketName = bucketKey;
      bucketNameLength = 1;
      newBucket = true;
    }

    foreach (var soundName in sortedSoundNames)
    {
      firstChar = soundName.ToUpperInvariant()[0];
      var bucketKey = (firstChar >= 'A' && firstChar <= 'Z') ? firstChar : '_';

      if (bucketKey != bucketName)
      {
        // We need to start a new bucket. The first name in a bucket will always start a new field.
        // * INVARIANT 1 guarantees we will always have < MAX_FIELDS fields in the current embed.
        FlushField(unconditionalFullFlush: false);
        NewBucket(bucketKey);
      }

      var formattedName = Formatter.InlineCode(soundName);
      var deltaCount = formattedName.Length + (sb.Length > 0 ? 1 : 0);

      // Check to see if we have room to add formattedName to the current message. We need to make
      // sure that charCount takes into account all the text that could possibly count toward
      // MAX_CHARS_PER_MESSAGE. This is limited to the following:
      // - all field values: the strings we add via the string builder. charCount accounts for this
      //   just after a name is added.
      // - all field names:
      //   - length of s_DefaultBucketName for just the very first field of the first bucket
      //   - 1 for the first field of all other buckets
      //   - 0 for all other fields
      //   charCount accounts for the length of the default bucket name on initialization. For all
      //   subsequent fields, charCount accounts for this just after a field is added to an embed.
      // - all embed titles: the only one with a title is the very first embed. charCount accounts
      //   for the length of the first embed's title on initialization.
      // - all embed descriptions: the only one with a description is the very first embed.
      //   charCount accounts for the length of the first embed's description on initialization.
      // - all the characters currently in the string builder that haven't been put in a field yet.
      if (charCount + deltaCount > MAX_CHARS_PER_MESSAGE)
      {
        // The current message is full. We need to move on to the next message builder.
        FlushField(unconditionalFullFlush: true);
      }

      // Check to see if we have enough room to add formattedName to the current field. The string
      // builder's length is our counter for this.
      if (sb.Length + deltaCount > MAX_CHARS_PER_FIELD)
      {
        // We need to move on to the next field.
        // * INVARIANT 1 guarantees we will always have < MAX_FIELDS fields in the current embed.
        FlushField(unconditionalFullFlush: false);
      }

      if (sb.Length > 0)
      {
        formattedName = " " + formattedName;
        ++deltaCount;
      }

      sb.Append(formattedName);
      charCount += deltaCount;
    }

    // We need to add to the list the message we were working on when the loop ended (if any).
    if (sb.Length > 0)
    {
      FlushField(unconditionalFullFlush: true);
    }

    return messages;
  }


  private static async ValueTask SetupHelper(CommandContext ctx)
  {
    await ctx.DeferResponseAsync().ConfigureAwait(false);
  }


  private async ValueTask PlayHelper(
    CommandContext ctx, string userInput, string audioString,
    TrackSearchMode? searchModeOrNull, PlayerRetrieveResult result)
  {
    if (result.Player is null) throw new InvalidOperationException(
      $"Expected {result.GetType().Name}.{nameof(result.Player)} not to be null, but it was. "
        + $"Result status: {result.Status}");

    LavalinkTrack? track;

    Logger.LogInformation("Requested: {UserInput} | File: {File}", userInput, audioString);

    if (searchModeOrNull is TrackSearchMode searchMode)
    {
      track = await AudioService.Tracks
        .LoadTrackAsync(
          audioString,
          searchMode)
        .ConfigureAwait(false);
    }
    else
    {
      var loadOptions = new TrackLoadOptions(
        SearchMode: TrackSearchMode.None,
        SearchBehavior: StrictSearchBehavior.Passthrough);

      track = await AudioService.Tracks
        .LoadTrackAsync(audioString, loadOptions)
        .ConfigureAwait(false);
    }
    
    if (track is null)
    {
      var errorMessage = $"[ Zeep. ] I couldn't find the local file {userInput}. [ Morp. ]";

      if (searchModeOrNull == TrackSearchMode.None)
        errorMessage =
          $"[ Zeep. ] I couldn't find a video with URL {audioString}. [ Morp. ]";
      else if (searchModeOrNull == TrackSearchMode.YouTube)
        errorMessage =
          $"[ Zeep. ] No search results for `{audioString}`. [ Morp. ]";

      var errorResponse = new DiscordFollowupMessageBuilder()
        .WithContent(errorMessage)
        .AsEphemeral();
      
      await ctx.EditResponseAsync(errorResponse).ConfigureAwait(false);

      return;
    }

    var position = await result.Player.PlayAsync(track).ConfigureAwait(false);

    var name = searchModeOrNull is null ? userInput : track.Uri?.ToString();
    var successMessage = $"[ Beep. ] Now playing: {name} [ Boop. ]";

    await ctx.EditResponseAsync(new DiscordFollowupMessageBuilder()
      .WithContent(successMessage).AsEphemeral()).ConfigureAwait(false);
    
    if (ctx is TextCommandContext textCtx)
      await textCtx.Message.ModifyEmbedSuppressionAsync(true);
  }


  private static async ValueTask StopHelper(CommandContext ctx, PlayerRetrieveResult result)
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
