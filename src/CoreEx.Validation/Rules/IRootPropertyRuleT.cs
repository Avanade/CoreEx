namespace CoreEx.Validation.Rules;

/// <summary>
/// Enables root property rule capabilities.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
public interface IRootPropertyRule<TEntity> : IPropertyRule<TEntity> where TEntity : class
{
    /// <summary>
    /// Indicates whether the originating property type is <see cref="Nullable{T}"/>.
    /// </summary>
    bool IsValueNullable { get; }

    /// <summary>
    /// Gets the originating property <see cref="Nullable{T}.Value"/> or <see langword="default"/> (where <see cref="IsValueNullable"/>).
    /// </summary>
    /// <typeparam name="T">The property <see cref="Nullable{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <param name="entity">The entity value.</param>
    /// <returns>The originating property <see cref="Nullable{T}.Value"/> or <see langword="default"/>.</returns>
    T GetNullableValueOrDefault<T>(TEntity entity);

    /// <summary>
    /// Indicates whether the originating property <see cref="Nullable{T}.Value"/> is <see langword="default"/> (where <see cref="IsValueNullable"/>).
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <returns><see langword="true"/> where <see langword="default"/>; otherwise, <see langword="false"/>.</returns>
    bool IsNullableValueDefault(TEntity entity);

    /// <summary>
    /// Sets (overrides) the property text to be used within any error message.
    /// </summary>
    /// <param name="text">The property <see cref="LText"/>.</param>
    void SetText(LText? text);

    /// <summary>
    /// Sets (overrides) the format to use when localizing the property value within any error message.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="formatProvider">The optional <see cref="IFormatProvider"/>.</param>
    /// <param name="quotingCharacter">The quoting character so it appears as a literal string.</param>
    /// <remarks>The underlying property type must implement <see cref="IFormattable"/> as this results in <see cref="IFormattable.ToString(string?, IFormatProvider?)"/> being used.</remarks>
    void SetFormat(string? format, IFormatProvider? formatProvider, char? quotingCharacter);

    /// <summary>
    /// Validates the property value.
    /// </summary>
    /// <param name="context">The <see cref="ValidationContext{TEntity}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    Task ValidateAsync(ValidationContext<TEntity> context, CancellationToken cancellationToken);
}