﻿namespace My.Hr.Business.Data
{
    /// <summary>
    /// Enables the <b>My.Hr</b> database using Entity Framework.
    /// </summary>
    public interface IHrEfDb : IEfDb
    {
        /// <summary>
        /// Gets the <see cref="Employee"/> entity.
        /// </summary>
        EfDbEntity<Employee, Employee> Employees { get; }

        /// <summary>
        /// Gets the <see cref="Employee2"/> entity.
        /// </summary>
        EfDbEntity<Employee2, Employee2> Employees2 { get; }
    }

    /// <summary>
    /// Represents the <b>My.Hr</b> database using Entity Framework.
    /// </summary>
    public class HrEfDb : EfDb<HrDbContext>, IHrEfDb
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HrEfDb"/> class.
        /// </summary>
        /// <param name="dbContext">The entity framework database context.</param>
        /// <param name="mapper">The <see cref="IMapper"/>.</param>
        public HrEfDb(HrDbContext dbContext, IMapper mapper) : base(dbContext, mapper) { }

        /// <summary>
        /// Gets the <see cref="Employee"/> encapsulated entity.
        /// </summary>
        public EfDbEntity<Employee, Employee> Employees => new(this);

        /// <summary>
        /// Gets the <see cref="Employee"/> encapsulated entity.
        /// </summary>
        public EfDbEntity<Employee2, Employee2> Employees2 => new(this);
    }
}