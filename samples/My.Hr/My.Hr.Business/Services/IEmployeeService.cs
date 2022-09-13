namespace My.Hr.Business.Services
{
    public interface IEmployeeService
    {
        Task<Employee?> GetEmployeeAsync(Guid id);

        Task<EmployeeCollectionResult> GetAllAsync(PagingArgs? paging);

        Task<Employee> AddEmployeeAsync(Employee employee);

        Task<Employee> UpdateEmployeeAsync(Employee employee, Guid id);

        Task DeleteEmployeeAsync(Guid id);

        Task VerifyEmployeeAsync(Guid id);
    }
}