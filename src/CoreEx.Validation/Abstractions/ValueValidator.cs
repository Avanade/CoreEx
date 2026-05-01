namespace CoreEx.Validation.Abstractions;

/// <summary>
/// Enables validation for a value.
/// </summary>
/// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
public sealed class ValueValidator<T> : Validator<ValidationValue<T>>, IValueValidator<T>
{
    private readonly ValidationValue<T> _validationValue;
    private readonly IPropertyRuntimeMetadata _metadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueValidator{T}"/> class.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="name">The value name.</param>
    /// <param name="jsonName">The value JSON name.</param>
    /// <param name="text">The friendly text name.</param>
    /// <param name="configure">The action to configure the resulting <see cref="IPropertyRule{TEntity, TProperty}"/>.</param>
    /// <param name="getNullableValue">A function to get the underlying nullable value.</param>
    /// <param name="isNullableValueDefault">A function to determine whether the underlying nullable value is its default.</param>
    internal ValueValidator(T value, string name, string? jsonName, LText? text, Action<IPropertyRule<ValidationValue<T>, T>>? configure, Func<ValidationValue<T>, T>? getNullableValue, Func<ValidationValue<T>, bool>? isNullableValueDefault)
    {
        _validationValue = new(value);
        _metadata = new PropertyRuntimeMetadata<ValidationValue<T>, T?>(name, static e => e.Value, text: text is null ? null : () => text.Value, jsonName: jsonName);
        HasPropertyInternal(_metadata, configure, getNullableValue, isNullableValueDefault);
    }

    /// <inheritdoc/>
    public Task<IValidationResult<ValidationValue<T>>> ValidateAsync(CancellationToken cancellationToken = default) => ValidateAsync(null, cancellationToken);

    /// <inheritdoc/>
    public async Task<IValidationResult<ValidationValue<T>>> ValidateAsync(ValidationArgs? args, CancellationToken cancellationToken = default)
        => await ValidateAsync(_validationValue, args ?? new ValidationArgs { FullyQualifiedEntityName = _metadata.Name, FullyQualifiedJsonEntityName = _metadata.JsonName }, cancellationToken).ConfigureAwait(false);
}