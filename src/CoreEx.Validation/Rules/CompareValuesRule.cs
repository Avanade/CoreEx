namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides a comparison validation against one or more values.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
/// <param name="compareToValues">The compare-to value(s).</param>
/// <param name="comparer">The optional <see cref="IEqualityComparer{T}"/>.</param>
/// <param name="overrideValueWhereMatched">Indicates whether to override the underlying property value with the corresponding matched value.</param>
public sealed class CompareValuesRule<TEntity, TProperty>(Func<PropertyContext<TEntity, TProperty>, IEnumerable<TProperty>> compareToValues, IEqualityComparer<TProperty>? comparer = null, bool overrideValueWhereMatched = false)
    : PropertyRuleBase<TEntity, TProperty> where TEntity : class where TProperty : IEquatable<TProperty>
{
    private readonly Func<PropertyContext<TEntity, TProperty>, IEnumerable<TProperty>> _compareToValues = compareToValues.ThrowIfNull();
    private readonly IEqualityComparer<TProperty> _comparer = comparer ?? EqualityComparer<TProperty>.Default;
    private readonly bool _overrideValueWhereMatched = overrideValueWhereMatched;

    /// <inheritdoc/>
    protected override Task OnValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken)
    {
        // Get the compare to value(s).
        var compareToValues = _compareToValues(context);
        if (compareToValues is null)
            return Task.CompletedTask;

        // Perform the comparison (single enumeration to determine whether a match exists and, if so, which value matched).
        var matched = false;
        var match = default(TProperty)!;
        foreach (var v in compareToValues)
        {
            if (_comparer.Equals(context.Value, v))
            {
                matched = true;
                match = v;
                break;
            }
        }

        if (!matched)
        {
            context.AddError(ErrorText ?? ValidatorStrings.InvalidFormat);
            return Task.CompletedTask;
        }

        // Override the value where matched, is requested, and is different.
        if (_overrideValueWhereMatched && !EqualityComparer<TProperty>.Default.Equals(match, context.Value))
            context.Override(match);

        return Task.CompletedTask;
    }
}