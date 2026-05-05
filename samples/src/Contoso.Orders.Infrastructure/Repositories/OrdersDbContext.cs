namespace Contoso.Orders.Infrastructure.Repositories;

public class OrdersDbContext(DbContextOptions<OrdersDbContext> options, SqlServerDatabase database) : DbContext(options), IEfDbContext
{
    public IDatabase BaseDatabase { get; } = database.ThrowIfNull();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer(BaseDatabase.Connection);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ThrowIfNull().Entity<Persistence.Order>(e =>
        {
            e.ToTable("Order", "Orders");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("OrderId").HasColumnType("NVARCHAR(50)");
            e.Property(p => p.CustomerId).HasColumnName("CustomerId").HasColumnType("NVARCHAR(100)");
            e.Property(p => p.StatusCode).HasColumnName("StatusCode").HasColumnType("NVARCHAR(50)");
            e.Property(p => p.CreatedBy).HasColumnName("CreatedBy").HasColumnType("NVARCHAR(250)");
            e.Property(p => p.CreatedOn).HasColumnName("CreatedOn").HasColumnType("DATETIMEOFFSET");
            e.Property(p => p.UpdatedBy).HasColumnName("UpdatedBy").HasColumnType("NVARCHAR(250)");
            e.Property(p => p.UpdatedOn).HasColumnName("UpdatedOn").HasColumnType("DATETIMEOFFSET");
            e.Property(p => p.ETag).HasColumnName("RowVersion").HasColumnType("TIMESTAMP").IsRowVersion().HasConversion(StringBase64Converter.Default);
            e.HasMany(p => p.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.Navigation(p => p.Items).AutoInclude(true);
        });

        modelBuilder.ThrowIfNull().Entity<Persistence.OrderItem>(e =>
        {
            e.ToTable("OrderItem", "Orders");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("OrderItemId").HasColumnType("NVARCHAR(50)");
            e.Property(p => p.OrderId).HasColumnName("OrderId").HasColumnType("NVARCHAR(50)");
            e.Property(p => p.ProductId).HasColumnName("ProductId").HasColumnType("NVARCHAR(100)");
            e.Property(p => p.Quantity).HasColumnName("Quantity").HasColumnType("DECIMAL(18,4)");
            e.Property(p => p.UnitPrice).HasColumnName("UnitPrice").HasColumnType("DECIMAL(18,4)");
            e.Property(p => p.CreatedBy).HasColumnName("CreatedBy").HasColumnType("NVARCHAR(250)");
            e.Property(p => p.CreatedOn).HasColumnName("CreatedOn").HasColumnType("DATETIMEOFFSET");
            e.Property(p => p.UpdatedBy).HasColumnName("UpdatedBy").HasColumnType("NVARCHAR(250)");
            e.Property(p => p.UpdatedOn).HasColumnName("UpdatedOn").HasColumnType("DATETIMEOFFSET");
        });

        modelBuilder.ThrowIfNull().Entity<Persistence.OrderStatus>(e =>
        {
            e.ToTable("OrderStatus", "Orders");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("OrderStatusId").HasColumnType("NVARCHAR(50)");
            e.Property(p => p.Code).HasColumnName("Code").HasColumnType("NVARCHAR(50)");
            e.Property(p => p.Text).HasColumnName("Text").HasColumnType("NVARCHAR(250)");
            e.Property(p => p.SortOrder).HasColumnName("SortOrder").HasColumnType("INT");
            e.Property(p => p.IsActive).HasColumnName("IsActive").HasColumnType("BIT");
            e.Property(p => p.CreatedBy).HasColumnName("CreatedBy").HasColumnType("NVARCHAR(250)");
            e.Property(p => p.CreatedOn).HasColumnName("CreatedOn").HasColumnType("DATETIMEOFFSET");
            e.Property(p => p.UpdatedBy).HasColumnName("UpdatedBy").HasColumnType("NVARCHAR(250)");
            e.Property(p => p.UpdatedOn).HasColumnName("UpdatedOn").HasColumnType("DATETIMEOFFSET");
            e.Property(p => p.ETag).HasColumnName("RowVersion").HasColumnType("TIMESTAMP").IsRowVersion().HasConversion(StringBase64Converter.Default);
            e.Ignore(p => p.Description).Ignore(p => p.StartsOn).Ignore(p => p.EndsOn);
        });
    }
}