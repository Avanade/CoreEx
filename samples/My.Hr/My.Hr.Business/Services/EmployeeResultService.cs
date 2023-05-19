namespace My.Hr.Business.Services;

/// <summary>
/// Example using <see cref="CoreEx.EntityFrameworkCore.IEfDb"/> that largely encapsulates/simplifies the EF access with a <see cref="Result"/> (achieving railway-oriented programming)
/// </summary>
public class EmployeeResultService : IEmployeeResultService
{
    private readonly IHrEfDb _efDb;
    private readonly IEventPublisher _publisher;
    private readonly HrSettings _settings;

    public EmployeeResultService(IHrEfDb efDb, IEventPublisher publisher, HrSettings settings)
    {
        _efDb = efDb;
        _publisher = publisher;
        _settings = settings;
    }

    public Task<Result<Employee?>> GetEmployeeAsync(Guid id) => _efDb.Employees.GetWithResultAsync(id);

    public Task<Result<EmployeeCollectionResult>> GetAllAsync(PagingArgs? paging) 
        => _efDb.Employees.Query(q => q.OrderBy(x => x.LastName).ThenBy(x => x.FirstName)).WithPaging(paging).SelectResultWithResultAsync<EmployeeCollectionResult, EmployeeCollection>();

    public Task<Result<Employee>> AddEmployeeAsync(Employee employee) => _efDb.Employees.CreateWithResultAsync(employee);

    public Task<Result<Employee>> UpdateEmployeeAsync(Employee employee, Guid id) => _efDb.Employees.UpdateWithResultAsync(employee.Adjust(x => x.Id = id));

    public Task<Result> DeleteEmployeeAsync(Guid id) => _efDb.Employees.DeleteWithResultAsync(id);

    public Task<Result> VerifyEmployeeAsync(Guid id) 
        => Result
            .GoAsync(GetEmployeeAsync(id))
            .When(employee => employee == null, () => Result<Employee>.NotFoundError())
            .ThenAsync(async employee =>
            {
                // Publish message to service bus for employee verification.
                var verification = new EmployeeVerificationRequest
                {
                    Name = employee.FirstName,
                    Age = DateTime.UtcNow.Subtract(employee.Birthday.GetValueOrDefault()).Days / 365,
                    Gender = employee.Gender?.Code
                };

                _publisher.PublishNamed(_settings.VerificationQueueName, new EventData { Value = verification });
                await _publisher.SendAsync();
            });
}