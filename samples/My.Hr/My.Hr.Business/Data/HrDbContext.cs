using Microsoft.EntityFrameworkCore;
using My.Hr.Business.Models;
using My.Hr.Business.Configuration;

namespace My.Hr.Business.Data;

public class HrDbContext : DbContext
{
    public DbSet<USState> USStates { get; set; }
    public DbSet<Gender> Genders { get; set; }
    public DbSet<Employee> Employees { get; set; }

#pragma warning disable CS8618 // Non-nullable property - properties set by Entity Framework Core
    public HrDbContext(DbContextOptions options) : base(options) { }
#pragma warning restore CS8618 // Non-nullable property

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .ApplyConfiguration<USState>(new UsStateConfiguration())
            .ApplyConfiguration<Gender>(new GenderConfiguration())
            .ApplyConfiguration<Employee>(new EmployeeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}