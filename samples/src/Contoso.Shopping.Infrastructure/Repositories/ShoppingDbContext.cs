namespace Contoso.Shopping.Infrastructure.Repositories;

public partial class ShoppingDbContext(DbContextOptions<ShoppingDbContext> options, SqlServerDatabase database) : DbContext(options), IEfDbContext
{
    /// <inheritdoc/>
    public IDatabase BaseDatabase { get; } = database.ThrowIfNull();

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Uses IDatabase.Connection to ensure the same database/connection is used.
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer(BaseDatabase.Connection, contextOwnsConnection: false).EnableDetailedErrors(true);
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Add the generated models to the model builder.
        AddGeneratedModels(modelBuilder);

        // Extend the 'Shopping.Basket' model configuration.
        modelBuilder.ThrowIfNull().Entity<Persistence.Basket>(e =>
        {
            e.HasMany(r => r.Items).WithOne().HasPrincipalKey(p => p.Id).HasForeignKey(p => p.BasketId).OnDelete(DeleteBehavior.ClientCascade);
            e.Navigation(r => r.Items).AutoInclude(true);
        });
    }
}