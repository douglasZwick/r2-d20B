using System.ComponentModel;
using System.Net.NetworkInformation;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Trees;


namespace R2D20B.Commands
{
  internal class BasicCommands
  {
    [Command("ping")]
    [Description("Used as a simple acknowledgement that I'm online.")]
    public static async ValueTask Ping(CommandContext ctx)
    {
      await ctx.RespondAsync("[ Beep. ]");
    }


    [Command("echo")]
    [Description("Makes me repeat what you input back to you.")]
    public static async ValueTask Echo(CommandContext ctx,
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
    public static async ValueTask Role(CommandContext ctx,
      [Description("The role to check.")]
      string roleName)
    {
      
    }
  }
}
