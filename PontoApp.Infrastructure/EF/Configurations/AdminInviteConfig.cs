using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoApp.Domain.Entities;

namespace PontoApp.Infrastructure.EF.Config;

public class AdminInviteConfig : IEntityTypeConfiguration<AdminInvite>
{
    public void Configure(EntityTypeBuilder<AdminInvite> b)
    {
        b.ToTable("AdminInvites");
        b.HasKey(x => x.Id);

        b.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);
        b.Property(x => x.ExpiresAtUtc).IsRequired();
        b.Property(x => x.MaxUses).HasDefaultValue(1);
        b.Property(x => x.UsedCount).HasDefaultValue(0);

        b.HasIndex(x => x.TokenHash).IsUnique();
    }
}
