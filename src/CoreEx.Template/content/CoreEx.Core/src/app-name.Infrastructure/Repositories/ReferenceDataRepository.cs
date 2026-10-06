namespace app-name.Infrastructure.Repositories;

/// <summary>Provides the <see cref="ReferenceDataRepository"/> implementation (see generated <c>ReferenceDataRepository.g.cs</c> for entity-specific providers).</summary>
// #if implement-cosmos
public partial class ReferenceDataRepository(domain-nameCosmosDb cosmos)
{
    private readonly domain-nameCosmosDb _cosmos = cosmos.ThrowIfNull();
}
// #else
public partial class ReferenceDataRepository(domain-nameEfDb ef)
{
    private readonly domain-nameEfDb _ef = ef.ThrowIfNull();
}
// #endif
