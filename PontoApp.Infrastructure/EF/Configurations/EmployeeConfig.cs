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
        b.Property(x => x.Nome).HasMaxLength(120).IsRequired();
        b.Property(x => x.Pin).HasMaxLength(40).IsRequired();
        b.Property(x => x.Ativo).HasDefaultValue(true);
        b.Property(x => x.IsAdmin).HasDefaultValue(false);

    }
}
