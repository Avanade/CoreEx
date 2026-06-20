namespace CoreEx.Database.Postgres.Extended;

/// <summary>
/// Provides the <see cref="PostgresDatabase"/> invoker functionality.
/// </summary>
[InvokerName("CoreEx.Database.Postgres.Postgres")]
public class PostgresInvoker : DatabaseInvoker
{
    private static PostgresInvoker? _default;

    /// <summary>
    /// Gets the default <see cref="PostgresInvoker"/> instance.
    /// </summary>
    public static PostgresInvoker Default => ExecutionContext.GetService<PostgresInvoker>() ?? (_default ??= new PostgresInvoker());
}