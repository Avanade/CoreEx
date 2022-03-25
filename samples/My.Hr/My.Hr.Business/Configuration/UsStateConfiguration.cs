using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using My.Hr.Business.Models;

namespace My.Hr.Business.Configuration;

public class UsStateConfiguration : IEntityTypeConfiguration<USState>
{
    public void Configure(EntityTypeBuilder<USState> entity)
    {
        entity.ToTable("USState", "Hr");
        entity.HasKey("Id");
        entity.Property(p => p.Id).HasColumnName("USStateId").HasColumnType("UNIQUEIDENTIFIER");
        entity.Property(p => p.Code).HasColumnType("NVARCHAR(50)");
        entity.Property(p => p.Text).HasColumnType("NVARCHAR(250)");
        entity.Property(p => p.IsActive).HasColumnType("BIT");
        entity.Property(p => p.SortOrder).HasColumnType("INT");
        entity.Property(p => p.RowVersion).HasColumnType("TIMESTAMP").IsRowVersion();
        entity.Property(p => p.CreatedBy).HasColumnType("NVARCHAR(250)").ValueGeneratedOnUpdate();
        entity.Property(p => p.CreatedDate).HasColumnType("DATETIME2").ValueGeneratedOnUpdate();
        entity.Property(p => p.UpdatedBy).HasColumnType("NVARCHAR(250)").ValueGeneratedOnAdd();
        entity.Property(p => p.UpdatedDate).HasColumnType("DATETIME2").ValueGeneratedOnAdd();

        entity.Ignore(p => p.EndDate);
        entity.Ignore(p => p.StartDate);
        entity.Ignore(p => p.Description);
    }
}
