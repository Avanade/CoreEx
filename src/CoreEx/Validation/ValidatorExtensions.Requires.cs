namespace CoreEx.Validation;

public static partial class ValidatorExtensions
{
    /// <summary>
    /// Validates (requires) that the <paramref name="value"/> is non-default and continues; otherwise, will throw a <see cref="ValidationException"/>.
    /// </summary>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="name">The value name.</param>
    /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
    /// <returns>The value where non-default.</returns>
    /// <exception cref="ValidationException">Thrown where the value is default.</exception>
    [return: NotNull()]
    public static T Required<T>(this T value, [CallerArgumentExpression(nameof(value))] string? name = null, LText? text = null)
        => (Comparer<T?>.Default.Compare(value, default!) == 0) ? throw Validation.CreateRequiredValueResult<Result>(name, text).Error : value!;

    /// <summary>
    /// Requires (validates) that the <paramref name="value"/> is non-default and continues; otherwise, will return the <paramref name="result"/> with a corresponding <see cref="ValidationException"/>.
    /// </summary>
    /// <typeparam name="TResult">The <see cref="Result"/> or <see cref="Result{T}"/> (see <see cref="IResult"/>) <see cref="Type"/>.</typeparam>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result"/> or <see cref="Result{T}"/> (see <see cref="IResult"/>) instance.</param>
    /// <param name="value">The function to return the value to validate is required.</param>
    /// <param name="name">The value name.</param>
    /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
    /// <returns>The resulting <see cref="IResult"/></returns>
    public static TResult Requires<TResult, T>(this TResult result, Func<T> value, string name, LText? text = null) where TResult : IResult, new()
    {
        value.ThrowIfNull();
        name.ThrowIfNullOrEmpty();

        return result.IsSuccess && Comparer<T>.Default.Compare(value(), default!) == 0 ? Validation.CreateRequiredValueResult<TResult>(name, text) : result;
    }

    /// <summary>
    /// Requires (validates) that the <paramref name="value"/> is non-default and continues; otherwise, will return the <paramref name="result"/> with a corresponding <see cref="ValidationException"/>.
    /// </summary>
    /// <typeparam name="TResult">The <see cref="Result"/> or <see cref="Result{T}"/> (see <see cref="IResult"/>) <see cref="Type"/>.</typeparam>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="result">The <see cref="Result"/> or <see cref="Result{T}"/> (see <see cref="IResult"/>) instance.</param>
    /// <param name="value">The value to validate is required.</param>
    /// <param name="name">The value name (defaults to <paramref name="value"/> caller argument expression).</param>
    /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
    /// <returns>The resulting <see cref="IResult"/></returns>
    public static TResult Requires<TResult, T>(this TResult result, T value, [CallerArgumentExpression(nameof(value))] string? name = null, LText? text = null) where TResult : IResult, new()
        => result.Requires(() => value, name ?? Validation.ValueName, text);
}