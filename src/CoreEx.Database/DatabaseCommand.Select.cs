namespace CoreEx.Database;

public abstract partial class DatabaseCommand
{
    /// <summary>
    /// Selects from the first result set using a <paramref name="func"/> to handle each <see cref="DatabaseRecord"/>.
    /// </summary>
    /// <param name="func">The <see cref="DatabaseRecord"/> handling function.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>The <paramref name="func"/> returns a <see cref="bool"/> that controls whether the next <see cref="DatabaseRecord"/> should be read. A value of <see langword="true"/> indicates that 
    /// reading should continue; otherwise, <see langword="false"/> to stop.</remarks>
    public Task SelectAsync(Func<DatabaseRecord, bool> func, CancellationToken cancellationToken = default) => SelectInternalAsync(func, nameof(SelectAsync), cancellationToken);

    /// <summary>
    /// Select the rows from the query (interna)l.
    /// </summary>
    private async Task SelectInternalAsync(Func<DatabaseRecord, bool> func, string memberName, CancellationToken cancellationToken)
    {
        func.ThrowIfNull();

        await Database.Invoker.InvokeAsync(Database, DbArgs, async (_, _, cancellationToken) =>
        {
            using var cmd = await CreateCommandAsync(cancellationToken).ConfigureAwait(false);
            using var dr = await LogCommand(cmd).ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!func(new DatabaseRecord(Database, dr)))
                    break;
            }
        }, cancellationToken, memberName).ConfigureAwait(false);
    }
}