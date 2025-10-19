using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoApp.Domain.Entities;

namespace PontoApp.Infrastructure.EF.Configurations;

public class EmployeeConfig : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> eb)
    {
        eb.ToTable("Employees");
        eb.HasKey(e => e.Id);

        eb.Property(e => e.Nome).IsRequired().HasMaxLength(100);
        eb.Property(e => e.Pin).IsRequired().HasMaxLength(6);
        eb.Property(e => e.Cpf).IsRequired().HasMaxLength(11).IsFixedLength();
        eb.Property(e => e.Email).IsRequired().HasMaxLength(160);
        eb.Property(e => e.PhotoPath).HasMaxLength(400);
        eb.Property(e => e.Phone).HasMaxLength(30);
        eb.Property(e => e.NisPis).HasMaxLength(20);
        eb.Property(e => e.City).HasMaxLength(80);
        eb.Property(e => e.State).HasMaxLength(2).IsFixedLength();
        eb.Property(e => e.Departamento).HasMaxLength(100);
        eb.Property(e => e.Cargo).HasMaxLength(100);
        eb.Property(e => e.Matricula).HasMaxLength(50);
        eb.Property(e => e.EmployerName).HasMaxLength(150);
        eb.Property(e => e.UnitName).HasMaxLength(150);
        eb.Property(e => e.ManagerName).HasMaxLength(150);
        eb.Property(e => e.HourlyRate).HasPrecision(10, 2);

        eb.Property(e => e.ShiftStart).HasColumnType("time");
        eb.Property(e => e.ShiftEnd).HasColumnType("time");

        eb.HasOne<Company>()
          .WithMany()
          .HasForeignKey(e => e.CompanyId)
          .OnDelete(DeleteBehavior.Restrict);

        eb.HasIndex(e => new { e.CompanyId, e.Pin })
          .IsUnique()
          .HasDatabaseName("UX_Employees_Company_Pin")
          .HasFilter("[IsDeleted]=0 AND [Pin] IS NOT NULL");

        eb.HasIndex(e => new { e.CompanyId, e.Cpf })
          .IsUnique()
          .HasDatabaseName("UX_Employees_Company_Cpf")
          .HasFilter("[IsDeleted]=0");

        eb.HasIndex(e => new { e.CompanyId, e.Email })
          .HasDatabaseName("IX_Employees_Company_Email")
          .HasFilter("[IsDeleted]=0");

        eb.HasIndex(e => e.Nome)
          .HasDatabaseName("IX_Employees_Company_Nome");
    }
}
