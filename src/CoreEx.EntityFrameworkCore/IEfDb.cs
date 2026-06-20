namespace CoreEx.EntityFrameworkCore;

/// <summary>
/// Enables the extended <see cref="IDatabase"/>-based <see href="https://learn.microsoft.com/en-us/ef/core/">Entity Framework Core</see> capabilities.
/// </summary>
public interface IEfDb
{
    /// <summary>
    /// Gets the underlying <see name="Microsoft.EntityFrameworkCore.DbContext"/>.
    /// </summary>
    DbContext DbContext { get; }

    /// <summary>
    /// Gets the <see cref="IDatabase"/>.
    /// </summary>
    IDatabase Database { get; }

    /// <summary>
    /// Gets the <see cref="EfDbOptions"/>.
    /// </summary>
    EfDbOptions Options { get; }

    /// <summary>
    /// Gets the <see cref="CoreEx.ExecutionContext"/>.
    /// </summary>
    ExecutionContext ExecutionContext { get; }

    /// <summary>
    /// Gets the <see cref="EfDbInvoker"/>.
    /// </summary>
    EfDbInvoker Invoker { get; }
}