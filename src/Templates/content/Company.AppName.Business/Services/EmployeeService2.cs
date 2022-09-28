namespace Company.AppName.Business.Services;

/// <summary>
/// Example using <see cref="CoreEx.EntityFrameworkCore.IEfDb"/> that largely encapsulates/simplifies the EF access.
/// </summary>
public class EmployeeService2 : IEmployeeService
{
    private readonly IAppNameEfDb _efDb;
    private readonly IEventPublisher _publisher;
    private readonly AppNameSettings _settings;

    public EmployeeService2(IAppNameEfDb efDb, IEventPublisher publisher, AppNameSettings settings)
    {
        _efDb = efDb;
        _publisher = publisher;
        _settings = settings;
    }

    public Task<Employee?> GetEmployeeAsync(Guid id) => _efDb.Employees.GetAsync(id);

    public Task<EmployeeCollectionResult> GetAllAsync(PagingArgs? paging)
        => _efDb.Employees.Query(q => q.OrderBy(x => x.LastName).ThenBy(x => x.FirstName)).WithPaging(paging).SelectResultAsync<EmployeeCollectionResult, EmployeeCollection>();

    public Task<Employee> AddEmployeeAsync(Employee employee) => _efDb.Employees.CreateAsync(employee);

    public Task<Employee> UpdateEmployeeAsync(Employee employee, Guid id) => _efDb.Employees.UpdateAsync(employee.Adjust(x => x.Id = id));

    public Task DeleteEmployeeAsync(Guid id) => _efDb.Employees.DeleteAsync(id);

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