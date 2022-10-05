namespace Company.AppName.Business.Data;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employee", "AppName");
        builder.Property(p => p.Id).HasColumnName("EmployeeId").HasColumnType("UNIQUEIDENTIFIER");
        builder.Property(p => p.Email).HasColumnType("NVARCHAR(250)");
        builder.Property(p => p.FirstName).HasColumnType("NVARCHAR(100)");
        builder.Property(p => p.LastName).HasColumnType("NVARCHAR(100)");
        builder.Property(p => p.Gender).HasColumnName("GenderCode").HasColumnType("NVARCHAR(50)").HasConversion(v => v!.Code, v => (Gender?)v);
        builder.Property(p => p.Birthday).HasColumnType("DATE");
        builder.Property(p => p.StartDate).HasColumnType("DATE");
        builder.Property(p => p.TerminationDate).HasColumnType("DATE");
        builder.Property(p => p.TerminationReasonCode).HasColumnType("NVARCHAR(50)");
        builder.Property(p => p.PhoneNo).HasColumnType("NVARCHAR(50)");
        builder.Property(p => p.ETag).HasColumnName("RowVersion").IsRowVersion().HasConversion(s => s == null ? Array.Empty<byte>() : Convert.FromBase64String(s), d => Convert.ToBase64String(d));
        builder.HasKey("Id");
    }
}