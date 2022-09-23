using CoreEx.Database;
using CoreEx.EntityFrameworkCore;

namespace Company.AppName.Business.Data;

public class HrDbContext : DbContext, IEfDbContext
{
    public IDatabase BaseDatabase { get; }

    public DbSet<USState> USStates { get; set; }

    public DbSet<Gender> Genders { get; set; }

    public DbSet<Employee> Employees { get; set; }

#pragma warning disable CS8618 // Non-nullable property - properties set by Entity Framework Core
    public HrDbContext(DbContextOptions options, IDatabase database) : base(options) => BaseDatabase = database ?? throw new ArgumentNullException(nameof(database));
#pragma warning restore CS8618 // Non-nullable property

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .ApplyConfiguration(new UsStateConfiguration())
            .ApplyConfiguration(new EmployeeConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}