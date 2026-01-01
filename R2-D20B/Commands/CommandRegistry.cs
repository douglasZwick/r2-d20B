using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using DSharpPlus.Commands.Processors.TextCommands;
using R2D20B.Attributes;


namespace R2D20B.Commands;


internal sealed class CommandRegistry
{
  private CommandsExtension? m_Commands = null;
  public CommandsExtension Commands =>
    m_Commands ?? throw new InvalidOperationException("Commands extension not initialized yet.");


  public void Initialize(CommandsExtension commands)
  {
    if (m_Commands is not null)
      throw new InvalidOperationException("CommandRegistry already initialized.");

    m_Commands = commands;
    commands.CommandExecuted += OnCommandExecuted;
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
