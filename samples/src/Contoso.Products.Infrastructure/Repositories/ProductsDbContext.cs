namespace Contoso.Products.Infrastructure.Repositories;

public partial class ProductsDbContext(DbContextOptions<ProductsDbContext> options, SqlServerDatabase database) : DbContext(options), IEfDbContext
{
    /// <inheritdoc/>
    public IDatabase BaseDatabase { get; } = database.ThrowIfNull();

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Uses IDatabase.Connection to ensure the same database/connection is used.
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer(BaseDatabase.Connection, contextOwnsConnection: false);
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Add the generated models to the model builder.
        AddGeneratedModels(modelBuilder);
    }
}