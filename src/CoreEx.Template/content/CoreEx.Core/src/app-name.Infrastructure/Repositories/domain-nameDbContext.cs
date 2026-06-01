namespace app-name.Infrastructure.Repositories;

#if (implement-sqlserver)
/// <summary>Provides the <b>domain-name</b> <see cref="DbContext"/> with <see cref="IEfDbContext"/> support.</summary>
public partial class domain-nameDbContext(DbContextOptions<domain-nameDbContext> options, SqlServerDatabase database) : DbContext(options), IEfDbContext
{
    /// <inheritdoc/>
    public IDatabase BaseDatabase { get; } = database.ThrowIfNull();

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer(BaseDatabase.Connection, contextOwnsConnection: false).EnableDetailedErrors(true);
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Add the generated models to the model builder.
        AddGeneratedModels(modelBuilder);
    }
}
#elif (implement-postgres)
/// <summary>Provides the <b>domain-name</b> <see cref="DbContext"/> with <see cref="IEfDbContext"/> support.</summary>
public partial class domain-nameDbContext(DbContextOptions<domain-nameDbContext> options, PostgresDatabase database) : DbContext(options), IEfDbContext
{
    /// <inheritdoc/>
    public IDatabase BaseDatabase { get; } = database.ThrowIfNull();

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseNpgsql(BaseDatabase.Connection, contextOwnsConnection: false).EnableDetailedErrors(true);
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Add the generated models to the model builder.
        AddGeneratedModels(modelBuilder);
    }
}
#endif