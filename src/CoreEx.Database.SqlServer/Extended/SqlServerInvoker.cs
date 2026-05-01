namespace CoreEx.Database.SqlServer.Extended;

/// <summary>
/// Provides the <see cref="SqlServerDatabase"/> invoker functionality.
/// </summary>
[InvokerName("CoreEx.Database.SqlServer.SqlServer")]
public class SqlServerInvoker : DatabaseInvoker
{
    private static SqlServerInvoker? _default;

    /// <summary>
    /// Gets the default <see cref="SqlServerInvoker"/> instance.
    /// </summary>
    public static SqlServerInvoker Default => ExecutionContext.GetService<SqlServerInvoker>() ?? (_default ??= new SqlServerInvoker());
}