using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using R2D20B.Commands;
using DSharpPlus.Commands.EventArgs;
using R2D20B.Attributes;
using DSharpPlus.Commands.Processors.TextCommands;
using Microsoft.Extensions.DependencyInjection;


namespace R2D20B
{
  internal class Bot
  {
    public DiscordClient m_Client;
    public CommandsExtension? m_CommandsExtension;
    // public IEnumerable<Command> m_Commands { get; private set; } = [];


    public Bot()
    {
      var token = BotConfig.GetToken();

      m_Client = DiscordClientBuilder
        .CreateDefault(token, DiscordIntents.All)
        .ConfigureServices(services =>
        {
          services.AddSingleton(this);
        })
        .UseCommands((services, extension) =>
        {
          m_CommandsExtension = extension;
          extension.AddCommands(typeof(BasicCommands).Assembly);
          
          extension.CommandExecuted += OnCommandExecuted;
        })
        .Build();
      
      // m_Commands = m_CommandsExtension!.Commands.Values;
    }

    public async Task RunAsync()
    {
      var status =
        new DiscordActivity("Ligma", DiscordActivityType.Playing);
      await m_Client.ConnectAsync(status, DiscordUserStatus.Online);

      await Task.Delay(-1);
    }


    public async Task OnCommandExecuted(CommandsExtension extension,
      CommandExecutedEventArgs e)
    {
      var attribs = e.Context.Command.Attributes;
      var shouldAutoDelete = attribs.OfType<AutoDeleteAttribute>().Any();

      if (shouldAutoDelete)
      {
        if (e.Context is TextCommandContext textCtx)
        {
          try
          {
            await textCtx.Message.DeleteAsync("Auto-delete command message");
          }
          catch (Exception ex)
          {
            var msg = textCtx.Message.Content;
            Console.WriteLine("Failed to delete auto-delete command" +
              $"message {msg}. Exception: {ex}");
          }
        }
      }
    }
  }
}
