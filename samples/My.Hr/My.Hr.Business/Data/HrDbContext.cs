using Microsoft.EntityFrameworkCore;
using My.Hr.Business.Models;
using My.Hr.Business.Configuration;

public class HrDbContext : DbContext
{
    public DbSet<USState> USStates { get; set; }
    public DbSet<Employee> Employees { get; set; }

    public HrDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .ApplyConfiguration<USState>(new UsStateConfiguration())
            .ApplyConfiguration<Employee>(new EmployeeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}