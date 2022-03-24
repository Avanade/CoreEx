using Microsoft.EntityFrameworkCore;
using My.Hr.Business.Models;

namespace My.Hr.Business.Services;

public class EmployeeService
{
    private readonly HrDbContext _dbContext;

    public EmployeeService(HrDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Employee?> GetEmployeeAsync(Guid id)
    {
        return await _dbContext.Employees.FirstOrDefaultAsync(e => e.EmployeeId == id);
    }

    public async Task<IEnumerable<Employee>> GetAllAsync()
    {
        return await _dbContext.Employees.ToListAsync();
    }

    public async Task<Employee> AddEmployeeAsync(Employee employee)
    {   
        _dbContext.Employees.Add(employee);
        await _dbContext.SaveChangesAsync();
        return employee;
    }

    public async Task<Employee> UpdateEmployeeAsync(Employee employee, Guid id)
    {
        if(employee.EmployeeId != id)
        {
            throw new InvalidOperationException("EmployeeId does not match");
        }

        _dbContext.Employees.Update(employee);
        await _dbContext.SaveChangesAsync();
        return employee;
    }

    public async Task DeleteEmployeeAsync(Guid id)
    {
        var employee = await _dbContext.Employees.FirstOrDefaultAsync(e => e.EmployeeId == id);
        if (employee != null)
        {
            _dbContext.Employees.Remove(employee);
            await _dbContext.SaveChangesAsync();
        }
    }
}