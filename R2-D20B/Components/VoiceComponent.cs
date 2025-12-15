using System.Diagnostics;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Logging;

namespace R2D20B.Components
{
  internal class VoiceComponent(ILogger<VoiceComponent> logger)
  {
    private ILogger<VoiceComponent> Logger { get; init; } = logger;

    public VoiceNextExtension? VoiceNext { get; set; }


    public async Task JoinVoiceChannelAsync2(CommandContext ctx, DiscordChannel voiceChannel)
    {
      if (VoiceNext is not VoiceNextExtension vn) return;

      var existing = vn.GetConnection(voiceChannel.Guild);

      Logger.LogInformation("JoinVoiceChannelAsync | " +
        "Previous connection null: {NullPreviousConnection}", existing is null);
      Logger.LogInformation("JoinVoiceChannelAsync | " +
        "Channel type: {Type} | Channel ID: {ChannelId} | Guild ID: {GuildId}",
        voiceChannel.Type, voiceChannel.Id, voiceChannel.Guild.Id);

      if (existing is not null)
      {
        if (existing.TargetChannel == voiceChannel)
        {
          await ctx.RespondAsync("[ Borp. ] I'm already in that channel. [ Zeep. ]");

          return;
        }

        existing.Disconnect();
      }

      VoiceNextConnection connection;

      try
      {
        var connectTask = vn.ConnectAsync(voiceChannel);
        var finished = await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(10)));

        if (finished != connectTask)
        {
          Logger.LogError("VoiceNext ConnectAsync timed out (handshake never completed).");
          return;
        }

        connection = await connectTask;
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "JoinVoiceChannelAsync | " +
          "vn.ConnectAsync failed for channel {ChannelId} | Exception: {Exception}",
          voiceChannel.Id, ex.Message);
        return;
      }

      Logger.LogInformation("JoinVoiceChannelAsync | " +
        "Current connection null: {NullCurrentConnection} | Returned target channel ID: {TargetId}",
        connection is null, connection?.TargetChannel.Id);
      
      // Add an event listener to the connection.VoiceReceived event later if needed
    }

    
    public async Task JoinVoiceChannelAsync(CommandContext ctx, DiscordChannel voiceChannel)
    {
      if (VoiceNext is not VoiceNextExtension vn)
      {
        Logger.LogError("Join | VoiceNext is null / not initialized.");
        return;
      }

      Logger.LogInformation("Join | voiceChannel: {Name} type: {Type} id: {Id} guild: {GuildId}",
        voiceChannel.Name, voiceChannel.Type, voiceChannel.Id, voiceChannel.Guild?.Id);

      var existing = vn.GetConnection(voiceChannel.Guild!);
      Logger.LogInformation("Join | existing connection null? {IsNull}", existing is null);

      if (existing is not null)
      {
        Logger.LogInformation("Join | existing target id: {TargetId}", existing.TargetChannel?.Id);

        if (existing.TargetChannel?.Id == voiceChannel.Id)
        {
          await ctx.RespondAsync("[ Borp. ] I'm already in that channel. [ Zeep. ]");
          return;
        }

        existing.Disconnect();
      }

      VoiceNextConnection connection;

      try
      {
        var connectTask = vn.ConnectAsync(voiceChannel);
        var finished = await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(10)));

        if (finished != connectTask)
        {
          Logger.LogError("VoiceNext ConnectAsync timed out (handshake never completed).");
          return;
        }

        connection = await connectTask;
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "Join | vn.ConnectAsync threw.");
        await ctx.RespondAsync("Connect failed (see logs).");
        return;
      }

      Logger.LogInformation("Join | returned connection null? {IsNull}", connection is null);
      Logger.LogInformation("Join | returned target id: {TargetId}", connection?.TargetChannel?.Id);

      var fromCache = vn.GetConnection(voiceChannel.Guild!);
      Logger.LogInformation("Join | cache connection null? {IsNull}", fromCache is null);
    }


    public async Task LeaveVoiceChannel(CommandContext ctx)
    {
      if (VoiceNext is not VoiceNextExtension vn) return;

      var connection = vn.GetConnection(ctx.Channel.Guild);

      Logger.LogInformation("LeaveVoiceChannel | Current voice connection: {CurrentConnection}",
        connection);

      if (connection is null)
      {
        await ctx.RespondAsync(
          "[ Borp. ] I'm not currently connected to a voice channel. [ Zeep. ]");

        return;
      }

      connection.Disconnect();
    }


    public async Task PlaySoundAsync(CommandContext ctx, string soundName)
    {
      if (VoiceNext is not VoiceNextExtension vn) return;

      var connection = vn.GetConnection(ctx.Channel.Guild);

      Logger.LogInformation(
        "PlaySoundAsync | Current voice connection: {CurrentConnection} | Sound name: {SoundName}",
        connection, soundName);

      if (connection is null)
      {
        await ctx.RespondAsync(
          "[ Borp. ] I'm not currently connected to a voice channel. [ Zeep. ]");

        return;
      }

      var path = GetSoundPath(soundName);

      Logger.LogInformation("PlaySoundAsync | Sound path: {SoundPath}", path);

      if (!File.Exists(path))
      {
        await ctx.RespondAsync(
          "[ Borp. ] I can't find that sound. [ Zeep. ]");

        return;
      }

      var ffmpeg = Process.Start(new ProcessStartInfo
      {
        FileName = "ffmpeg",
        Arguments = $@"-i ""{path}"" -ac 2 -f s16le -ar 48000 pipe:1",
        RedirectStandardOutput = true,
        UseShellExecute = false,
      });

      if (ffmpeg is null)
      {
        await ctx.RespondAsync(
          "[ Borp. ] Error starting ffmpeg. [ Zeep. ]");

        return;
      }

      var pcm = ffmpeg.StandardOutput.BaseStream;
      var sink = connection.GetTransmitSink();

      await pcm.CopyToAsync(sink);
    }


    private static string GetSoundPath(string soundName)
    {
      const string extension = "mp3";

      soundName = soundName.ToLowerInvariant();
      var fileName = Path.ChangeExtension(soundName, extension);
      return Path.Combine(AppContext.BaseDirectory, fileName);
    }
  }
}
