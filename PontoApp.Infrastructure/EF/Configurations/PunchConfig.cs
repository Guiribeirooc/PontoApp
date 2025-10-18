using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoApp.Domain.Entities;

namespace PontoApp.Infrastructure.EF.Configurations;
public class PunchConfig : IEntityTypeConfiguration<Punch>
{
    public void Configure(EntityTypeBuilder<Punch> b)
    {
        b.ToTable("Punches");
        b.HasKey(p => p.Id);

        b.Property(p => p.DataHora)
         .HasColumnType("datetime2")
         .IsRequired();

        b.Property(p => p.Tipo)
         .HasConversion<int>()
         .IsRequired();

        b.Property(p => p.Ip).HasMaxLength(64);
        b.Property(p => p.Justificativa).HasMaxLength(200);
        b.Property(p => p.Origem).HasMaxLength(50);

        b.HasOne(p => p.Employee)
         .WithMany(e => e.Punches)
         .HasForeignKey(p => p.EmployeeId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(p => p.DataHora);
        b.HasIndex(p => new { p.EmployeeId, p.DataHora });
    }
}
