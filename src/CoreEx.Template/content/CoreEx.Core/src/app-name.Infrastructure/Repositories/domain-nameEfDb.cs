namespace app-name.Infrastructure.Repositories;

/// <summary>Provides the <see cref="domain-nameDbContext"/> <see cref="EfDb{TDbContext}"/> wrapper.</summary>
public sealed class domain-nameEfDb(domain-nameDbContext dbContext) : EfDb<domain-nameDbContext>(dbContext, _options)
{
    private static readonly EfDbOptions _options = new();
}