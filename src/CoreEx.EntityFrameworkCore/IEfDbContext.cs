namespace CoreEx.EntityFrameworkCore;

/// <summary>
/// Enables access to the underlying <see cref="IDatabase"/> instance (see <see cref="BaseDatabase"/>).
/// </summary>
public interface IEfDbContext
{
    /// <summary>
    /// Gets the base <see cref="IDatabase"/>.
    /// </summary>
    public IDatabase BaseDatabase { get; }
}