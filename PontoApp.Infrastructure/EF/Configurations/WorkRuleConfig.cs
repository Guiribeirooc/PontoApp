using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoApp.Domain.Entities;

namespace PontoApp.Infrastructure.EF.Configurations;

public class WorkRuleConfig : IEntityTypeConfiguration<WorkRule>
{
    public void Configure(EntityTypeBuilder<WorkRule> b)
    {
        b.ToTable("WorkRules");
        b.HasKey(x => x.Id);

        b.Property(x => x.Nome).IsRequired().HasMaxLength(100);
        b.Property(x => x.CargaDiariaMin).IsRequired();
        b.Property(x => x.ToleranciaMin).IsRequired();

        b.HasOne<Company>()
         .WithMany()
         .HasForeignKey(x => x.CompanyId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.CompanyId, x.Nome })
         .IsUnique()
         .HasDatabaseName("UX_WorkRules_Company_Name");
    }
}
