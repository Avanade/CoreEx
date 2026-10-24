namespace CoreEx.Entities.Abstractions;

/// <summary>
/// Represents a value wrapper that can distinguish an unspecified value from a specified value.
/// </summary>
/// <typeparam name="T">The wrapped value type.</typeparam>
public interface IOptionalValue<out T> : IWrappedValue<T>
{
    /// <summary>
    /// Indicates whether a value was specified.
    /// </summary>
    bool IsSpecified { get; }
}
