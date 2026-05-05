using CoreEx.Database.Postgres.Test.Unit.Models;
using CoreEx.EntityFrameworkCore;
using CoreEx.EntityFrameworkCore.Converters;
using Microsoft.EntityFrameworkCore;

namespace CoreEx.Database.Postgres.Test.Unit.Repository;

public class TestDbContext(DbContextOptions<TestDbContext> options, PostgresDatabase database) : DbContext(options), IEfDbContext
{
    public IDatabase BaseDatabase { get; } = database.ThrowIfNull();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Uses IDatabase.Connection to ensure the same database/connection is used.
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseNpgsql(BaseDatabase.Connection, contextOwnsConnection: false);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Add the model configuration.
        modelBuilder.ThrowIfNull().Entity<TestTable>(e =>
        {
            e.ToTable("table", "test");
            e.HasKey(nameof(TestTable.Id));
            e.Property(p => p.Id).HasColumnName("table_id").HasColumnType("uuid");
            e.Property(p => p.Text).HasColumnName("text").HasColumnType("varchar(200)");
            e.Property(p => p.Number).HasColumnName("number").HasColumnType("integer");
            e.Property(p => p.Amount).HasColumnName("amount").HasColumnType("decimal(19, 4)");
            e.Property(p => p.Flag).HasColumnName("flag").HasColumnType("boolean");
            e.Property(p => p.Date).HasColumnName("date").HasColumnType("date");
            e.Property(p => p.Time).HasColumnName("time").HasColumnType("time");
            e.Property(p => p.Json).HasColumnName("json").HasColumnType("jsonb").HasConversion(JsonElementStringEfConverter.Default);
            e.Property(p => p.TenantId).HasColumnName("tenant_id").HasColumnType("varchar(20)");
            e.Property(p => p.ETag).HasColumnName("xmin").HasColumnType("xid").IsRowVersion().HasConversion(ValueConverterBridge.Create<string?, uint>(BaseDatabase.RowVersionConverter));
            e.Property(p => p.CreatedBy).HasColumnName("created_by").HasColumnType("varchar(250)").ValueGeneratedOnUpdate();
            e.Property(p => p.CreatedOn).HasColumnName("created_on").HasColumnType("timestamptz").ValueGeneratedOnUpdate();
            e.Property(p => p.UpdatedBy).HasColumnName("updated_by").HasColumnType("varchar(250)").ValueGeneratedOnAdd();
            e.Property(p => p.UpdatedOn).HasColumnName("updated_on").HasColumnType("timestamptz").ValueGeneratedOnAdd();
            e.Property(p => p.IsDeleted).HasColumnName("is_deleted").HasColumnType("boolean").HasDefaultValue(false);
        });
    }
}