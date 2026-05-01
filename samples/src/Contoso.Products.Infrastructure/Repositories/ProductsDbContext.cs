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
            optionsBuilder.UseSqlServer(BaseDatabase.Connection);
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Add the generated models to the model builder.
        AddGeneratedModels(modelBuilder);

        //// Add the 'Products.Category' model configuration.
        //modelBuilder.ThrowIfNull().Entity<Persistence.Category>(e =>
        //{
        //    e.ToTable("Category", "Products");
        //    e.HasKey(nameof(Persistence.Category.Id));
        //    e.Property(p => p.Id).HasColumnName("CategoryId").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.Code).HasColumnName("Code").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.Text).HasColumnName("Text").HasColumnType("NVARCHAR(250)");
        //    e.Property(p => p.SortOrder).HasColumnName("SortOrder").HasColumnType("INT");
        //    e.Property(p => p.IsActive).HasColumnName("IsActive").HasColumnType("BIT");
        //    e.Property(p => p.CreatedBy).HasColumnName("CreatedBy").HasColumnType("NVARCHAR(250)").ValueGeneratedOnUpdate();
        //    e.Property(p => p.CreatedOn).HasColumnName("CreatedOn").HasColumnType("DATETIMEOFFSET").ValueGeneratedOnUpdate();
        //    e.Property(p => p.UpdatedBy).HasColumnName("UpdatedBy").HasColumnType("NVARCHAR(250)").ValueGeneratedOnAdd();
        //    e.Property(p => p.UpdatedOn).HasColumnName("UpdatedOn").HasColumnType("DATETIMEOFFSET").ValueGeneratedOnAdd();
        //    e.Property(p => p.ETag).HasColumnName("RowVersion").HasColumnType("TIMESTAMP").IsRowVersion().HasConversion(StringBase64Converter.Default);
        //    e.Ignore(p => p.Description).Ignore(p => p.StartsOn).Ignore(p => p.EndsOn);
        //});

        //// Add the 'Products.SubCategory' model configuration.
        //modelBuilder.ThrowIfNull().Entity<Persistence.SubCategory>(e =>
        //{
        //    e.ToTable("SubCategory", "Products");
        //    e.HasKey(nameof(Persistence.SubCategory.Id));
        //    e.Property(p => p.Id).HasColumnName("SubCategoryId").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.Code).HasColumnName("Code").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.Text).HasColumnName("Text").HasColumnType("NVARCHAR(250)");
        //    e.Property(p => p.SortOrder).HasColumnName("SortOrder").HasColumnType("INT");
        //    e.Property(p => p.IsActive).HasColumnName("IsActive").HasColumnType("BIT");
        //    e.Property(p => p.CategoryCode).HasColumnName("CategoryCode").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.CreatedBy).HasColumnName("CreatedBy").HasColumnType("NVARCHAR(250)").ValueGeneratedOnUpdate();
        //    e.Property(p => p.CreatedOn).HasColumnName("CreatedOn").HasColumnType("DATETIMEOFFSET").ValueGeneratedOnUpdate();
        //    e.Property(p => p.UpdatedBy).HasColumnName("UpdatedBy").HasColumnType("NVARCHAR(250)").ValueGeneratedOnAdd();
        //    e.Property(p => p.UpdatedOn).HasColumnName("UpdatedOn").HasColumnType("DATETIMEOFFSET").ValueGeneratedOnAdd();
        //    e.Property(p => p.ETag).HasColumnName("RowVersion").HasColumnType("TIMESTAMP").IsRowVersion().HasConversion(StringBase64Converter.Default);
        //    e.Ignore(p => p.Description).Ignore(p => p.StartsOn).Ignore(p => p.EndsOn);
        //});

        //// Add the 'Products.UnitOfMeasure' model configuration.
        //modelBuilder.ThrowIfNull().Entity<Persistence.UnitOfMeasure>(e =>
        //{
        //    e.ToTable("UnitOfMeasure", "Products");
        //    e.HasKey(nameof(Persistence.UnitOfMeasure.Id));
        //    e.Property(p => p.Id).HasColumnName("UnitOfMeasureId").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.Code).HasColumnName("Code").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.Text).HasColumnName("Text").HasColumnType("NVARCHAR(250)");
        //    e.Property(p => p.SortOrder).HasColumnName("SortOrder").HasColumnType("INT");
        //    e.Property(p => p.IsActive).HasColumnName("IsActive").HasColumnType("BIT");
        //    e.Property(p => p.Scale).HasColumnName("Scale").HasColumnType("INT");
        //    e.Property(p => p.CreatedBy).HasColumnName("CreatedBy").HasColumnType("NVARCHAR(250)").ValueGeneratedOnUpdate();
        //    e.Property(p => p.CreatedOn).HasColumnName("CreatedOn").HasColumnType("DATETIMEOFFSET").ValueGeneratedOnUpdate();
        //    e.Property(p => p.UpdatedBy).HasColumnName("UpdatedBy").HasColumnType("NVARCHAR(250)").ValueGeneratedOnAdd();
        //    e.Property(p => p.UpdatedOn).HasColumnName("UpdatedOn").HasColumnType("DATETIMEOFFSET").ValueGeneratedOnAdd();
        //    e.Property(p => p.ETag).HasColumnName("RowVersion").HasColumnType("TIMESTAMP").IsRowVersion().HasConversion(StringBase64Converter.Default);
        //    e.Ignore(p => p.Description).Ignore(p => p.StartsOn).Ignore(p => p.EndsOn);
        //});

        //// Add the 'Products.Brand' model configuration.
        //modelBuilder.ThrowIfNull().Entity<Persistence.Brand>(e =>
        //{
        //    e.ToTable("Brand", "Products");
        //    e.HasKey(nameof(Persistence.Brand.Id));
        //    e.Property(p => p.Id).HasColumnName("BrandId").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.Code).HasColumnName("Code").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.Text).HasColumnName("Text").HasColumnType("NVARCHAR(250)");
        //    e.Property(p => p.SortOrder).HasColumnName("SortOrder").HasColumnType("INT");
        //    e.Property(p => p.IsActive).HasColumnName("IsActive").HasColumnType("BIT");
        //    e.Property(p => p.CreatedBy).HasColumnName("CreatedBy").HasColumnType("NVARCHAR(250)").ValueGeneratedOnUpdate();
        //    e.Property(p => p.CreatedOn).HasColumnName("CreatedOn").HasColumnType("DATETIMEOFFSET").ValueGeneratedOnUpdate();
        //    e.Property(p => p.UpdatedBy).HasColumnName("UpdatedBy").HasColumnType("NVARCHAR(250)").ValueGeneratedOnAdd();
        //    e.Property(p => p.UpdatedOn).HasColumnName("UpdatedOn").HasColumnType("DATETIMEOFFSET").ValueGeneratedOnAdd();
        //    e.Property(p => p.ETag).HasColumnName("RowVersion").HasColumnType("TIMESTAMP").IsRowVersion().HasConversion(StringBase64Converter.Default);
        //    e.Ignore(p => p.Description).Ignore(p => p.StartsOn).Ignore(p => p.EndsOn);
        //});

        //// Add the 'Products.MovementKind' model configuration.
        //modelBuilder.ThrowIfNull().Entity<Persistence.MovementKind>(e =>
        //{
        //    e.ToTable("MovementKind", "Products");
        //    e.HasKey(nameof(Persistence.MovementKind.Id));
        //    e.Property(p => p.Id).HasColumnName("MovementKindId").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.Code).HasColumnName("Code").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.Text).HasColumnName("Text").HasColumnType("NVARCHAR(250)");
        //    e.Property(p => p.SortOrder).HasColumnName("SortOrder").HasColumnType("INT");
        //    e.Property(p => p.IsActive).HasColumnName("IsActive").HasColumnType("BIT");
        //    e.Property(p => p.CreatedBy).HasColumnName("CreatedBy").HasColumnType("NVARCHAR(250)").ValueGeneratedOnUpdate();
        //    e.Property(p => p.CreatedOn).HasColumnName("CreatedOn").HasColumnType("DATETIMEOFFSET").ValueGeneratedOnUpdate();
        //    e.Property(p => p.UpdatedBy).HasColumnName("UpdatedBy").HasColumnType("NVARCHAR(250)").ValueGeneratedOnAdd();
        //    e.Property(p => p.UpdatedOn).HasColumnName("UpdatedOn").HasColumnType("DATETIMEOFFSET").ValueGeneratedOnAdd();
        //    e.Property(p => p.ETag).HasColumnName("RowVersion").HasColumnType("TIMESTAMP").IsRowVersion().HasConversion(StringBase64Converter.Default);
        //    e.Ignore(p => p.Description).Ignore(p => p.StartsOn).Ignore(p => p.EndsOn);
        //});

        //// Add the 'Products.MovementStatus' model configuration.
        //modelBuilder.ThrowIfNull().Entity<Persistence.MovementStatus>(e =>
        //{
        //    e.ToTable("MovementStatus", "Products");
        //    e.HasKey(nameof(Persistence.MovementStatus.Id));
        //    e.Property(p => p.Id).HasColumnName("MovementStatusId").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.Code).HasColumnName("Code").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.Text).HasColumnName("Text").HasColumnType("NVARCHAR(250)");
        //    e.Property(p => p.SortOrder).HasColumnName("SortOrder").HasColumnType("INT");
        //    e.Property(p => p.IsActive).HasColumnName("IsActive").HasColumnType("BIT");
        //    e.Property(p => p.CreatedBy).HasColumnName("CreatedBy").HasColumnType("NVARCHAR(250)").ValueGeneratedOnUpdate();
        //    e.Property(p => p.CreatedOn).HasColumnName("CreatedOn").HasColumnType("DATETIMEOFFSET").ValueGeneratedOnUpdate();
        //    e.Property(p => p.UpdatedBy).HasColumnName("UpdatedBy").HasColumnType("NVARCHAR(250)").ValueGeneratedOnAdd();
        //    e.Property(p => p.UpdatedOn).HasColumnName("UpdatedOn").HasColumnType("DATETIMEOFFSET").ValueGeneratedOnAdd();
        //    e.Property(p => p.ETag).HasColumnName("RowVersion").HasColumnType("TIMESTAMP").IsRowVersion().HasConversion(StringBase64Converter.Default);
        //    e.Ignore(p => p.Description).Ignore(p => p.StartsOn).Ignore(p => p.EndsOn);
        //});

        //// Add the 'Products.Product' model configuration.
        //modelBuilder.ThrowIfNull().Entity<Persistence.Product>(e =>
        //{
        //    e.ToTable("Product", "Products");
        //    e.HasKey(nameof(Persistence.Product.Id));
        //    e.Property(p => p.Id).HasColumnName("ProductId").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.Sku).HasColumnName("Sku").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.Text).HasColumnName("Text").HasColumnType("NVARCHAR(250)");
        //    e.Property(p => p.SubCategoryCode).HasColumnName("SubCategoryCode").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.BrandCode).HasColumnName("BrandCode").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.UnitOfMeasureCode).HasColumnName("UnitOfMeasureCode").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.Price).HasColumnName("Price").HasColumnType("DECIMAL(18, 2)");
        //    e.Property(p => p.IsInactive).HasColumnName("IsInactive").HasColumnType("BIT");
        //    e.Property(p => p.IsNonStocked).HasColumnName("IsNonStocked").HasColumnType("BIT");
        //    e.Property(p => p.CreatedBy).HasColumnName("CreatedBy").HasColumnType("NVARCHAR(250)").ValueGeneratedOnUpdate();
        //    e.Property(p => p.CreatedOn).HasColumnName("CreatedOn").HasColumnType("DATETIMEOFFSET").ValueGeneratedOnUpdate();
        //    e.Property(p => p.UpdatedBy).HasColumnName("UpdatedBy").HasColumnType("NVARCHAR(250)").ValueGeneratedOnAdd();
        //    e.Property(p => p.UpdatedOn).HasColumnName("UpdatedOn").HasColumnType("DATETIMEOFFSET").ValueGeneratedOnAdd();
        //    e.Property(p => p.IsDeleted).HasColumnName("IsDeleted").HasColumnType("BIT");
        //    e.Property(p => p.ETag).HasColumnName("RowVersion").HasColumnType("TIMESTAMP").IsRowVersion().HasConversion(StringBase64Converter.Default);
        //});

        //// Add the 'Products.Inventory' model configuration.
        //modelBuilder.ThrowIfNull().Entity<Persistence.Inventory>(e =>
        //{
        //    e.ToTable("Inventory", "Products");
        //    e.HasKey(nameof(Persistence.Inventory.Id));
        //    e.Property(p => p.Id).HasColumnName("InventoryId").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.QtyOnHand).HasColumnName("QtyOnHand").HasColumnType("DECIMAL(18, 2)");
        //    e.Property(p => p.ETag).HasColumnName("RowVersion").HasColumnType("TIMESTAMP").IsRowVersion().HasConversion(ValueConverterBridge.Create<string?, byte[]>(BaseDatabase.RowVersionConverter));
        //});

        //// Add the 'Products.Movement' model configuration.
        //modelBuilder.ThrowIfNull().Entity<Persistence.Movement>(e =>
        //{
        //    e.ToTable("Movement", "Products");
        //    e.HasKey(nameof(Persistence.Movement.Id));
        //    e.Property(p => p.Id).HasColumnName("MovementId").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.ReferenceId).HasColumnName("ReferenceId").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.MovementKindCode).HasColumnName("MovementKindCode").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.MovementStatusCode).HasColumnName("MovementStatusCode").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.ProductId).HasColumnName("ProductId").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.Quantity).HasColumnName("Quantity").HasColumnType("DECIMAL(18, 2)");
        //    e.Property(p => p.UnitOfMeasureCode).HasColumnName("UnitOfMeasureCode").HasColumnType("NVARCHAR(50)");
        //    e.Property(p => p.CreatedBy).HasColumnName("CreatedBy").HasColumnType("NVARCHAR(250)").ValueGeneratedOnUpdate();
        //    e.Property(p => p.CreatedOn).HasColumnName("CreatedOn").HasColumnType("DATETIMEOFFSET").ValueGeneratedOnUpdate();
        //    e.Property(p => p.UpdatedBy).HasColumnName("UpdatedBy").HasColumnType("NVARCHAR(250)").ValueGeneratedOnAdd();
        //    e.Property(p => p.UpdatedOn).HasColumnName("UpdatedOn").HasColumnType("DATETIMEOFFSET").ValueGeneratedOnAdd();
        //    e.Property(p => p.ETag).HasColumnName("RowVersion").HasColumnType("TIMESTAMP").IsRowVersion().HasConversion(ValueConverterBridge.Create<string?, byte[]>(BaseDatabase.RowVersionConverter));
        //});
    }
}