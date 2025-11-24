namespace R2D20B
{
  internal class Program
  {
    static async Task Main(string[] args)
    {
      var bot = new Bot();
      await bot.RunAsync();
    }
  }
}
