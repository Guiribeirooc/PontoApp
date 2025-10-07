using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoApp.Domain.Entities;

namespace PontoApp.Infrastructure.EF.Configurations;

public class AppUserConfig : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("Users");
        b.Property(x => x.Email).IsRequired().HasMaxLength(256);
        b.HasIndex(x => x.Email).IsUnique();

        b.Property(x => x.PasswordHash).IsRequired();
        b.Property(x => x.PasswordSalt).IsRequired();

        b.Property(x => x.ResetCode).HasMaxLength(20);
        b.Property(x => x.IsDeleted).HasDefaultValue(false);

        b.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
