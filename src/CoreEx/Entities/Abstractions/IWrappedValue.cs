namespace CoreEx.Entities.Abstractions;

/// <summary>
/// Represents a value wrapper that exposes its underlying value.
/// </summary>
/// <typeparam name="T">The wrapped value type.</typeparam>
public interface IWrappedValue<out T>
{
    /// <summary>
    /// Gets the wrapped value.
    /// </summary>
    T Value { get; }
}
