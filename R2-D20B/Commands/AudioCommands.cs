using System.ComponentModel;
using Formatter = DSharpPlus.Formatter;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;
using DSharpPlus.Commands.ArgumentModifiers;
using Microsoft.Extensions.Logging;


namespace R2D20B.Commands
{
  internal class AudioCommands(Bot bot, ILogger<AudioCommands> logger)
  {
    private Bot Bot { get; init; } = bot;
    private ILogger<AudioCommands> Logger { get; init; } = logger;


    // [Command("join")]
    // [Description("Makes me join the specified voice channel (or whichever channel you're in, "
    //   + "if no channel was given).")]
    // public async ValueTask Join(CommandContext ctx,
    //   [Description("The name of the voice channel for me to join.")]
    //   [RemainingText] string voiceChannelName = "")
    // {
    //   Logger.LogInformation("Join command | ============== Function top ==============");
    //   Logger.LogInformation("Join command | " +
    //     "Specified channel name: {GivenName}", voiceChannelName);

    //   if (ctx is null) return;
    //   if (ctx.Guild is not DiscordGuild guild) return;
      
    //   if (string.IsNullOrWhiteSpace(voiceChannelName))
    //   {
    //     if (!guild.Members.TryGetValue(ctx.User.Id, out var member)) return;
    //     if (member.VoiceState.ChannelId is null)
    //     {
    //       // say something

    //       return;
    //     }

    //     if (!guild.Channels.TryGetValue(member.VoiceState.ChannelId.Value, out var channelById))
    //     {
    //       // say something
          
    //       return;
    //     }

    //     Logger.LogInformation("Join command | " +
    //       "Member name: {MemberName} | Channel ID: {ChannelId} | Channel by ID: {ChannelById}",
    //       member.DisplayName, member.VoiceState.ChannelId, channelById.Name);

    //     await Bot.Voice.JoinVoiceChannelAsync(ctx, channelById);

    //     return;
    //   }

    //   var specifiedChannel = guild.Channels.Values.FirstOrDefault(
    //     c => string.Equals(voiceChannelName, c.Name, StringComparison.OrdinalIgnoreCase));

    //   if (specifiedChannel is null) return;

    //   Logger.LogInformation("Join command | " +
    //     "Channel.Name: {ChannelName}", specifiedChannel.Name);

    //   await Bot.Voice.JoinVoiceChannelAsync(ctx, specifiedChannel);
    // }


    // [Command("leave")]
    // [Description("Makes me leave the voice channel I'm in.")]
    // public async ValueTask Leave(CommandContext ctx)
    // {
    //   Logger.LogDebug("Leave | Command context: {CommandContext}", ctx);

    //   if (ctx is null) return;
      
    //   await Bot.Voice.LeaveVoiceChannel(ctx);
    // }


    // public async ValueTask Play(CommandContext ctx,
    //   [RemainingText] string soundName)
    // {
    //   Logger.LogDebug("Play | Command context: {CommandContext} | Sound name: {SoundName}",
    //     ctx, soundName);

    //   if (ctx is null) return;
    //   if (string.IsNullOrWhiteSpace(soundName)) return;
      
    //   await Bot.Voice.PlaySoundAsync(ctx, soundName);
    // }
  }
}