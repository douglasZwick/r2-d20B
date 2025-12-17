using DSharpPlus.Commands;

namespace R2D20B.Commands;


internal sealed class AsyncCommandExecutor : ICommandExecutor
{
  private readonly DefaultCommandExecutor Executor = new();


  public ValueTask ExecuteAsync(CommandContext ctx, CancellationToken token)
    => new(Task.Run(() => Executor.ExecuteAsync(ctx, token).AsTask(), token));
}
