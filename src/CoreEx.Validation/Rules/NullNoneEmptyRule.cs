namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides a <paramref name="mustBeNull"/>, <paramref name="mustBeDefault"/> and <paramref name="mustBeEmpty"/> validation.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
/// <param name="mustBeNull">Indicates that a validation error should occur when the value is <i>not</i> <see langword="null"/>.</param>
/// <param name="mustBeDefault">Indicates that a validation error should occur when the value is <i>not</i> <see langword="default"/>.</param>
/// <param name="mustBeEmpty">Indicates that a validation error should occur when the value is considered <i>not</i> empty.</param>
public sealed class NullNoneEmptyRule<TEntity, TProperty>(Func<PropertyContext<TEntity, TProperty>, bool> mustBeNull, Func<PropertyContext<TEntity, TProperty>, bool> mustBeDefault, Func<PropertyContext<TEntity, TProperty>, bool> mustBeEmpty) : PropertyRuleBase<TEntity, TProperty> where TEntity : class
{
    private readonly Func<PropertyContext<TEntity, TProperty>, bool> _mustBeNull = mustBeNull.ThrowIfNull();
    private readonly Func<PropertyContext<TEntity, TProperty>, bool> _mustBeDefault = mustBeDefault.ThrowIfNull();
    private readonly Func<PropertyContext<TEntity, TProperty>, bool> _mustBeEmpty = mustBeEmpty.ThrowIfNull();

    /// <inheritdoc/>
    protected override bool ValidateWhenNull => true;

    /// <inheritdoc/>
    protected override Task OnValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken)
    {
        var mustBeNull = _mustBeNull(context);
        var mustBeDefault = _mustBeDefault(context);
        var mustBeEmpty = _mustBeEmpty(context);

        // Check if must be null.
        if (mustBeNull && !context.IsValueNull)
            return AddError(context);

        // Compare the value against its default.
        if (mustBeDefault && context.IsValueNullable && !context.IsNullableValueDefault())
            return AddError(context);

        if (mustBeDefault && !context.IsValueNullable && Comparer<TProperty>.Default.Compare(context.Value, default) != 0)
            return AddError(context);

        if (!mustBeEmpty)
            return Task.CompletedTask;

        // Also check for empty strings.
        if (context.Value is string val)
            return val is null || val.Length == 0 || string.IsNullOrWhiteSpace(val) ? Task.CompletedTask : AddError(context);

        if (context.Metadata.Type == typeof(string) && context.IsValueNull)
            return Task.CompletedTask;

        // Also check for empty collections.
        if (context.Value is ICollection coll)
            return coll.Count == 0 ? Task.CompletedTask : AddError(context);

        // Also check for empty enumerables.
        if (context.Value is IEnumerable enumerable)
        {
            var enumerator = enumerable.GetEnumerator();
            return !enumerator.MoveNext() ? Task.CompletedTask : AddError(context);
        }

        return context.IsValueNull ? Task.CompletedTask : AddError(context);
    }

    /// <summary>
    /// Create the error message.
    /// </summary>
    private Task AddError(PropertyContext<TEntity, TProperty> context)
    {
        context.AddError(ErrorText ?? ValidatorStrings.NoneFormat);
        return Task.CompletedTask;
    }
}