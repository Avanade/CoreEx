namespace CoreEx;

/// <summary>
/// Provides standard extensions.
/// </summary>
public static partial class Extensions
{
    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the <paramref name="value"/> is <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="value">The value to validate as non-null.</param>
    /// <param name="paramName">The name of the parameter with which the <paramref name="value"/> corresponds.</param>
    /// <returns>The <paramref name="value"/> to support fluent-style method-chaining.</returns>
    [DebuggerStepThrough]
    [return: NotNull]
    public static T ThrowIfNull<T>([NotNull] this T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        return value;
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the <paramref name="value"/> is <see langword="null"/> or <see cref="string.Empty"/>.
    /// </summary>
    /// <param name="value">The value to validate as non-null.</param>
    /// <param name="paramName">The name of the parameter with which the <paramref name="value"/> corresponds.</param>
    /// <returns>The <paramref name="value"/> to support fluent-style method-chaining.</returns>
    [DebuggerStepThrough]
    [return: NotNull]
    public static string ThrowIfNullOrEmpty([NotNull] this string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(value, paramName);
        return value;
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the <paramref name="value"/> is <see cref="string.Empty"/>.
    /// </summary>
    /// <param name="value">The value to validate as non-null.</param>
    /// <param name="paramName">The name of the parameter with which the <paramref name="value"/> corresponds.</param>
    /// <returns>The <paramref name="value"/> to support fluent-style method-chaining.</returns>
    [DebuggerStepThrough]
    [return: NotNullIfNotNull(nameof(value))]
    public static string? ThrowIfEmpty(this string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is not null && value.Length == 0)
            throw new ArgumentException("The value cannot be an empty string.", paramName);

        return value;
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> <i>when</i> the <paramref name="predicate"/> execution results in <see langword="true"/>.
    /// </summary>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="predicate">The predicate.</param>
    /// <param name="message">The error message; defaults to the predicate expression.</param>
    /// <param name="paramName">The name of the parameter with which the <paramref name="value"/> corresponds.</param>
    /// <returns>The <paramref name="value"/> to support fluent-style method-chaining.</returns>
    [DebuggerStepThrough]
    [return: NotNullIfNotNull(nameof(value))]
    public static T ThrowWhen<T>(this T value, Func<T, bool> predicate, [CallerArgumentExpression(nameof(predicate))] string? message = null, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (predicate(value))
            throw new ArgumentException(message, paramName);

        return value;
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> <i>when</i> the <paramref name="value"/> is less than zero.
    /// </summary>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="message">The optional message to include in the exception if the condition is met.</param>
    /// <param name="paramName">The name of the parameter with which the <paramref name="value"/> corresponds.</param>
    /// <returns>The <paramref name="value"/> to support fluent-style method-chaining.</returns>
    [DebuggerStepThrough]
    public static T ThrowIfLessThanZero<T>(this T value, string? message = null, [CallerArgumentExpression(nameof(value))] string? paramName = null) where T : System.Numerics.INumber<T>
        => (value < T.Zero) ? throw new ArgumentOutOfRangeException(paramName, message.ThrowIfEmpty() ?? "The value cannot be less than zero.") : value;

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> <i>when</i> the <paramref name="value"/> is less than or equal to zero.
    /// </summary>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="message">The optional message to include in the exception if the condition is met.</param>
    /// <param name="paramName">The name of the parameter with which the <paramref name="value"/> corresponds.</param>
    /// <returns>The <paramref name="value"/> to support fluent-style method-chaining.</returns>
    [DebuggerStepThrough]
    public static T ThrowIfLessThanOrEqualToZero<T>(this T value, string? message = null, [CallerArgumentExpression(nameof(value))] string? paramName = null) where T : System.Numerics.INumber<T>
        => (value <= T.Zero) ? throw new ArgumentOutOfRangeException(paramName, message.ThrowIfEmpty() ?? "The value cannot be less than or equal to zero.") : value;

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> <i>when</i> the <paramref name="value"/> is greater than zero.
    /// </summary>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="message">The optional message to include in the exception if the condition is met.</param>
    /// <param name="paramName">The name of the parameter with which the <paramref name="value"/> corresponds.</param>
    /// <returns>The <paramref name="value"/> to support fluent-style method-chaining.</returns>
    [DebuggerStepThrough]
    public static T ThrowIfGreaterThanZero<T>(this T value, string? message = null, [CallerArgumentExpression(nameof(value))] string? paramName = null) where T : System.Numerics.INumber<T>
        => (value > T.Zero) ? throw new ArgumentOutOfRangeException(paramName, message.ThrowIfEmpty() ?? "The value cannot be greater than zero.") : value;

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> <i>when</i> the <paramref name="value"/> is greater than or equal to zero.
    /// </summary>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="message">The optional message to include in the exception if the condition is met.</param>
    /// <param name="paramName">The name of the parameter with which the <paramref name="value"/> corresponds.</param>
    /// <returns>The <paramref name="value"/> to support fluent-style method-chaining.</returns>
    [DebuggerStepThrough]
    public static T ThrowIfGreaterThanOrEqualToZero<T>(this T value, string? message = null, [CallerArgumentExpression(nameof(value))] string? paramName = null) where T : System.Numerics.INumber<T>
        => (value >= T.Zero) ? throw new ArgumentOutOfRangeException(paramName, message.ThrowIfEmpty() ?? "The value cannot be greater than or equal to zero.") : value;

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> <i>when</i> the <paramref name="value"/> is equal to zero.
    /// </summary>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="message">The optional message to include in the exception if the condition is met.</param>
    /// <param name="paramName">The name of the parameter with which the <paramref name="value"/> corresponds.</param>
    /// <returns>The <paramref name="value"/> to support fluent-style method-chaining.</returns>
    [DebuggerStepThrough]
    public static T ThrowIfEqualToZero<T>(this T value, string? message = null, [CallerArgumentExpression(nameof(value))] string? paramName = null) where T : System.Numerics.INumber<T>
        => (value == T.Zero) ? throw new ArgumentOutOfRangeException(paramName, message.ThrowIfEmpty() ?? "The value cannot be equal to zero.") : value;

    /// <summary>
    /// Enables adjustment (changes) to a <paramref name="value"/> via an <paramref name="adjuster"/> action.
    /// </summary>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="value">The value to adjust.</param>
    /// <param name="adjuster">The adjusting action (invoked only where the <paramref name="value"/> is not <see langword="null"/>).</param>
    /// <returns>The adjusted value (same instance).</returns>
    /// <remarks>Useful in scenarios to in-line simple changes to a value to simplify code.</remarks>
    [DebuggerStepThrough]
    [return: NotNullIfNotNull(nameof(value))]
    public static T? Adjust<T>(this T? value, Action<T>? adjuster)
    {
        if (value is not null)
            adjuster?.Invoke(value);

        return value!;
    }

    /// <summary>
    /// Enables adjustment (changes) to a <paramref name="value"/> via an <paramref name="adjuster"/> action when the <paramref name="predicate"/> is <see langword="true"/>.
    /// </summary>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="value">The value to adjust.</param>
    /// <param name="predicate">The <see cref="Predicate{T}"/> that determines whether the <paramref name="predicate"/> is invoked.</param>
    /// <param name="adjuster">The adjusting action (invoked only where the <paramref name="value"/> is not <see langword="null"/> and the <paramref name="predicate"/> results in <see langword="true"/>).</param>
    /// <returns>The adjusted value (same instance).</returns>
    /// <remarks>Useful in scenarios to in-line simple changes to a value to simplify code.</remarks>
    [DebuggerStepThrough]
    [return: NotNullIfNotNull(nameof(value))]
    public static T? AdjustWhen<T>(this T? value, Predicate<T> predicate, Action<T> adjuster)
    {
        if (value is not null && predicate(value))
            adjuster?.Invoke(value);

        return value!;
    }

    /// <summary>
    /// Converts a <see cref="string"/> into sentence case.
    /// </summary>
    /// <param name="text">The text to convert.</param>
    /// <returns>The <see cref="string"/> as sentence case.</returns>
    /// <remarks>For example a value of '<c>VarNameDB</c>' would return '<c>Var name DB</c>'.
    /// <para>Uses <see cref="SentenceCase.ToSentenceCase(string?)"/> to perform the conversion.</para></remarks>
    [DebuggerStepThrough]
    [return: NotNullIfNotNull(nameof(text))]
    public static string? ToSentenceCase(this string? text) => SentenceCase.ToSentenceCase(text);

    /// <summary>
    /// Indicates whether the exception is <see cref="OperationCanceledException"/> (including where wrapped by one or more nested <see cref="AggregateException"/> <see cref="Exception.InnerException"/> levels).
    /// </summary>
    /// <param name="ex">The <see cref="Exception"/>.</param>
    /// <returns><see langword="true"/> indicates canceled; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Nested wrapping occurs where a cancellation passes through multiple <c>Result.ThrowOnError()</c> boundaries, each of which wraps the error in a new <see cref="AggregateException"/>.</remarks>
    [DebuggerStepThrough]
    public static bool IsCanceled(this Exception ex) => ex is OperationCanceledException || (ex is AggregateException aex && aex.InnerException is not null && aex.InnerException.IsCanceled());

    /// <summary>
    /// Indicates whether the exception is <see cref="OperationCanceledException"/> (including where wrapped by one or more nested <see cref="AggregateException"/> <see cref="Exception.InnerException"/> levels) that is
    /// attributable specifically to the <paramref name="cancellationToken"/>.
    /// </summary>
    /// <param name="ex">The <see cref="Exception"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the cancellation must be attributable to.</param>
    /// <returns><see langword="true"/> indicates canceled by the <paramref name="cancellationToken"/>; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Unlike <see cref="IsCanceled(Exception)"/> (which matches <i>any</i> cancellation regardless of source), this distinguishes an operation's own cancellation (e.g. a host shutdown or client disconnect, which
    /// should typically bubble up unclassified) from an unrelated cancellation elsewhere in the call stack (e.g. an inner <see cref="System.Net.Http.HttpClient"/> timeout using its own <see cref="CancellationTokenSource"/>).</remarks>
    [DebuggerStepThrough]
    public static bool IsCanceledBy(this Exception ex, CancellationToken cancellationToken)
        => (ex is OperationCanceledException oce && oce.CancellationToken == cancellationToken) || (ex is AggregateException aex && aex.InnerException is not null && aex.InnerException.IsCanceledBy(cancellationToken));
}