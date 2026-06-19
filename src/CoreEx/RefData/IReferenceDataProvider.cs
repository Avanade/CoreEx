namespace CoreEx.RefData;

/// <summary>
/// Enables a means to manage and group one or more <see cref="IReferenceData"/> <see cref="Types"/> for use by the centralized <see cref="ReferenceDataOrchestrator"/>.
/// </summary>
public interface IReferenceDataProvider
{
    /// <summary>
    /// Gets all the underlying <see cref="IReferenceData"/> and corresponding <see cref="IReferenceDataCollection"/> <see cref="Type"></see> pairs provided.
    /// </summary>
    /// <returns>The <see cref="IReferenceData"/> and corresponding <see cref="IReferenceDataCollection"/> <see cref="Type"></see> pairs provided.</returns>
    IEnumerable<(Type, Type)> Types { get; }

    /// <summary>
    /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The corresponding <see cref="IReferenceDataCollection"/>.</returns>
    Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default);
}