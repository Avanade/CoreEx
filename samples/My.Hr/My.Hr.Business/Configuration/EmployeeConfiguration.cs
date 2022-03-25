using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using My.Hr.Business.Models;

namespace My.Hr.Business.Configuration;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employee", "Hr");
        builder.HasKey("EmployeeId");
        builder.Property(p => p.EmployeeId).HasColumnType("UNIQUEIDENTIFIER");
        builder.Property(p => p.Email).HasColumnType("NVARCHAR(250)");
        builder.Property(p => p.FirstName).HasColumnType("NVARCHAR(100)");
        builder.Property(p => p.LastName).HasColumnType("NVARCHAR(100)");
        builder.Property(p => p.GenderCode).HasColumnType("NVARCHAR(50)");
        builder.Property(p => p.Birthday).HasColumnType("DATE");
        builder.Property(p => p.StartDate).HasColumnType("DATE");
        builder.Property(p => p.TerminationDate).HasColumnType("DATE");
        builder.Property(p => p.TerminationReasonCode).HasColumnType("NVARCHAR(50)");
        builder.Property(p => p.PhoneNo).HasColumnType("NVARCHAR(50)");
    }
}