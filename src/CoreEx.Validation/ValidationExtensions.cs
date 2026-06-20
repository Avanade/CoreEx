namespace CoreEx.Validation;

/// <summary>
/// Provides standard extensions.
/// </summary>
public static partial class ValidationExtensions
{
    /// <summary>
    /// Adds a <paramref name="clause"/> to the preceding <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="clause">The <see cref="IPropertyClause{TEntity, TProperty}"/> to add.</param>
    /// <returns>The <paramref name="rule"/> to support fluent-style method-chaining.</returns>
    public static IPropertyRule<TEntity, TProperty> AddClause<TEntity, TProperty>(IPropertyRule<TEntity, TProperty> rule, IPropertyClause<TEntity, TProperty> clause) where TEntity : class
    {
        rule.AddClause(clause.ThrowIfNull());
        return rule;
    }

    /// <summary>
    /// Chains the current <paramref name="rule"/> with the <paramref name="nextRule"/>, creating a chained sequence of rules.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="nextRule">The next <see cref="IPropertyRule{TEntity, TProperty}"/> in the chain.</param>
    /// <returns>The <paramref name="nextRule"/> to support fluent-style method-chaining.</returns>
    public static IPropertyRule<TEntity, TProperty> Chain<TEntity, TProperty>(IPropertyRule<TEntity, TProperty> rule, IPropertyRule<TEntity, TProperty> nextRule) where TEntity : class
    {
        rule.ThrowIfNull().Chain(nextRule.ThrowIfNull());
        return nextRule;
    }

    /// <summary>
    /// Chains the current <paramref name="rule"/> with the <paramref name="nextRule"/>, creating a chained sequence of rules.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="nextRule">The next <see cref="IPropertyRule{TEntity, TProperty}"/> in the chain.</param>
    /// <returns>The <paramref name="nextRule"/> to support fluent-style method-chaining.</returns>
    public static IPropertyRule<TEntity, TProperty> Chain<TEntity, TProperty>(IPropertyRule<TEntity, TProperty?> rule, IPropertyRule<TEntity, TProperty> nextRule) where TEntity : class where TProperty : struct
    {
        rule.ThrowIfNull().Chain(nextRule.ThrowIfNull());
        return nextRule;
    }

    /// <summary>
    /// Sets (overrides) the property <paramref name="text"/> to be used within validation messages.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IRootPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="text">The property <see cref="LText"/>.</param>
    /// <returns>The <paramref name="rule"/> to support fluent-style method-chaining.</returns>
    public static IRootPropertyRule<TEntity, TProperty> WithText<TEntity, TProperty>(this IRootPropertyRule<TEntity, TProperty> rule, LText? text) where TEntity : class
    {
        rule.ThrowIfNull().SetText(text);
        return rule;
    }

    /// <summary>
    /// Sets (overrides) the property <paramref name="text"/> to be used within validation messages.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IRootPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="text">The property <see cref="LText"/>.</param>
    /// <returns>The <paramref name="rule"/> to support fluent-style method-chaining.</returns>
    public static IRootPropertyRule<TEntity, TProperty?> WithText<TEntity, TProperty>(this IRootPropertyRule<TEntity, TProperty?> rule, LText? text) where TEntity : class where TProperty : struct
    {
        rule.ThrowIfNull().SetText(text);
        return rule;
    }

    /// <summary>
    /// Sets (overrides) the format and optional format provider to be used when formatting the property value within validation messages.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IRootPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="format">The format.</param>
    /// <param name="formatProvider">The optional <see cref="IFormatProvider"/></param>
    /// <param name="quotingCharacter">The quoting character so it appears as a literal string.</param>
    /// <returns>The <paramref name="rule"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The underlying <typeparamref name="TProperty"/> type must implement <see cref="IFormattable"/> as this results in <see cref="IFormattable.ToString(string?, IFormatProvider?)"/> being used.</remarks>
    public static IRootPropertyRule<TEntity, TProperty> WithFormat<TEntity, TProperty>(this IRootPropertyRule<TEntity, TProperty> rule, string? format, IFormatProvider? formatProvider = null, char? quotingCharacter = '\'') where TEntity : class where TProperty : IFormattable
    {
        rule.ThrowIfNull().SetFormat(format, formatProvider, quotingCharacter);
        return rule;
    }

