using System.ComponentModel;
using System.Text;
using Formatter = DSharpPlus.Formatter;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;
using R2D20B.Attributes;
using DSharpPlus.Commands.Trees.Metadata;
using Microsoft.Extensions.Logging;


namespace R2D20B.Commands
{
  internal class BasicCommands(
    CommandRegistry registry,
    HttpClient httpClient,
    UptimeService uptimeService,
    ILogger<BasicCommands> logger)
  {
    static private readonly int s_HttpBodySnippetLength = 500;

    private CommandRegistry Registry { get; init; } = registry;
    private HttpClient HttpClient { get; init; } = httpClient;
    private UptimeService UptimeService { get; init; } = uptimeService;
    private ILogger<BasicCommands> Logger { get; init; } = logger;


    private static bool CommandHasAttribute<T>(Command command)
      where T : Attribute
    {
      return command.Attributes.OfType<T>().Any();
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
      await ctx.RespondAsync("[ Beep boop. ] Command message deleted. " +
        "[ Meep. ]");
    }


    [Command("echo")]
    [Description("Makes me repeat what you input back to you.")]
    public async ValueTask Echo(CommandContext ctx,
      [Description("The stuff to repeat.")]
      params string[] args)
    {
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
      if (ctx is null) return;

      if (ctx.Guild is null)
      {
        await ctx.RespondAsync("[ Meep. ] I can't do that in this context. " +
          "[ Zorp. ]");
        return;
      }

      var roles = ctx.Guild.Roles.Values;
      var rQuery = from r in roles where r.Name == roleName select r;

      var guildName = ctx.Guild.Name;
      
      if (!rQuery.Any())
      {
        await ctx.RespondAsync("[ Boop. ] I couldn't find a role called " +
          $"{roleName} in {guildName}. [ Beep boop. ]");
        return;
      }

      var role = rQuery.First();
      
      var members = ctx.Guild.Members.Values;
      var mQuery = from m in members where m.Roles.Contains(role) select m;

      if (!mQuery.Any())
      {
        await ctx.RespondAsync($"[ Boop. ] No members of {guildName} " +
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
      if (ctx is null) return;

      if (ctx.Guild is null)
      {
        await ctx.RespondAsync("[ Meep. ] I can't do that in this context. " +
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
        await ctx.RespondAsync("[ Boop. ] I couldn't find a member named " +
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
      var urlInfo = new UrlInfo(url);

      if (!urlInfo.IsValid)
      {
        await ctx.RespondAsync("[ Meep. ] Invalid URL. [ Zorp. ]");

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
      if (ctx is null) return;

      var commands = Registry.Commands.Commands.Values;
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
  }
}
