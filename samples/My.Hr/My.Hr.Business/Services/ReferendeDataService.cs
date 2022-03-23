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
        return await _dbContext.USStates.ToListAsync();

        // return Task.FromResult(new List<USState>()
        // {
        //     new USState { Code = "AL", Text = "Alabama" },
        //     new USState { Code = "AK", Text = "Alaska" },
        //     new USState { Code = "AZ", Text = "Arizona" },
        //     new USState { Code = "AR", Text = "Arkansas" },
        //     new USState { Code = "CA", Text = "California" },
        //     new USState { Code = "CO", Text = "Colorado" },
        //     new USState { Code = "CT", Text = "Connecticut" },
        //     new USState { Code = "DE", Text = "Delaware" },
        //     new USState { Code = "FL", Text = "Florida" },
        //     new USState { Code = "GA", Text = "Georgia" },
        //     new USState { Code = "HI", Text = "Hawaii" },
        //     new USState { Code = "ID", Text = "Idaho" },
        //     new USState { Code = "IL", Text = "Illinois" }
        // }.AsEnumerable());
    }
}