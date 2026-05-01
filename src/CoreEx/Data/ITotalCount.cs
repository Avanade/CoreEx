namespace CoreEx.Data;

/// <summary>
/// Enables the total count capabilities for a sequence.
/// </summary>
public interface ITotalCount
{
    /// <summary>
    /// Indicates whether to get the total count (see <see cref="PagingResult.TotalCount"/>) when performing the underlying query (defaults to <see langword="false"/>).
    /// </summary>
    /// <remarks>This may result in a secondary query and therefore impact overall performance; this should be used judiciously.
    /// <para>There are no guarantees that a count will be performed, as this will depend on the underlying implementation; hence, it is simply a request.</para></remarks>
    public bool IsCountRequested { get; }

    /// <summary>
    /// Gets the total count of the elements in the sequence.
    /// </summary>
    /// <remarks>A <see langword="null"/> value indicates that the total count is unknown.</remarks>
    public long? TotalCount { get; }

    /// <summary>
    /// Sets the total count of the elements in the sequence.
    /// </summary>
    /// <param name="totalCount">The total count of the elements in the sequence.</param>
    /// <remarks>A <see langword="null"/> or negative value indicates that the total count is unknown.
    /// <para>The <see cref="TotalCount"/> is only set when <see cref="IsCountRequested"/> is <see langword="true"/>.</para></remarks>
    void WithTotalCount(long? totalCount);
}