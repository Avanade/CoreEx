namespace CoreEx.EntityFrameworkCore;

/// <summary>
/// Provides the extended <see cref="IDatabase"/>-based <see href="https://learn.microsoft.com/en-us/ef/core/">Entity Framework Core</see> capabilities.
/// </summary>
/// <typeparam name="TDbContext">The <see cref="DbContext"/> <see cref="Type"/>.</typeparam>
public class EfDb<TDbContext> : IEfDb, IDisposable where TDbContext : DbContext, IEfDbContext
{
    private readonly TDbContext _dbContext;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfDb{TDbContext}"/> class.
    /// </summary>
    /// <param name="dbContext">The <see cref="DbContext"/>.</param>
    /// <param name="options">The optional <see cref="EfDbOptions"/> (typically a singleton service or statically declared).</param>
    /// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
    public EfDb(TDbContext dbContext, EfDbOptions? options = null, ExecutionContext? executionContext = null)
    {
        _dbContext = dbContext.ThrowIfNull();
        Options = options ?? new EfDbOptions();
        ExecutionContext = executionContext ?? ExecutionContext.Current;
        Database.UseTransactionChanged += Database_UseTransactionChanged;
    }

    /// <inheritdoc/>
    public DbContext DbContext => _dbContext;

    /// <inheritdoc/>
    public IDatabase Database => _dbContext.BaseDatabase;

    /// <inheritdoc/>
    public EfDbOptions Options { get; }

    /// <inheritdoc/>
    public ExecutionContext ExecutionContext { get; }

    /// <inheritdoc/>
    public EfDbInvoker Invoker { get; set => field = value.ThrowIfNull(); } = EfDbInvoker.Default;

    /// <summary>
    /// Gets the <see cref="EfDbModel{TModel}"/> for the specified <typeparamref name="TModel"/>.
    /// </summary>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <returns>The <see cref="EfDbModel{TModel}"/>.</returns>
    public EfDbModel<TModel> Model<TModel>() where TModel : class => new(this, Options.GetOrAddModelOptions<TModel>());

    /// <summary>
    /// Wires up the current transaction to the <see cref="DbContext"/> when the transaction changes on the <see cref="Database"/>.
    /// </summary>
    private void Database_UseTransactionChanged(object? sender, EventArgs e) => _dbContext.Database.UseTransaction(Database.CurrentTransaction);

    /// <summary>
    /// Dispose of resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="ExecutionContext"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            if (!_disposed)
            {
                Database.UseTransactionChanged -= Database_UseTransactionChanged;
                _disposed = true;
            }
        }
    }
}