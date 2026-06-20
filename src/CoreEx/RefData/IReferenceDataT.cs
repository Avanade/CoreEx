namespace CoreEx.RefData;

/// <summary>
/// Enables the core <b>Reference Data</b> properties with a typed <see cref="IReadOnlyIdentifier{T}.Id"/>.
/// </summary>
/// <typeparam name="TId">The identifier <see cref="Type"/>.</typeparam>
public interface IReferenceData<TId> : IReadOnlyIdentifier<TId>, IReferenceData 
{
    /// <summary>
    /// Gets or initializes the identifier.
    /// </summary>
    new TId Id { get; init; }
}