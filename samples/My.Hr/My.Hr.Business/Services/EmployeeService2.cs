namespace My.Hr.Business.Services;

/// <summary>
/// Example using <see cref="CoreEx.EntityFrameworkCore.IEfDb"/> that largely encapsulates/simplifies the EF access.
/// </summary>
public class EmployeeService2 : IEmployeeService
{
    private readonly IEfDb _efDb;
    private readonly IEventPublisher _publisher;
    private readonly HrSettings _settings;

    public EmployeeService2(IEfDb efDb, IEventPublisher publisher, HrSettings settings)
    {
        _efDb = efDb;
        _publisher = publisher;
        _settings = settings;
    }

    public Task<Employee?> GetEmployeeAsync(Guid id) => _efDb.GetAsync<Employee, Employee>(id);

    public Task<EmployeeCollectionResult> GetAllAsync(PagingArgs? paging)
        => _efDb.Query<Employee, Employee>(q => q.OrderBy(x => x.LastName).ThenBy(x => x.FirstName)).WithPaging(paging).SelectResultAsync<EmployeeCollectionResult, EmployeeCollection>();

    public Task<Employee> AddEmployeeAsync(Employee employee) => _efDb.CreateAsync<Employee, Employee>(employee);

    public Task<Employee> UpdateEmployeeAsync(Employee employee, Guid id)
    {
        employee.Id = id;
        return _efDb.UpdateAsync<Employee, Employee>(employee);
    }

    public Task DeleteEmployeeAsync(Guid id) => _efDb.DeleteAsync<Employee, Employee>(id);

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