namespace CoreEx.Entities.Extended;

/// <summary>
/// Enables a means to determine if a value is in its default state.
/// </summary>
/// <remarks>For example; all underlying properties for an object have their respective default value.</remarks>
public interface IDefault
{
    /// <summary>
    /// Indicates whether the value is in its default state.
    /// </summary>
    /// <returns><see langword="true"/> indicates that the value is considered default; otherwise, <see langword="false"/>.</returns>
    bool IsDefault();
}