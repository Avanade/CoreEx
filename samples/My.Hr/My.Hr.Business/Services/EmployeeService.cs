using CoreEx.Data.Querying;

namespace My.Hr.Business.Services;

public class EmployeeService : IEmployeeService
{
    private static readonly QueryArgsConfig _queryConfig = QueryArgsConfig.Create()
        .WithFilter(filter => filter
            .AddField<string>("LastName", c => c.WithOperators(QueryFilterOperator.AllStringOperators).WithUpperCase())
            .AddField<string>("FirstName", c => c.WithOperators(QueryFilterOperator.AllStringOperators).WithUpperCase())
            .AddField<DateTime>("StartDate")
            .AddField<DateTime>("TerminationDate")
            .AddField<string>(nameof(Employee.Gender), c => c.WithValue(v =>
            {
                var g = Gender.ConvertFromCode(v);
                return g is not null && g.IsValid ? g : throw new FormatException("Gender is invalid.");
            })))
        .WithOrderBy(orderBy => orderBy
            .AddField("LastName")
            .AddField("FirstName")
            .WithDefault("LastName, FirstName"));

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
        var emp = await _dbContext.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (emp is not null && emp.Birthday.HasValue && emp.Birthday.Value.Year < 2000)
            CoreEx.ExecutionContext.Current.Messages.Add(MessageType.Warning, "Employee is considered old.");

        return emp;
    }

    public Task<EmployeeCollectionResult> GetAllAsync(QueryArgs? query, PagingArgs? paging) 
        => _dbContext.Employees.Where(_queryConfig, query).OrderBy(_queryConfig, query).ToCollectionResultAsync<EmployeeCollectionResult, EmployeeCollection, Employee>(paging);

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
        var employee = await GetEmployeeAsync(id) ?? throw new NotFoundException();

        // Publish message to service bus for employee verification.
        var verification = new EmployeeVerificationRequest
        {
            Name = employee.FirstName,
            Age = DateTime.UtcNow.Subtract(employee.Birthday.GetValueOrDefault()).Days / 365,
            Gender = employee.Gender?.Code
        };

        _publisher.PublishNamed(_settings.VerificationQueueName, new EventData { Value = verification });
        await _publisher.SendAsync();
    }
}