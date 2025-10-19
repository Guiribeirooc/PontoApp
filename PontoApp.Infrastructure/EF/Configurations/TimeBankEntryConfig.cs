using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoApp.Domain.Entities;

namespace PontoApp.Infrastructure.EF.Configurations;

public class TimeBankEntryConfig : IEntityTypeConfiguration<TimeBankEntry>
{
    public void Configure(EntityTypeBuilder<TimeBankEntry> b)
    {
        b.ToTable("TimeBankEntries");
        b.HasKey(x => x.Id);

        b.Property(x => x.At).HasColumnType("date");
        b.Property(x => x.Reason).HasMaxLength(200);
        b.Property(x => x.Source).HasMaxLength(30);

        b.HasOne(x => x.Employee)
         .WithMany()
         .HasForeignKey(x => x.EmployeeId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<Company>()
         .WithMany()
         .HasForeignKey(x => x.CompanyId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.EmployeeId, x.At })
         .HasDatabaseName("IX_TimeBank_Employee_At");
    }
}
