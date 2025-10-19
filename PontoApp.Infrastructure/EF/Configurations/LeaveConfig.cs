using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoApp.Domain.Entities;

namespace PontoApp.Infrastructure.EF.Configurations;

public class LeaveConfig : IEntityTypeConfiguration<Leave>
{
    public void Configure(EntityTypeBuilder<Leave> lv)
    {
        lv.ToTable("Leaves");
        lv.HasKey(x => x.Id);

        lv.Property(x => x.Type).IsRequired().HasConversion<int>();
        lv.Property(x => x.Status).IsRequired().HasConversion<int>();
        lv.Property(x => x.Notes).HasMaxLength(400);
        lv.Property(x => x.Start).HasColumnType("date");
        lv.Property(x => x.End).HasColumnType("date");

        lv.HasOne(x => x.Employee)
          .WithMany()
          .HasForeignKey(x => x.EmployeeId)
          .OnDelete(DeleteBehavior.Restrict);

        lv.HasOne<Company>()
          .WithMany()
          .HasForeignKey(x => x.CompanyId)
          .OnDelete(DeleteBehavior.Restrict);

        lv.HasIndex(x => new { x.EmployeeId, x.Start, x.End })
          .HasDatabaseName("IX_Leaves_Employee_Period");
    }
}
