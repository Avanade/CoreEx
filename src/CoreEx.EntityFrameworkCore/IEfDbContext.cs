namespace CoreEx.EntityFrameworkCore;

/// <summary>
/// Enables access to the underlying <see cref="IDatabase"/> instance (see <see cref="BaseDatabase"/>).
/// </summary>
public interface IEfDbContext
{
    /// <summary>
    /// Gets the base <see cref="IDatabase"/>.
    /// </summary>
    /// <remarks>Must return the same <see cref="IDatabase"/> instance on every access; <see cref="EfDb{TDbContext}"/> subscribes to <see cref="IDatabase.UseTransactionChanged"/> on this instance in its constructor and unsubscribes from it on <see cref="IDisposable.Dispose"/> using the same property, so a differing instance per call would leak the event handler.</remarks>
    public IDatabase BaseDatabase { get; }
}