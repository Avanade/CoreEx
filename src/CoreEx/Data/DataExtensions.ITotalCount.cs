namespace CoreEx.Data;

public static partial class DataExtensions
{
    /// <summary>
    /// Sets the <see cref="ITotalCount.TotalCount"/> of the elements in the sequence where <see cref="ITotalCount.IsCountRequested"/> is <see langword="true"/>.
    /// </summary>
    /// <typeparam name="TSource">The <paramref name="source"/> element <see cref="Type"/>.</typeparam>
    /// <param name="source">The <see cref="ITotalCount"/>.</param>
    /// <param name="totalCount">The total count of the elements in the sequence.</param>
    /// <returns>The <paramref name="source"/> to support fluent-style method-chaining.</returns>
    public static TSource WithTotalCount<TSource>(this TSource source, long totalCount) where TSource : ITotalCount => source.WithTotalCount(() => totalCount);

    /// <summary>
    /// Sets the <see cref="ITotalCount.TotalCount"/> of the elements in the sequence where <see cref="ITotalCount.IsCountRequested"/> is <see langword="true"/>.
    /// </summary>
    /// <typeparam name="TSource">The <paramref name="source"/> element <see cref="Type"/>.</typeparam>
    /// <param name="source">The <see cref="ITotalCount"/>.</param>
    /// <param name="totalCount">The function to determine the total count of the elements in the sequence.</param>
    /// <returns>The <paramref name="source"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="totalCount"/> function is wrapped in a try/catch, and will swallow and log any exception that occurs versus failing. The <see cref="PagingResult.TotalCount"/> in the event of an exception will
    /// be set to <see langword="null"/> indicating that the total count is unknown.</remarks>
    public static TSource WithTotalCount<TSource>(this TSource source, Func<long> totalCount) where TSource : ITotalCount
    {
        source.ThrowIfNull();
        if (!source.IsCountRequested || totalCount is null)
            return source;

        try
        {
            source.WithTotalCount(totalCount());
        }
        catch (Exception ex)
        {
            WithTotalCountException(ex);
        }

        return source;
    }

    /// <summary>
    /// Sets the <see cref="ITotalCount.TotalCount"/> of the elements in the sequence where <see cref="ITotalCount.IsCountRequested"/> is <see langword="true"/>.
    /// </summary>
    /// <typeparam name="TSource">The <paramref name="source"/> element <see cref="Type"/>.</typeparam>
    /// <param name="source">The <see cref="ITotalCount"/>.</param>
    /// <param name="totalCount">The function to determine the total count of the elements in the sequence.</param>
    /// <returns>The <paramref name="source"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="totalCount"/> function is wrapped in a try/catch, and will swallow and log any exception that occurs versus failing. The <see cref="PagingResult.TotalCount"/> in the event of an exception will
    /// be set to <see langword="null"/> indicating that the total count is unknown.</remarks>
    public static async Task<TSource> WithTotalCountAsync<TSource>(this TSource source, Func<Task<long?>> totalCount) where TSource : ITotalCount
        => await WithTotalCountAsync(source, async _ => await totalCount().ConfigureAwait(false), default).ConfigureAwait(false);

    /// <summary>
    /// Sets the <see cref="ITotalCount.TotalCount"/> of the elements in the sequence where <see cref="ITotalCount.IsCountRequested"/> is <see langword="true"/>.
    /// </summary>
    /// <typeparam name="TSource">The <paramref name="source"/> element <see cref="Type"/>.</typeparam>
    /// <param name="source">The <see cref="ITotalCount"/>.</param>
    /// <param name="totalCount">The function to determine the total count of the elements in the sequence.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <paramref name="source"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="totalCount"/> function is wrapped in a try/catch, and will swallow and log any exception that occurs versus failing. The <see cref="PagingResult.TotalCount"/> in the event of an exception will
    /// be set to <see langword="null"/> indicating that the total count is unknown.</remarks>
    public async static Task<TSource> WithTotalCountAsync<TSource>(this TSource source, Func<CancellationToken, Task<long?>> totalCount, CancellationToken cancellationToken = default) where TSource : ITotalCount
    {
        source.ThrowIfNull();
        if (!source.IsCountRequested || totalCount is null)
            return source;

        try
        {
            source.WithTotalCount(await totalCount(cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            WithTotalCountException(ex);
        }

        return source;
    }

    /// <summary>
    /// Common total count exception handling; i.e. logging the exception as a warning where possible.
    /// </summary>
    private static void WithTotalCountException(Exception ex)
    {
        var logger = ExecutionContext.GetService<ILoggerFactory>()?.CreateLogger(typeof(ITotalCount));
        if (logger is not null && logger.IsEnabled(LogLevel.Warning))
            logger.LogWarning(ex, "Unable to determine the total count of the elements in the sequence; the total count will not be returned as a result: {Message}.", ex.Message);
    }
}