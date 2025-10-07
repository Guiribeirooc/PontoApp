using Microsoft.EntityFrameworkCore;
using PontoApp.Domain.Entities;

namespace PontoApp.Infrastructure.EF
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt) { }

        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<Punch> Punches => Set<Punch>();
        public DbSet<AppUser> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<Employee>(eb =>
            {
                eb.HasKey(e => e.Id);

                eb.Property(e => e.Nome)
                  .IsRequired()
                  .HasMaxLength(80);

                eb.Property(e => e.Pin)
                  .IsRequired()
                  .HasMaxLength(6);

                eb.HasIndex(e => e.Pin)
                  .IsUnique()
                  .HasFilter("[IsDeleted] = 0 AND [Pin] IS NOT NULL");

                eb.Property(e => e.Cpf)
                  .IsRequired()
                  .HasMaxLength(14);

                eb.Property(e => e.Email)
                  .IsRequired()
                  .HasMaxLength(160);

                eb.Property(e => e.BirthDate)
                  .HasColumnType("date");

                eb.Property(e => e.PhotoPath)
                  .HasMaxLength(400);

                eb.HasQueryFilter(e => !e.IsDeleted);

                eb.HasIndex(e => e.Cpf)
                  .IsUnique()
                  .HasFilter("[IsDeleted] = 0");

                eb.HasIndex(e => e.Email).HasFilter("[IsDeleted] = 0");
                eb.HasIndex(e => e.Nome);

                eb.Property(e => e.ShiftStart)
                  .HasConversion(
                    v => v.HasValue ? new TimeSpan?(v.Value.ToTimeSpan()) : null,
                    v => v.HasValue ? new TimeOnly?(TimeOnly.FromTimeSpan(v.Value)) : null
                  )
                  .HasColumnType("time");

                eb.Property(e => e.ShiftEnd)
                  .HasConversion(
                    v => v.HasValue ? new TimeSpan?(v.Value.ToTimeSpan()) : null,
                    v => v.HasValue ? new TimeOnly?(TimeOnly.FromTimeSpan(v.Value)) : null
                  )
                  .HasColumnType("time");
            });

            b.Entity<Punch>(pb =>
            {
                pb.HasKey(p => p.Id);

                pb.Property(p => p.Tipo)
                  .IsRequired()
                  .HasConversion<int>();

                pb.Property(p => p.DataHora)
                  .IsRequired();

                pb.HasOne(p => p.Employee)
                  .WithMany()
                  .HasForeignKey(p => p.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);

                pb.Property(p => p.Justificativa).HasMaxLength(300);
                
                pb.Property(p => p.Origem).HasMaxLength(50);
            });
        }

    }
}
