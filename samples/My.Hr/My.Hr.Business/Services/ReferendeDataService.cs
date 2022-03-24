using Microsoft.EntityFrameworkCore;
using My.Hr.Business.Models;

namespace My.Hr.Business.Services;

public class ReferenceDataService
{
    private readonly HrDbContext _dbContext;

    public ReferenceDataService(HrDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<IEnumerable<USState>> GetAll(List<string>? codes = default, string? text = default)
    {
        // todo: add filtering
        return await _dbContext.USStates.ToListAsync();
    }
}