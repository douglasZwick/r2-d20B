namespace R2D20B
{
  internal class BotConfig
  {
    public static string GetToken()
    {
      var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");

      if (string.IsNullOrWhiteSpace(token))
        throw new InvalidOperationException(
          "Discord bot token not configured. Set the DISCORD_TOKEN " +
          "environment variable to the token value and try again."
        );
        
      return token;
      
      // TODO: when I have this hosted on GitHub, set up its environment
      //   appropriately
    }


    public static string GetLavalinkServerPassword()
    {
      var password = Environment.GetEnvironmentVariable("LAVALINK_SERVER_PASSWORD");

      if (string.IsNullOrWhiteSpace(password))
        throw new InvalidOperationException(
          "Lavalink password not configured. Set the LAVALINK_SERVER_PASSWORD "
          + "environment variable to the password value and try again."
        );
      
      return password;
    }


    public static ulong GetDebugGuildId()
    {
      var rawString = Environment.GetEnvironmentVariable("DEBUG_GUILD_ID");

      if (ulong.TryParse(rawString, out var id))
        return id;
      
      return 0;
    }
  }
}
