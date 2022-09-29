using CoreEx.Mapping;

namespace Company.AppName.Business.Data
{
    /// <summary>
    /// Enables the <b>Company.AppName</b> database using Entity Framework.
    /// </summary>
    public interface IAppNameEfDb : IEfDb
    {
        /// <summary>
        /// Gets the <see cref="Employee"/> entity.
        /// </summary>
        EfDbEntity<Employee, Employee> Employees { get; }
    }

    /// <summary>
    /// Represents the <b>Company.AppName</b> database using Entity Framework.
    /// </summary>
    public class AppNameEfDb : EfDb<AppNameDbContext>, IAppNameEfDb
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppNameEfDb"/> class.
        /// </summary>
        /// <param name="dbContext">The entity framework database context.</param>
        /// <param name="mapper">The <see cref="IMapper"/>.</param>
        public AppNameEfDb(AppNameDbContext dbContext, IMapper mapper) : base(dbContext, mapper) { }

        /// <summary>
        /// Gets the <see cref="Employee"/> encapsulated entity.
        /// </summary>
        public EfDbEntity<Employee, Employee> Employees => new(this);
    }
}