using System.ComponentModel;
using System.Text;
using Formatter = DSharpPlus.Formatter;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;
using R2D20B.Attributes;
using DSharpPlus.Commands.Trees.Metadata;
using Microsoft.Extensions.Logging;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.ContextChecks;
using Microsoft.Extensions.Hosting;


namespace R2D20B.Commands;


internal class BasicCommands(
  CommandRegistry registry,
  HttpClient httpClient,
  UptimeService uptimeService,
  EmojiCatalog emojiCatalog,
  IHostEnvironment hostEnvironment,
  BotSettings settings,
  ILogger<BasicCommands> logger)
{
  static private readonly int s_HttpBodySnippetLength = 500;

  private CommandRegistry Registry { get; } = registry;
  private HttpClient HttpClient { get; } = httpClient;
  private UptimeService UptimeService { get; } = uptimeService;
  private EmojiCatalog Catalog { get; } = emojiCatalog;
  private IHostEnvironment HostEnvironment { get; } = hostEnvironment;
  private BotSettings Settings { get; } = settings;
  private ILogger<BasicCommands> Logger { get; } = logger;

  private Random RNG { get; } = new();


  private static bool CommandHasAttribute<T>(Command command)
    where T : Attribute
  {
    return command.Attributes.OfType<T>().Any();
  }


  [Command("test")]
  [Description("For internal use only.")]
  [AutoDelete][Secret][RequirePermissions(DiscordPermission.UseExternalApps)]
  public async ValueTask OutputTest(CommandContext ctx, string emojiName)
  {
    DiscordEmoji.TryFromName(ctx.Client, $":{emojiName}:",  includeGuilds: true, out var emoji);

    await ctx.Channel.SendMessageAsync($"[{emojiName}: {emoji}]");
  }

  
  [Command("env")]
  [Description("Gets which .NET environment I'm running in right now.")]
  [AutoDelete][Secret][RequirePermissions(DiscordPermission.UseExternalApps)]
  public async ValueTask GetEnvironment(CommandContext ctx)
  {
    var env = HostEnvironment.EnvironmentName;
    await ctx.RespondAsync($"[ Doop. ] I'm running in my {env} environment. [ Melp. ]");
  }


  [Command("ping")]
  [TextAlias("pong", "pingay", "beep")]
  [Description("Used as a simple acknowledgement that I'm online.")]
  public async ValueTask Ping(CommandContext ctx)
  {
    await ctx.RespondAsync("[ Beep. ]");
  }


  [Command("autodeletetest")]
  [AutoDelete]
  [Description("Just used to test the AutoDelete attribute.")]
  public async ValueTask AutoDeleteTest(CommandContext ctx)
  {
    await ctx.Channel.SendMessageAsync("[ Beep boop. ] Command message deleted. " +
      "[ Meep. ]");
  }


  [Command("echo")]
  [Description("Makes me repeat what you input back to you.")]
  public async ValueTask Echo(CommandContext ctx,
    [Description("The stuff to repeat.")]
    params string[] args)
  {
    await ctx.Channel.TriggerTypingAsync();
    // TODO: Possssssibly use a StringBuilder here

    var message = "[ Meep. ]" + Environment.NewLine;
    foreach (var arg in args)
      message += "`" + arg + "`" + Environment.NewLine;
    message += "[ Zorp. ]";

    await ctx.RespondAsync(message);
  }


  [Command("role")]
  [Description("Lists all the members of this server who have the specified role.")]
  public async ValueTask Role(CommandContext ctx,
    [Description("The role to check.")]
    string roleName)
  {
    await ctx.Channel.TriggerTypingAsync();

    if (ctx.Guild is null)
    {
      await MaybeRespondAsync(ctx, "[ Meep. ] I can't do that in this context. " +
        "[ Zorp. ]");
      return;
    }

    var roles = ctx.Guild.Roles.Values;
    var rQuery = from r in roles where r.Name == roleName select r;

    var guildName = ctx.Guild.Name;
    
    if (!rQuery.Any())
    {
      await MaybeRespondAsync(ctx, "[ Boop. ] I couldn't find a role called " +
        $"{roleName} in {guildName}. [ Beep boop. ]");
      return;
    }

    var role = rQuery.First();
    
    var members = ctx.Guild.Members.Values;
    var mQuery = from m in members where m.Roles.Contains(role) select m;

    if (!mQuery.Any())
    {
      await MaybeRespondAsync(ctx, $"[ Boop. ] No members of {guildName} " +
        $"who have the role {roleName}. [ Boop beep. ]");
      return;
    }

    if (mQuery.Count() == 1)
    {
      await ctx.RespondAsync("[ Beep boop. ] The only member of " +
        $"{guildName} with the role {roleName} is " +
        $"{mQuery.First().DisplayName}. [ Beep. ]");
      return;
    }

    var outputSb = new StringBuilder();
    outputSb.AppendLine("[ Beep Boop. ] The following members of " +
      $"{guildName} bear the role {roleName}:");
    
    foreach (var member in mQuery)
      outputSb.AppendLine(member.DisplayName);
    
    outputSb.Append("[ Beep. ]");

    await ctx.RespondAsync(outputSb.ToString());
  }


  [Command("roles")]
  [Description("Lists all the roles that a specified member has in this server.")]
  public async ValueTask Roles(CommandContext ctx,
    [Description("The member to check.")]
    string memberName)
  {
    await ctx.Channel.TriggerTypingAsync();

    if (ctx.Guild is null)
    {
      await MaybeRespondAsync(ctx, "[ Meep. ] I can't do that in this context. " +
        "[ Zorp. ]");
      return;
    }

    var members = ctx.Guild.Members.Values;
    var mQuery = from m in members where
      m.Nickname == memberName ||
      m.GlobalName == memberName ||
      m.Username == memberName select m;

    var guildName = ctx.Guild.Name;
    
    if (!mQuery.Any())
    {
      await MaybeRespondAsync(ctx, "[ Boop. ] I couldn't find a member named " +
        $"{memberName} in {guildName}. [ Boop beep. ]");
      return;
    }

    var member = mQuery.First();
    var roles = member.Roles;

    if (roles.Count() == 1)
    {
      await ctx.RespondAsync("[ Beep boop. ] The only role that " +
        $"{memberName} has in {guildName} is {roles.First().Name}." +
        "[ Beep. ]");
      return;
    }

    var outputSb = new StringBuilder();
    outputSb.AppendLine($"[ Beep Boop. ] {memberName} has the following " +
      $"roles in {guildName}:");
    
    foreach (var role in roles)
      outputSb.AppendLine(role.Name);
    
    outputSb.Append("[ Beep. ]");

    await ctx.RespondAsync(outputSb.ToString());
  }


  [Command("http")]
  [Description("Sends an HTTP request to the specified URL and prints the result.")]
  public async ValueTask HttpTest(CommandContext ctx,
    [Description("The URL to which to send the request.")]
    string url)
  {
    await ctx.Channel.TriggerTypingAsync();
    var urlInfo = new UrlInfo(url);

    if (!urlInfo.IsValid)
    {
      await MaybeRespondAsync(ctx, "[ Meep. ] Invalid URL. [ Zorp. ]");

      return;
    }

    var replySb = new StringBuilder();

    using var response = await HttpClient.GetAsync(urlInfo.Uri.ToString());
    replySb.AppendLine($"Status Code: {response.StatusCode}");
    replySb.AppendLine($"Request Uri: {response.RequestMessage?.RequestUri}");
    replySb.AppendLine($"Headers:");
    replySb.AppendLine($"    Server: {response.Headers.Server}");
    replySb.AppendLine($"    Location: {response.Headers.Location}");
    replySb.AppendLine($"    Date: {response.Headers.Date}");
    replySb.AppendLine($"    Content.Headers.ContentType: {response.Content.Headers.ContentType}");

    var snippet = await response.Content.ReadAsStringAsync();
    var length = s_HttpBodySnippetLength;
    if (snippet.Length > length) snippet = snippet[..length] + " ...";

    replySb.AppendLine($"Body Snippet: {snippet}");

    await ctx.RespondAsync(replySb.ToString());
  }


  [Command("help")]
  [Description("Lists the commands that I can execute.")]
  public async ValueTask Help(CommandContext ctx)
  {
    var commands = Registry.Commands.Commands.Values
      .Where(c => !CommandHasAttribute<SecretAttribute>(c));
    var count = commands.Count();

    var embedBuilder = new DiscordEmbedBuilder()
      .WithTitle($"[ Here are the {count} commands that I can execute: ]")
      .WithFooter("For commands marked with " +
        $"{Formatter.InlineCode("Auto-Delete")}, I will automatically " +
        "delete your message that contained the command after executing it.");
    
    foreach (var command in commands)
    {
      var description = command.Description ?? string.Empty;
      if (CommandHasAttribute<AutoDeleteAttribute>(command))
        description += $" {Formatter.InlineCode("[ Auto-Delete ]")}";

      embedBuilder.AddField(command.FullName, description);
    }

    await ctx.RespondAsync(embedBuilder.Build());
  }


  [Command("uptime")]
  [Description("Shows how long I've been running.")]
  public async ValueTask Uptime(CommandContext ctx)
  {
    await ctx.RespondAsync(
      $"[ Beep. ] I've been running for {UptimeService.UptimeFormatted}. [ Boop. ]");
  }


  [Command("dance")]
  [AutoDelete]
  [Description("Makes me convert the given text into dancing emoji.")]
  public async ValueTask Dance(CommandContext ctx,
    [Description("The text to make dance.")]
    [RemainingText]
    string text = "")
  {
    if (string.IsNullOrWhiteSpace(text))
    {
      await MaybeRespondAsync(ctx, "[ Barp. ] You need to give me something to say. [ Glmp. ]");
      return;
    }

    await ctx.Channel.TriggerTypingAsync();
    var sb = new StringBuilder();

    foreach (var textChar in text.ToUpperInvariant())
      if (EmojiCatalog.DanceEmoji.TryGetValue(textChar, out var dancingChar))
        sb.Append(dancingChar);

      if (sb.Length <= 0)
      {
        var str = Formatter.InlineCode(text);
        await MaybeRespondAsync(ctx,
          $"[ Zip bip. ] I tried to make {str} dance, but I just couldn't do it. [ Plibt. ]");
        return;
      }

      await ctx.Channel.SendMessageAsync(sb.ToString());
    }


  [Command("dancelist")]
  [Description("Shows all the emoji I have registered in my DanceEmoji dictionary.")]
  public async ValueTask DanceList(CommandContext ctx)
  {
    await ctx.Channel.TriggerTypingAsync();
    var sb = new StringBuilder();

    foreach (var entry in EmojiCatalog.DanceEmoji)
      sb.Append($"{Formatter.InlineCode(entry.Key.ToString())} : {entry.Value}        ");

    if (sb.Length <= 0) return;

    await ctx.RespondAsync(sb.ToString());
  }


  [Command("clean")]
  [Description("Sends in the penguins.")]
  [TextAlias("penguins")]
  [AutoDelete]
  public async ValueTask Clean(CommandContext ctx)
  {
    await ctx.Channel.TriggerTypingAsync();

    const int MAX_LARGE_EMOJI = 30;
    const int EMOJI_PER_ROW = 10;
    const int ROWS_PER_MESSAGE = MAX_LARGE_EMOJI / EMOJI_PER_ROW;
    const int MESSAGES_PER_CLEAN = 6;

    const double NOTCLEAN_CHANCE = 0.02;  // probability of getting :notclean: vs others
    const double CLEANISH_CHANCE = 0.70;  // probability of getting :cleanish: vs :clean:

    var messages = new List<string>();
    var messageSb = new StringBuilder();
    var rowSb = new StringBuilder();

    bool ShouldUseNotClean() =>
      RNG.NextDouble() < NOTCLEAN_CHANCE;
    bool ShouldUseCleanish() =>
      RNG.NextDouble() < CLEANISH_CHANCE;
    string GetNextEmoji() =>
      ShouldUseNotClean()
      ? EmojiCatalog.NotClean
      : (ShouldUseCleanish()
        ? EmojiCatalog.Cleanish
        : EmojiCatalog.Clean);

    for (var messageIndex = 0; messageIndex < MESSAGES_PER_CLEAN; ++messageIndex)
    {
      for (var rowIndex = 0; rowIndex < ROWS_PER_MESSAGE; ++rowIndex)
      {
        for (var emojiIndex = 0; emojiIndex < EMOJI_PER_ROW; ++emojiIndex)
          rowSb.Append(GetNextEmoji());

        messageSb.AppendLine(rowSb.ToString());
        rowSb.Clear();
      }

      messages.Add(messageSb.ToString());
      messageSb.Clear();
    }

    foreach (var message in messages)
      await ctx.Channel.SendMessageAsync(message);
  }


  
  public async ValueTask React(CommandContext ctx,
    string emojiName, int index)
  {
    if (index < 0)
    {
      await MaybeRespondAsync(ctx,
        "[ Skrp. ] Please provide a message index (i.e. a number) after the emoji name. [ Kip. ]");
      return;
    }

    DiscordEmoji? emoji;

    if (!Catalog.Emoji.TryGetValue(emojiName, out emoji))
    {
      if (!DiscordEmoji.TryFromName(ctx.Client, $":{emojiName}:",
        includeGuilds: true, out emoji))
      {
      await MaybeRespondAsync(ctx,
        "[ Skrp. ] I couldn't find an emoji by that name. [ Kip. ]");
        return;
      }
    }

    var messages = ctx.Channel.GetMessagesAsync(index);
    var messageToReactTo = await messages.LastAsync();

    await messageToReactTo.CreateReactionAsync(emoji);
    // Next I'll want to use messageToReactTo.WaitForReactionAsync somehow
  }


  private async ValueTask MaybeRespondAsync(CommandContext ctx, string content)
  {
    if (!Settings.Noisy) return;
    await ctx.RespondAsync(content);
  }
}
