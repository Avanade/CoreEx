namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides the root <see cref="IPropertyRuleEx{TEntity, TProperty}"/> capabilities.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="System.Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="System.Type"/>.</typeparam>
public sealed class RootPropertyRule<TEntity, TProperty> : IPropertyRuleEx<TEntity, TProperty>, IRootPropertyRule<TEntity, TProperty> where TEntity : class
{
    private readonly Func<TEntity, TProperty>? _getNullableValue;
    private readonly Func<TEntity, bool>? _isNullableValueDefault;
    private List<IPropertyClause<TEntity>>? _clauses;
    private IPropertyRule<TEntity>? _chainedRule;

    /// <summary>
    /// Initializes a new instance of the <see cref="RootPropertyRule{TEntity, TProperty}"/> class.
    /// </summary>
    /// <param name="metadata">The <see cref="IPropertyRuntimeMetadata"/>.</param>
    /// <param name="getNullableValue">A function to get the underlying nullable value.</param>
    /// <param name="isNullableValueDefault">A function to determine whether the underlying nullable value is its default.</param>
    internal RootPropertyRule(IPropertyRuntimeMetadata metadata, Func<TEntity, TProperty>? getNullableValue, Func<TEntity, bool>? isNullableValueDefault)
    {
        Metadata = metadata.ThrowIfNull();
        _getNullableValue = getNullableValue;
        _isNullableValueDefault = isNullableValueDefault;

        if (metadata.Format is not null)
            ValueFormatter = new(metadata.Format);
    }

    /// <summary>
    /// Gets the <see cref="IPropertyRuntimeMetadata"/>.
    /// </summary>
    public IPropertyRuntimeMetadata Metadata { get; }

    /// <inheritdoc/>
    public bool IsValueNullable => _getNullableValue is not null;

    /// <summary>
    /// Gets the property text override (where set); otherwise, <see langword="null"/>.
    /// </summary>
    public LText? Text { get; private set; }

    /// <inheritdoc/>
    public void SetText(LText? text) => Text = text;

    /// <summary>
    /// Gets the <see cref="Abstractions.ValueFormatter"/> to use when localizing the property value within an error message.
    /// </summary>
    public ValueFormatter ValueFormatter { get; private set; } = ValueFormatter.Default;

    /// <inheritdoc/>
    void IRootPropertyRule<TEntity>.SetFormat(string? format, IFormatProvider? formatProvider, char? quotingCharacter) => ValueFormatter = new(format, formatProvider, quotingCharacter);

    /// <inheritdoc/>
    /// <remarks>Not supported for a <see cref="RootPropertyRule{TEntity, TProperty}"/>; will throw a <see cref="NotSupportedException"/>.</remarks>
    public LText? ErrorText 
    { 
        get => throw new NotSupportedException($"A {GetType().Name} does not support {nameof(ErrorText)}."); 
        set => throw new NotSupportedException($"A {GetType().Name} does not support {nameof(ErrorText)}.");
    }

    /// <inheritdoc/>
    /// <remarks>Not supported for a <see cref="RootPropertyRule{TEntity, TProperty}"/>; will throw a <see cref="NotSupportedException"/>.</remarks>
    IPropertyRule<TEntity, TProperty> IPropertyRule<TEntity, TProperty>.WithMessage(LText errorText) => throw new NotSupportedException($"A {GetType().Name} does not support {nameof(IPropertyRule<,>.WithMessage)}.");

    /// <summary>
    /// Gets the originating property <see cref="Nullable{T}.Value"/> or <see langword="default"/> (where <see cref="IsValueNullable"/>).
    /// </summary>
    /// <typeparam name="T">The property <see cref="Nullable{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <returns>The originating property <see cref="Nullable{T}.Value"/> or <see langword="default"/>.</returns>
    public T GetNullableValueOrDefault<T>(TEntity entity) => IsValueNullable ? Internal.Cast<TProperty?, T>(_getNullableValue!(entity)) : throw new InvalidOperationException("The property must be Nullable<T>.");

    /// <summary>
    /// Indicates whether the originating property <see cref="Nullable{T}.Value"/> is <see langword="default"/> (where <see cref="IsValueNullable"/>).
    /// </summary>
    public bool IsNullableValueDefault(TEntity entity) => IsValueNullable ? _isNullableValueDefault!(entity) : throw new InvalidOperationException("The property must be Nullable<T>.");

    /// <inheritdoc/>
    void IPropertyRule<TEntity>.AddClause(IPropertyClause<TEntity> clause) => (_clauses ??= []).Add(clause.ThrowIfNull());

    /// <inheritdoc/>
    void IPropertyRule<TEntity>.Chain(IPropertyRule<TEntity> rule)
    {
        if (_chainedRule is not null)
            throw new InvalidOperationException("A rule can only support a single chained rule.");

        _chainedRule = rule.ThrowIfNull();
    }

    /// <summary>
    /// Checks the clauses.
    /// </summary>
    /// <param name="context">The <see cref="IPropertyContext"/>.</param>
    /// <param name="clauses">The <see cref="IPropertyClause{TEntity}"/> <see cref="List{T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> where validation is to continue; otherwise, <see langword="false"/> to stop.</returns>
    internal static async Task<bool> CheckClausesAsync(PropertyContext<TEntity, TProperty> context, List<IPropertyClause<TEntity>>? clauses, CancellationToken cancellationToken)
    {
        if (clauses is not null)
        {
            foreach (var clause in clauses)
            {
                var cr = await clause.CheckAsync(context, cancellationToken).ConfigureAwait(false);
                if (!cr)
                    return cr;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    async Task IPropertyRule<TEntity>.ValidateAsync(IPropertyContext<TEntity> context, CancellationToken cancellationToken)
        => await ValidateAsync((PropertyContext<TEntity, TProperty>)context, cancellationToken);

    /// <inheritdoc/>
    public async Task ValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken)
    {
        if (_chainedRule is not null)
            await _chainedRule.ValidateAsync(context, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ValidateAsync(ValidationContext<TEntity> context, CancellationToken cancellationToken)
    {
        // Check that there are no pre-existing errors for the property.
        if (context.HasError<TProperty>(Metadata))
            return;

        // Create the property context.
        var pc = new PropertyContext<TEntity, TProperty>(this, context);

        // Check the clauses.
        var cr = await CheckClausesAsync(pc, _clauses, cancellationToken).ConfigureAwait(false);
        if (!cr)
            return;

        // Perform the validation.
        await ValidateAsync(pc, cancellationToken).ConfigureAwait(false);
    }
}