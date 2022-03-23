using Microsoft.EntityFrameworkCore;
using My.Hr.Business.Models;

public class HrDbContext : DbContext
{
    public DbSet<USState> USStates { get; set; }

    public HrDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .ApplyConfiguration<USState>(new UsStateConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}