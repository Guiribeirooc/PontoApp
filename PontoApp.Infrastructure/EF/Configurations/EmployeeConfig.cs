using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoApp.Domain.Entities;

namespace PontoApp.Infrastructure.EF.Configurations;
public class EmployeeConfig : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> b)
    {
        b.ToTable("Employees");

        b.HasKey(x => x.Id);

        // Básicos
        b.Property(x => x.Nome)
            .IsRequired()
            .HasMaxLength(120);

        b.Property(x => x.Pin)
            .IsRequired()
            .HasMaxLength(6); 

        b.Property(x => x.Ativo)
            .HasDefaultValue(true);

        b.Property(x => x.IsAdmin)
            .HasDefaultValue(false);

        b.Property(x => x.Cpf)
            .IsRequired()
            .HasMaxLength(11); 

        b.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(160);

        b.Property(x => x.Phone)
            .HasMaxLength(30);

        b.Property(x => x.NisPis)
            .HasMaxLength(30);

        b.Property(x => x.BirthDate)
            .HasColumnType("date");

        b.Property(x => x.PhotoPath)
            .HasMaxLength(400);

        b.Property(x => x.City).HasMaxLength(80);
        b.Property(x => x.State).HasMaxLength(60);
        b.Property(x => x.Departamento).HasMaxLength(80);
        b.Property(x => x.Cargo).HasMaxLength(80);
        b.Property(x => x.Matricula).HasMaxLength(50);

        b.Property(x => x.HourlyRate)
            .HasPrecision(10, 2); 

        b.Property(x => x.AdmissionDate).HasColumnType("date");
        b.Property(x => x.TrackingStart).HasColumnType("date");
        b.Property(x => x.TrackingEnd).HasColumnType("date");
        b.Property(x => x.VacationAccrualStart).HasColumnType("date");

        b.Property(x => x.ManagerName).HasMaxLength(80);
        b.Property(x => x.EmployerName).HasMaxLength(120);
        b.Property(x => x.UnitName).HasMaxLength(120);

        b.Property(x => x.ShiftStart)
            .HasConversion(
                v => v.HasValue ? v.Value.ToTimeSpan() : (TimeSpan?)null,
                v => v.HasValue ? TimeOnly.FromTimeSpan(v.Value) : (TimeOnly?)null
            )
            .HasColumnType("time");

        b.Property(x => x.ShiftEnd)
            .HasConversion(
                v => v.HasValue ? v.Value.ToTimeSpan() : (TimeSpan?)null,
                v => v.HasValue ? TimeOnly.FromTimeSpan(v.Value) : (TimeOnly?)null
            )
            .HasColumnType("time");

        b.HasIndex(x => x.Pin)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [Pin] IS NOT NULL");

        b.HasIndex(x => x.Cpf)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        b.HasIndex(x => x.Email)
            .HasFilter("[IsDeleted] = 0");

        b.HasIndex(x => x.Nome);

        b.HasQueryFilter(e => !e.IsDeleted);
    }
}
