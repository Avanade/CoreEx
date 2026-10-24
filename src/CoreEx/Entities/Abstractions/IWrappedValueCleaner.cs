namespace CoreEx.Entities.Abstractions;

/// <summary>
/// Enables a value wrapper to participate in runtime cleaning and return its own type.
/// </summary>
/// <typeparam name="TSelf">The implementing wrapper type.</typeparam>
public interface IWrappedValueCleaner<TSelf> where TSelf : IWrappedValueCleaner<TSelf>
{
    /// <summary>
    /// Cleans the wrapped value and returns the reconstructed wrapper.
    /// </summary>
    /// <param name="args">The cleaning configuration.</param>
    /// <returns>The wrapper containing the cleaned value.</returns>
    /// <remarks>The result must be non-null and have the same runtime wrapper type, preserving the wrapper's invariants. Optional wrappers must also preserve whether the value was specified, including when cleaning produces a null inner value, and must not read an unspecified value.</remarks>
    TSelf Clean(CleanArgs args);
}
