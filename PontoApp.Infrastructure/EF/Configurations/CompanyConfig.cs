using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoApp.Domain.Entities;

namespace PontoApp.Infrastructure.EF.Configurations;

public class CompanyConfig : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> b)
    {
        b.ToTable("Companies");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.CNPJ).IsRequired().HasMaxLength(14).IsFixedLength();
        b.HasIndex(x => x.CNPJ).IsUnique();

        b.Property(x => x.IE).HasMaxLength(30);
        b.Property(x => x.IM).HasMaxLength(30);
        b.Property(x => x.Logradouro).HasMaxLength(150);
        b.Property(x => x.Numero).HasMaxLength(20);
        b.Property(x => x.Complemento).HasMaxLength(100);
        b.Property(x => x.Bairro).HasMaxLength(80);
        b.Property(x => x.Cidade).HasMaxLength(80);
        b.Property(x => x.UF).HasMaxLength(2).IsFixedLength();
        b.Property(x => x.CEP).HasMaxLength(8).IsFixedLength();
        b.Property(x => x.Pais).HasMaxLength(60);
        b.Property(x => x.Telefone).HasMaxLength(20);
        b.Property(x => x.EmailContato).HasMaxLength(190);
        b.Property(x => x.CreatedAt).HasColumnType("datetime2");
    }
}
