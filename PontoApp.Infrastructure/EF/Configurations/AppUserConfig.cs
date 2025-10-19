using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoApp.Domain.Entities;

namespace PontoApp.Infrastructure.EF.Configurations;

public class AppUserConfig : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("Users");
        b.HasKey(x => x.Id);

        b.Property(x => x.Email).IsRequired().HasMaxLength(190);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.PasswordHash).IsRequired();
        b.Property(x => x.PasswordSalt).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnType("datetime2");
        b.Property(x => x.LastLoginAt).HasColumnType("datetime2");

        // único por empresa (usando filtro para ignorar deletados)
        b.HasIndex(x => new { x.CompanyId, x.Email })
         .IsUnique()
         .HasDatabaseName("UX_Users_Company_Email")
         .HasFilter("[IsDeleted] = 0");

        b.HasOne<Company>()
         .WithMany()
         .HasForeignKey(x => x.CompanyId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Employee)
         .WithMany()
         .HasForeignKey(x => x.EmployeeId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}
