namespace CoreEx.Database;

public abstract partial class DatabaseCommand
{
    /// <summary>
    /// Executes the query and returns the first column of the first row in the result set returned by the query.
    /// </summary>
    /// <typeparam name="T">The result <see cref="Type"/>.</typeparam>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The value of the first column of the first row in the result set.</returns>
    public Task<T> ScalarAsync<T>(CancellationToken cancellationToken = default) => ScalarAsync<T>(null, cancellationToken);

    /// <summary>
    /// Executes the query and returns the first column of the first row in the result set returned by the query.
    /// </summary>
    /// <typeparam name="T">The result <see cref="Type"/>.</typeparam>
    /// <param name="parameters">The post-execution delegate to enable parameter access.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The value of the first column of the first row in the result set.</returns>
    public async Task<T> ScalarAsync<T>(Action<DbParameterCollection>? parameters, CancellationToken cancellationToken = default) => await Database.Invoker.InvokeAsync(Database, DbArgs, async (_, _, cancellationToken) =>
    {
        using var cmd = await CreateCommandAsync(cancellationToken).ConfigureAwait(false);
        parameters?.Invoke(cmd.Parameters);
        var result = await LogCommand(cmd).ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        if (result is null || result == DBNull.Value)
            return default!;

        if (result is DateTime dt)
        {
            dt = Cleaner.Clean(dt, Database.DateTimeTransform);

            if (typeof(T) == typeof(DateTimeOffset))
            {
                var dto = new DateTimeOffset(dt);
                return Internal.Cast<DateTimeOffset, T>(dto);
            }

            if (typeof(T) == typeof(DateTimeOffset?))
            {
                DateTimeOffset? dto = new DateTimeOffset(dt);
                return Internal.Cast<DateTimeOffset?, T>(dto);
            }

            result = dt;
        }

        return (T)result;
    }, cancellationToken).ConfigureAwait(false);
}