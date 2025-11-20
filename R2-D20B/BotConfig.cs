namespace R2D20B
{
  internal class BotConfig
  {
    public static string GetToken()
    {
      var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");

      if (!string.IsNullOrWhiteSpace(token))
        return token;
      
      // TODO: when I have this hosted on GitHub, set up its environment
      //   appropriately

      throw new InvalidOperationException(
        "Discord bot token not configured. Set the DISCORD_TOKEN " +
        "environment variable to the token value and try again."
      );
    }
  }
}