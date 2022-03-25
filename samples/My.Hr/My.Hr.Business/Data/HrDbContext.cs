using Microsoft.EntityFrameworkCore;
using My.Hr.Business.Models;
using My.Hr.Business.Configuration;

public class HrDbContext : DbContext
{
#pragma warning disable CS8618 // Non-nullable property - properties set by Entity Framework Core

    public DbSet<USState> USStates { get; set; }
    public DbSet<Employee> Employees { get; set; }

#pragma warning restore IDE0060 // Non-nullable property

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