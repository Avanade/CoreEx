namespace Contoso.Orders.Infrastructure.Repositories;

public partial class OrdersDbContext(DbContextOptions<OrdersDbContext> options, SqlServerDatabase database) : DbContext(options), IEfDbContext
{
    public IDatabase BaseDatabase { get; } = database.ThrowIfNull();

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer(BaseDatabase.Connection);
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Add the generated models to the model builder.
        AddGeneratedModels(modelBuilder);

        modelBuilder.ThrowIfNull().Entity<Persistence.Order>(e =>
        {
            e.HasMany(p => p.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.Navigation(r => r.Items).AutoInclude(true);
        });
    }

    /// <summary>Adds the CodeGen-generated entity models to the <paramref name="modelBuilder"/>.</summary>
    /// <param name="modelBuilder">The <see cref="ModelBuilder"/>.</param>
    partial void AddGeneratedModels(ModelBuilder modelBuilder);
}
