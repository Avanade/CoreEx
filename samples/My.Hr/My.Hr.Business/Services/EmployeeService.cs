using CoreEx.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using My.Hr.Business.Models;
using My.Hr.Business.ServiceContracts;

namespace My.Hr.Business.Services;

public class EmployeeService
{
    private readonly HrDbContext _dbContext;
    private readonly IEventPublisher _publisher;
    private readonly HrSettings _settings;

    public EmployeeService(HrDbContext dbContext, IEventPublisher publisher, HrSettings settings)
    {
        _dbContext = dbContext;
        _publisher = publisher;
        _settings = settings;
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
        if (employee.EmployeeId != id)
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

    public async Task<IActionResult> VerifyEmployeeAsync(Guid id)
    {
        // first get Employee
        var employee = await GetEmployeeAsync(id);

        if (employee == null)
        {
            return new NotFoundResult();
        }

        if (string.IsNullOrEmpty(employee.FirstName))
        {
            return new BadRequestObjectResult("Employee is missing FirstName");
        }

        if (string.IsNullOrEmpty(employee.GenderCode))
        {
            return new BadRequestObjectResult("Employee is missing GenderCode");
        }

        // publish message to service bus for employee verification
        var verification = new EmployeeVerificationRequest
        {
            Name = employee.FirstName,
            Age = DateTime.UtcNow.Subtract(employee.Birthday.GetValueOrDefault()).Days / 365,
            Gender = employee.GenderCode
        };

        _publisher.Publish(_settings.VerificationQueueName, new EventData { Value = verification });
        await _publisher.SendAsync();

        return new NoContentResult();
    }
}