    /// <summary>
    /// Sets (overrides) the format and optional format provider to be used when formatting the property value within validation messages.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IRootPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="format">The format.</param>
    /// <param name="formatProvider">The optional <see cref="IFormatProvider"/></param>
    /// <param name="quotingCharacter">The quoting character so it appears as a literal string.</param>
    /// <returns>The <paramref name="rule"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The underlying <typeparamref name="TProperty"/> type must implement <see cref="IFormattable"/> as this results in <see cref="IFormattable.ToString(string?, IFormatProvider?)"/> being used.</remarks>
    public static IRootPropertyRule<TEntity, TProperty?> WithFormat<TEntity, TProperty>(this IRootPropertyRule<TEntity, TProperty?> rule, string? format, IFormatProvider? formatProvider = null, char? quotingCharacter = '\'') where TEntity : class where TProperty : struct, IFormattable
    {
        rule.ThrowIfNull().SetFormat(format, formatProvider, quotingCharacter);
        return rule;
    }

    /// <summary>
    /// Includes the specified <paramref name="baseValidator"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TSelf">The <see cref="ValidatorBase{TEntity, TSelf}"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TInclude">The included base entity <see cref="Type"/>.</typeparam>
    /// <param name="validator">The <see cref="ValidatorBase{TEntity, TSelf}"/>.</param>
    /// <param name="baseValidator">The base <see cref="IValidatorEx{T}"/>.</param>
    /// <returns>The <paramref name="validator"/> to support fluent-style method-chaining.</returns>
    /// <remarks><i>Note:</i> the <paramref name="baseValidator"/> is added internally as an <see cref="IncludeBaseRule{TEntity}"/> rule; therefore, it will be executed in the order added in relation to other property-base rules.</remarks>
    public static TSelf Include<TEntity, TSelf, TInclude>(this ValidatorBase<TEntity, TSelf> validator, IValidatorEx<TInclude> baseValidator) where TEntity : class, TInclude where TInclude : class where TSelf : ValidatorBase<TEntity, TSelf>
        => validator.ThrowIfNull().IncludeBase(baseValidator.ThrowIfNull());

    /// <summary>
    /// Creates an <see cref="IValueValidator{T}"/> to enable validation of the specified <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="configure">The action to configure the resulting <see cref="IPropertyRule{TEntity, TProperty}"/>.</param>
    /// <param name="name">The value name (defaults to <paramref name="value"/> name using the caller argument expression).</param>
    /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
    /// <param name="jsonName">The JSON name where different to the name and using <see cref="ValidationArgs.UseJsonNames"/>.</param>
    /// <returns>The <see cref="IValueValidator{T}"/>.</returns>
    /// <remarks>The <paramref name="configure"/> should be used to further configure the validation rules, clauses, etc. 
    /// <para>Finally, the <see cref="IValueValidator{T}.ValidateAsync(CancellationToken)"/> or <see cref="IValueValidator{T}.ValidateAsync(ValidationArgs?, CancellationToken)"/>, should be invoked to execute the underlying validation.</para></remarks>
    public static IValueValidator<T> Validator<T>(this T? value, Action<IPropertyRule<ValidationValue<T>, T>>? configure, [CallerArgumentExpression(nameof(value))] string? name = null, LText? text = null, string? jsonName = null) where T : notnull
        => new ValueValidator<T>(value!, name ?? Validation.ValueName, jsonName, text, configure, null, null);

    /// <summary>
    /// Creates an <see cref="IValueValidator{T}"/> to enable validation of the specified <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="configure">The action to configure the resulting <see cref="IPropertyRule{TEntity, TProperty}"/>.</param>
    /// <param name="name">The value name (defaults to <paramref name="value"/> name using the caller argument expression).</param>
    /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
    /// <param name="jsonName">The JSON name where different to the name and using <see cref="ValidationArgs.UseJsonNames"/>.</param>
    /// <returns>The <see cref="IValueValidator{T}"/>.</returns>
    /// <remarks>The <paramref name="configure"/> should be used to further configure the validation rules, clauses, etc. 
    /// <para>Finally, the <see cref="IValueValidator{T}.ValidateAsync(CancellationToken)"/> or <see cref="IValueValidator{T}.ValidateAsync(ValidationArgs?, CancellationToken)"/>, should be invoked to execute the underlying validation.</para></remarks>
    public static IValueValidator<T?> Validator<T>(this T? value, Action<IPropertyRule<ValidationValue<T?>, T?>>? configure, [CallerArgumentExpression(nameof(value))] string? name = null, LText? text = null, string? jsonName = null) where T : struct
        => new ValueValidator<T?>(value, name ?? Validation.ValueName, jsonName, text, configure, e => e.Value.GetValueOrDefault(), e => Comparer<T>.Default.Compare(e.Value.GetValueOrDefault(), default) == 0);
}