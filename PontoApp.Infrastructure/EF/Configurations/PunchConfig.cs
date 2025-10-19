using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoApp.Domain.Entities;

namespace PontoApp.Infrastructure.EF.Configurations;

public class PunchConfig : IEntityTypeConfiguration<Punch>
{
    public void Configure(EntityTypeBuilder<Punch> pb)
    {
        pb.ToTable("Punches");
        pb.HasKey(p => p.Id);

        pb.Property(p => p.DataHora).IsRequired().HasColumnType("datetime2");
        pb.Property(p => p.Tipo).IsRequired().HasConversion<int>();
        pb.Property(p => p.Justificativa).HasMaxLength(300);
        pb.Property(p => p.Origem).HasMaxLength(50);
        pb.Property(p => p.SourceIp).HasMaxLength(45);
        pb.Property(p => p.Notes).HasMaxLength(500);

        pb.HasOne(p => p.Employee)
          .WithMany(e => e.Punches)
          .HasForeignKey(p => p.EmployeeId)
          .OnDelete(DeleteBehavior.Restrict);

        pb.HasOne<Company>()
          .WithMany()
          .HasForeignKey(p => p.CompanyId)
          .OnDelete(DeleteBehavior.Restrict);

        pb.HasIndex(p => new { p.CompanyId, p.EmployeeId, p.DataHora })
          .HasDatabaseName("IX_Punches_Company_Employee_Ts");
    }
}
