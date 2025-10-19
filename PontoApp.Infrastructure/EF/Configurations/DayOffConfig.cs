using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoApp.Domain.Entities;

namespace PontoApp.Infrastructure.EF.Configurations;

public class DayOffConfig : IEntityTypeConfiguration<DayOff>
{
    public void Configure(EntityTypeBuilder<DayOff> df)
    {
        df.ToTable("DayOffs");
        df.HasKey(x => x.Id);

        df.Property(x => x.Date).HasColumnType("date");
        df.Property(x => x.Reason).HasMaxLength(200);

        df.HasOne(x => x.Employee)
          .WithMany()
          .HasForeignKey(x => x.EmployeeId)
          .OnDelete(DeleteBehavior.Restrict);

        df.HasOne<Company>()
          .WithMany()
          .HasForeignKey(x => x.CompanyId)
          .OnDelete(DeleteBehavior.Restrict);

        df.HasIndex(x => new { x.EmployeeId, x.Date })
          .IsUnique()
          .HasDatabaseName("UX_DayOffs_Employee_Date");
    }
}
