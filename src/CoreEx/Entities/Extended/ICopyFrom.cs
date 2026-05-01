namespace CoreEx.Entities.Extended;

/// <summary>
/// Enables a <see cref="CopyFrom"/>.
/// </summary>
public interface ICopyFrom
{
    /// <summary>
    /// Copies <paramref name="from"/> into this.
    /// </summary>
    /// <param name="from">The from value.</param>
    /// <remarks>Only mutable (set) properties will be copied; i.e. read-only (get) properties will remain unchanged.</remarks>
    void CopyFrom<TFrom>(TFrom from) where TFrom : class;
}