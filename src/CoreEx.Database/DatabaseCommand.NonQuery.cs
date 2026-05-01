namespace CoreEx.Database;

public abstract partial class DatabaseCommand
{
    /// <summary>
    /// Executes a non-query command.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The number of rows affected.</returns>
    public Task<int> NonQueryAsync(CancellationToken cancellationToken = default) => NonQueryAsync(null, cancellationToken);

    /// <summary>
    /// Executes a non-query command.
    /// </summary>
    /// <param name="parameters">The post-execution delegate to enable parameter access.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The number of rows affected.</returns>
    public async Task<int> NonQueryAsync(Action<DbParameterCollection>? parameters, CancellationToken cancellationToken = default) => await Database.Invoker.InvokeAsync(Database, DbArgs, async (_, _, cancellationToken) =>
    {
        using var cmd = await CreateCommandAsync(cancellationToken).ConfigureAwait(false);
        parameters?.Invoke(cmd.Parameters);
        return await LogCommand(cmd).ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }, cancellationToken).ConfigureAwait(false);
}