namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides a comparison validation between two values.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
/// <param name="min">The function to get the minimum value.</param>
/// <param name="max">The function to get the maximum value.</param>
/// <param name="minText">The minimum text formatter (used in the error message); otherwise, uses the resulting <paramref name="min"/> value.</param>
/// <param name="maxText">The maximum text formatter (used in the error message); otherwise, uses the resulting <paramref name="max"/> value.</param>
/// <param name="exclusiveBetween">Indicates whether the between comparison is exclusive or inclusive (default).</param>
/// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
public sealed class BetweenRule<TEntity, TProperty>(Func<PropertyContext<TEntity, TProperty>, TProperty> min, Func<PropertyContext<TEntity, TProperty>, TProperty> max, Func<TProperty, LText?>? minText = null, Func<TProperty, LText?>? maxText = null, bool exclusiveBetween = false, IComparer<TProperty>? comparer = null) 
    : PropertyRuleBase<TEntity, TProperty> where TEntity : class where TProperty : IComparable<TProperty>
{
    private readonly Func<PropertyContext<TEntity, TProperty>, TProperty> _min = min.ThrowIfNull();
    private readonly Func<PropertyContext<TEntity, TProperty>, TProperty> _max = max.ThrowIfNull();
    private readonly Func<TProperty, LText?>? _minText = minText;
    private readonly Func<TProperty, LText?>? _maxText= maxText;
    private readonly bool _exclusiveBetween = exclusiveBetween;

    /// <summary>
    /// Gets the <see cref="IComparer{T}"/>.
    /// </summary>
    public IComparer<TProperty> Comparer { get; } = comparer ?? Comparer<TProperty>.Default;

    /// <inheritdoc/>
    protected override Task OnValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken)
    {
        // Get the min and max values.
        var min = _min(context);
        var max = _max(context);

        // Compare the values.
        if ((_exclusiveBetween && (Comparer.Compare(context.Value, min) <= 0 || Comparer.Compare(context.Value, max) >= 0))
            || (!_exclusiveBetween && (Comparer.Compare(context.Value, min) < 0 || Comparer.Compare(context.Value, max) > 0)))
        {
            context.AddError(ErrorText ?? (_exclusiveBetween ? ValidatorStrings.BetweenExclusiveFormat : ValidatorStrings.BetweenInclusiveFormat),
                _minText?.Invoke(min) ?? context.FormatValue(min),
                _maxText?.Invoke(max) ?? context.FormatValue(max));
        }

        return Task.CompletedTask;
    }
}