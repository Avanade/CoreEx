namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides a mandatory validation rule; determined as mandatory when is <see langword="null"/> or it equals its default/empty state.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
/// <param name="mustNotBeDefault">Indicates that a validation error should occur when the value is <see langword="default"/>.</param>
/// <param name="mustNotBeEmpty">Indicates that a validation error should occur when the value is considered empty.</param>
/// <remarks>A value will be determined as mandatory when it equals its <see langword="default"/>. For example an <see cref="int"/> will error when the value is zero; however, a
/// <see cref="Nullable{Int32}"/> will error only when <see langword="null"/>. A zero is considered non-<see langword="default"/> and will succeed, unless <paramref name="mustNotBeDefault"/> is used.
/// <para>For a <see cref="string"/> mandatory is determined by <see cref="string.Length"/> being zero or <see cref="string.IsNullOrWhiteSpace(string?)"/> resulting in <see langword="true"/>.</para>
/// <para>Finally, a <see cref="ICollection"/> and <see cref="IEnumerable"/> mandatory is determined by whether they contain any items.</para></remarks>
public sealed class MandatoryRule<TEntity, TProperty>(Func<PropertyContext<TEntity, TProperty>, bool> mustNotBeDefault, Func<PropertyContext<TEntity, TProperty>, bool> mustNotBeEmpty) : PropertyRuleBase<TEntity, TProperty> where TEntity : class
{
    private readonly Func<PropertyContext<TEntity, TProperty>, bool> _mustNotBeDefault = mustNotBeDefault.ThrowIfNull();
    private readonly Func<PropertyContext<TEntity, TProperty>, bool> _mustNotBeEmpty = mustNotBeEmpty.ThrowIfNull();

    /// <inheritdoc/>
    protected override bool ValidateWhenNull => true;

    /// <inheritdoc/>
    protected override Task OnValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken)
    {
        // Check for null.
        if (context.IsValueNull)
            return AddError(context);

        var mustNotBeDefault = _mustNotBeDefault(context);
        var mustNotBeEmpty = _mustNotBeEmpty(context);

        // Compare the value against its default.
        if (mustNotBeDefault && context.IsValueNullable && context.IsNullableValueDefault())
            return AddError(context);

        if (mustNotBeDefault && !context.IsValueNullable && Comparer<TProperty>.Default.Compare(context.Value, default) == 0) 
            return AddError(context);

        if (!mustNotBeEmpty)
            return Task.CompletedTask;

        // Also check for empty strings.
        if (context.Value is string val)
            return val is null || val.Length == 0 || string.IsNullOrWhiteSpace(val) ? AddError(context) : Task.CompletedTask;

        // Also check for empty collections.
        if (context.Value is ICollection coll && coll.Count == 0)
            return coll.Count == 0 ? AddError(context) : Task.CompletedTask;

        // Also check for empty enumerables.
        if (context.Value is IEnumerable enumerable)
        {
            var enumerator = enumerable.GetEnumerator();
            if (!enumerator.MoveNext())
                return AddError(context);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Create the error message.
    /// </summary>
    private Task AddError(PropertyContext<TEntity, TProperty> context)
    {
        context.AddError(ErrorText ?? ValidatorStrings.MandatoryFormat);
        return Task.CompletedTask;
    }
}