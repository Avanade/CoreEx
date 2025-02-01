namespace My.Hr.Business.Services;

/// <summary>
/// Example using <see cref="CoreEx.EntityFrameworkCore.IEfDb"/> that largely encapsulates/simplifies the EF access.
/// </summary>
public class EmployeeService2 : IEmployeeService
{
    private readonly IHrEfDb _efDb;
    private readonly IEventPublisher _publisher;
    private readonly HrSettings _settings;

    public EmployeeService2(IHrEfDb efDb, IEventPublisher publisher, HrSettings settings)
    {
        _efDb = efDb;
        _publisher = publisher;
        _settings = settings;
    }

    public Task<Employee?> GetEmployeeAsync(Guid id) => _efDb.Employees.GetAsync(id);

    public Task<EmployeeCollectionResult> GetAllAsync(QueryArgs? query, PagingArgs? paging) 
        => _efDb.Employees.Query(q => q.OrderBy(x => x.LastName).ThenBy(x => x.FirstName)).WithPaging(paging).SelectResultAsync<EmployeeCollectionResult, EmployeeCollection>();

    public Task<Employee> AddEmployeeAsync(Employee employee) => _efDb.Employees.CreateAsync(employee);

    public Task<Employee> UpdateEmployeeAsync(Employee employee, Guid id) => _efDb.Employees.UpdateAsync(employee.Adjust(x => x.Id = id));

    public Task DeleteEmployeeAsync(Guid id) => _efDb.Employees.DeleteAsync(id);

    public async Task VerifyEmployeeAsync(Guid id)
    {
        // Get the employee.
        var employee = await GetEmployeeAsync(id) ?? throw new NotFoundException();

        // Publish message to service bus for employee verification.
        var verification = new EmployeeVerificationRequest
        {
            Name = employee.FirstName,
            Age = SystemTime.Timestamp.Subtract(employee.Birthday.GetValueOrDefault()).Days / 365,
            Gender = employee.Gender?.Code
        };

        _publisher.PublishNamed(_settings.VerificationQueueName, new EventData { Value = verification });
        await _publisher.SendAsync();
    }
}