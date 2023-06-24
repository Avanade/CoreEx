namespace My.Hr.Business.Services
{
    public interface IEmployeeResultService
    {
        Task<Result<Employee?>> GetEmployeeAsync(Guid id);

        Task<Result<EmployeeCollectionResult>> GetAllAsync(PagingArgs? paging);

        Task<Result<Employee>> AddEmployeeAsync(Employee employee);

        Task<Result<Employee>> UpdateEmployeeAsync(Employee employee, Guid id);

        Task<Result> DeleteEmployeeAsync(Guid id);

        Task<Result> VerifyEmployeeAsync(Guid id);
    }
}