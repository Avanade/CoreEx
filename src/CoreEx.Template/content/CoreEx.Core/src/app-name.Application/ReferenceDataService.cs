namespace app-name.Application;

/// <summary>Provides the <see cref="ReferenceDataService"/> implementation (see generated <c>ReferenceDataService.g.cs</c> for entity-specific providers).</summary>
[ScopedService]
public partial class ReferenceDataService(IReferenceDataRepository repository) : IReferenceDataProvider
{
    private readonly IReferenceDataRepository _repository = repository.ThrowIfNull();

    /// <inheritdoc/>
    public virtual IEnumerable<(Type, Type)> Types => [];

    /// <inheritdoc/>
    public virtual Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException($"Type {type.FullName} is not a known {nameof(IReferenceData)}.");
}