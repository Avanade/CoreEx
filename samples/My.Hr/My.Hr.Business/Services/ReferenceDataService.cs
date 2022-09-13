using CoreEx.Database;
using CoreEx.Database.Extended;

namespace My.Hr.Business.Services;

public class ReferenceDataService : IReferenceDataProvider
{
    private readonly IDatabase _db;
    private readonly HrDbContext _dbContext;

    public ReferenceDataService(IDatabase db, HrDbContext dbContext)
    {
        _db = db;
        _dbContext = dbContext;
    }

    public Type[] Types => new Type[] { typeof(USState), typeof(Gender) };

    public async Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default) => type switch
    {
        Type t when t == typeof(USState) => await USStateCollection.CreateAsync(_dbContext.USStates.AsNoTracking(), cancellationToken).ConfigureAwait(false),
        Type t when t == typeof(Gender) => await _db.ReferenceData<GenderCollection, Gender, Guid>("Hr", "Gender").LoadAsync("GenderId", cancellationToken: cancellationToken).ConfigureAwait(false),
        _ => throw new InvalidOperationException($"Type {type.FullName} is not a known {nameof(IReferenceData)}.")
    };
}