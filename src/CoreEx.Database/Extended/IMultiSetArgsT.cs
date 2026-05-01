namespace CoreEx.Database.Extended;

/// <summary>
/// Enables the <see cref="IDatabase"/> multi-set arguments with a <see cref="Mapper"/>.
/// </summary>
/// <typeparam name="T">The item <see cref="Type"/>.</typeparam>
public interface IMultiSetArgs<T> : IMultiSetArgs where T : class, new()
{
    /// <summary>
    /// Gets the <see cref="IDatabaseMapper{TItem}"/> for the <see cref="DatabaseRecord"/>.
    /// </summary>
    IDatabaseMapper<T> Mapper { get; }
}