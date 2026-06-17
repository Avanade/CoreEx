namespace CoreEx.Database.Extended;

/// <summary>
/// Provides <see cref="IMultiSetArgs"/> helpers.
/// </summary>
public static class MultiSetArgs
{
    /// <summary>
    /// Creates an <see cref="IMultiSetArgs"/> <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <param name="args">The <see cref="IMultiSetArgs"/> arguments.</param>
    /// <returns>The <see cref="IEnumerable{T}"/> <see cref="IMultiSetArgs"/>.</returns>
    public static IEnumerable<IMultiSetArgs> Create(params IEnumerable<IMultiSetArgs> args) => args.AsEnumerable();
}