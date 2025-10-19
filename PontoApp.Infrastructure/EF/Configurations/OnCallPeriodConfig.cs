using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoApp.Domain.Entities;

namespace PontoApp.Infrastructure.EF.Configurations;

public class OnCallPeriodConfig : IEntityTypeConfiguration<OnCallPeriod>
{
    public void Configure(EntityTypeBuilder<OnCallPeriod> oc)
    {
        oc.ToTable("OnCallPeriods");
        oc.HasKey(x => x.Id);

        oc.Property(x => x.Start).HasColumnType("date");
        oc.Property(x => x.End).HasColumnType("date");
        oc.Property(x => x.Notes).HasMaxLength(200);

        oc.HasOne(x => x.Employee)
          .WithMany()
          .HasForeignKey(x => x.EmployeeId)
          .OnDelete(DeleteBehavior.Restrict);

        oc.HasOne<Company>()
          .WithMany()
          .HasForeignKey(x => x.CompanyId)
          .OnDelete(DeleteBehavior.Restrict);

        oc.HasIndex(x => new { x.EmployeeId, x.Start, x.End })
          .HasDatabaseName("IX_OnCall_Employee_Period");
    }
}
