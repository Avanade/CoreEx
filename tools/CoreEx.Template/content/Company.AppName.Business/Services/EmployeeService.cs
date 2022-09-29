namespace Company.AppName.Business.Services;

public class EmployeeService : IEmployeeService
{
    private readonly AppNameDbContext _dbContext;
    private readonly IEventPublisher _publisher;
    private readonly AppNameSettings _settings;

    public EmployeeService(AppNameDbContext dbContext, IEventPublisher publisher, AppNameSettings settings)
    {
        _dbContext = dbContext;
        _publisher = publisher;
        _settings = settings;
    }

    public async Task<Employee?> GetEmployeeAsync(Guid id)
        => await _dbContext.Employees.FirstOrDefaultAsync(e => e.Id == id);

    public Task<EmployeeCollectionResult> GetAllAsync(PagingArgs? paging)
        => _dbContext.Employees.OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToCollectionResultAsync<EmployeeCollectionResult, EmployeeCollection, Employee>(paging);

    public async Task<Employee> AddEmployeeAsync(Employee employee)
    {
        _dbContext.Employees.Add(employee);
        await _dbContext.SaveChangesAsync();
        return employee;
    }

    public async Task<Employee> UpdateEmployeeAsync(Employee employee, Guid id)
    {
        if (!await _dbContext.Employees.AnyAsync(e => e.Id == id).ConfigureAwait(false))
            throw new NotFoundException();

        employee.Id = id;
        _dbContext.Employees.Update(employee);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        return employee;
    }

    public async Task DeleteEmployeeAsync(Guid id)
    {
        var employee = await _dbContext.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (employee != null)
        {
            _dbContext.Employees.Remove(employee);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task VerifyEmployeeAsync(Guid id)
    {
        // Get the employee.
        var employee = await GetEmployeeAsync(id);
        if (employee == null)
            throw new NotFoundException();

        // Publish message to service bus for employee verification.
        var verification = new EmployeeVerificationRequest
        {
            Name = employee.FirstName,
            Age = DateTime.UtcNow.Subtract(employee.Birthday.GetValueOrDefault()).Days / 365,
            Gender = employee.Gender?.Code
        };

        _publisher.Publish(_settings.VerificationQueueName, new EventData { Value = verification });
        await _publisher.SendAsync();
    }
}