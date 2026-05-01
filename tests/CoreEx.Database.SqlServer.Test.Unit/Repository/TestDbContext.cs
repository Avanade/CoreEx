using CoreEx.Database.SqlServer.Test.Unit.Models;
using CoreEx.EntityFrameworkCore;
using CoreEx.EntityFrameworkCore.Converters;
using Microsoft.EntityFrameworkCore;

namespace CoreEx.Database.SqlServer.Test.Unit.Repository;

public class TestDbContext(DbContextOptions<TestDbContext> options, SqlServerDatabase database) : DbContext(options), IEfDbContext
{
    public IDatabase BaseDatabase { get; } = database.ThrowIfNull();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Uses IDatabase.Connection to ensure the same database/connection is used.
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer(BaseDatabase.Connection);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Add the model configuration.
        modelBuilder.ThrowIfNull().Entity<TestTable>(e =>
        {
            e.ToTable("Table", "Test");
            e.HasKey(nameof(TestTable.Id));
            e.Property(p => p.Id).HasColumnName("TableId").HasColumnType("UNIQUEIDENTIFIER");
            e.Property(p => p.Text).HasColumnName("Text").HasColumnType("NVARCHAR(200)");
            e.Property(p => p.Number).HasColumnName("Number").HasColumnType("INT");
            e.Property(p => p.Amount).HasColumnName("Amount").HasColumnType("MONEY");
            e.Property(p => p.Flag).HasColumnName("Flag").HasColumnType("BIT");
            e.Property(p => p.Date).HasColumnName("Date").HasColumnType("DATE");
            e.Property(p => p.Time).HasColumnName("Time").HasColumnType("TIME");
            e.Property(p => p.Json).HasColumnName("Json").HasColumnType("NVARCHAR(500)").HasConversion(JsonElementStringConverter.Default);
            e.Property(p => p.TenantId).HasColumnName("TenantId").HasColumnType("NVARCHAR(20)");
            e.Property(p => p.ETag).HasColumnName("RowVersion").HasColumnType("TIMESTAMP").IsRowVersion().HasConversion(StringBase64Converter.Default);
            e.Property(p => p.CreatedBy).HasColumnName("CreatedBy").HasColumnType("NVARCHAR(250)").ValueGeneratedOnUpdate();
            e.Property(p => p.CreatedOn).HasColumnName("CreatedOn").HasColumnType("DATETIMEOFFSET").ValueGeneratedOnUpdate();
            e.Property(p => p.UpdatedBy).HasColumnName("UpdatedBy").HasColumnType("NVARCHAR(250)").ValueGeneratedOnAdd();
            e.Property(p => p.UpdatedOn).HasColumnName("UpdatedOn").HasColumnType("DATETIMEOFFSET").ValueGeneratedOnAdd();
            e.Property(p => p.IsDeleted).HasColumnName("IsDeleted").HasColumnType("BIT").HasDefaultValue(false);
        });
    }
}