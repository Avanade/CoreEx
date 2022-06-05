namespace My.Hr.Business.Data;

public class GenderConfiguration : IEntityTypeConfiguration<Gender>
{
    public void Configure(EntityTypeBuilder<Gender> entity)
    {
        entity.ToTable("Gender", "Hr");
        entity.HasKey("Id");
        entity.Property(p => p.Id).HasColumnName("GenderId").HasColumnType("UNIQUEIDENTIFIER");
        entity.Property(p => p.Code).HasColumnType("NVARCHAR(50)");
        entity.Property(p => p.Text).HasColumnType("NVARCHAR(250)");
        entity.Property(p => p.IsActive).HasColumnType("BIT");
        entity.Property(p => p.SortOrder).HasColumnType("INT");
        entity.Property(p => p.ETag).HasColumnName("RowVersion").IsRowVersion().HasConversion(s => s == null ? Array.Empty<byte>() : Convert.FromBase64String(s), d => Convert.ToBase64String(d));
        entity.Ignore(p => p.EndDate);
        entity.Ignore(p => p.StartDate);
        entity.Ignore(p => p.Description);
        entity.Ignore(p => p.IsReadOnly);
        entity.Ignore(p => p.IsValid);
        entity.Ignore(p => p.IsChanged);
        entity.Ignore(p => p.IsInitial);
        entity.Ignore(p => p.NotifyChangesWhenSameValue);
    }
}