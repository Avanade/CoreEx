using CoreEx.RefData;
using My.Hr.Business.Data;
using My.Hr.Business.Models;

namespace My.Hr.Business.Services;

public class ReferenceDataService : IReferenceDataProvider
{
    private readonly HrDbContext _dbContext;

    public ReferenceDataService(HrDbContext dbContext) => _dbContext = dbContext;

    public Type[] Types => new Type[] { typeof(USState), typeof(Gender) };

    public async Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default) => type switch
    {
        Type _ when type == typeof(USState) => await GetUSStatesAsync().ConfigureAwait(false),
        Type _ when type == typeof(Gender) => await GetGendersAsync().ConfigureAwait(false),
        _ => throw new InvalidOperationException()
    };

    public Task<USStateCollection> GetUSStatesAsync() => USStateCollection.CreateAsync(_dbContext.USStates);

    public Task<GenderCollection> GetGendersAsync() => GenderCollection.CreateAsync(_dbContext.Genders);
}