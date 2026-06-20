namespace CoreEx.Metadata;

/// <summary>
/// Enables the runtime metadata definition for a property within an entity.
/// </summary>
public interface IPropertyRuntimeMetadata
{
    /// <summary>
    /// Gets the owning entity <see cref="System.Type"/>.
    /// </summary>
    Type Owner { get; }

    /// <summary>
    /// Gets the property <see cref="System.Type"/>.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Gets the property name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the property text.
    /// </summary>
    LText Text { get; }

    /// <summary>
    /// Gets the JSON property name.
    /// </summary>
    string? JsonName { get; }

    /// <summary>
    /// Gets the default value.
    /// </summary>
    object? DefaultValue { get; }

    /// <summary>
    /// Gets the <see cref="Entities.CleanOption"/>.
    /// </summary>
    CleanOption CleanOption { get; }

    /// <summary>
    /// Indicates whether the property is read-only (i.e. does not have a setter).
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Gets the format string used when formatting the property value as a <see langword="string"/>.
    /// </summary>
    /// <remarks>See <see cref="IFormattable.ToString(string?, IFormatProvider?)"/> and <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/composite-formatting">composite formatting</see>.</remarks>
    string? Format { get; }

    /// <summary>
    /// Indicates whether the property value is considered in its default state.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    bool IsDefault(object entity);

    /// <summary>
    /// Cleans the property value based on the <see cref="CleanOption"/>.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    void Clean(object entity);

    /// <summary>
    /// Gets the property value.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <returns>The property value.</returns>
    object? GetValue(object entity);

    /// <summary>
    /// Get the property value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity">The entity value.</param>
    /// <returns>The property value.</returns>
    T GetValue<T>(object entity);

    /// <summary>
    /// Sets (overrides) the property value with the specified <paramref name="value"/>.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="value">The overriding property value.</param>
    void SetValue(object entity, object? value);

    /// <summary>
    /// Sets (overrides) the property value with the specified <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity">The entity value.</param>
    /// <param name="value">The overriding property value.</param>
    void SetValue<T>(object entity, T value);

    /// <summary>
    /// Gets the JSON property name.
    /// </summary>
    /// <param name="options">The optional <see cref="JsonSerializerOptions"/>.</param>
    /// <returns>The JSON property name.</returns>
    /// <remarks>Uses the <see cref="JsonName"/> where not <see langword="null"/>; otherwise, uses the property <see cref="Name"/> passed through the optional <see cref="JsonSerializerOptions.PropertyNamingPolicy"/> <see cref="JsonNamingPolicy.ConvertName(string)"/>.</remarks>
    string GetJsonName(JsonSerializerOptions? options = null);
}