using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using PontoApp.Domain.Entities;

namespace PontoApp.Infrastructure.EF
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt) { }

        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<Punch> Punches => Set<Punch>();
        public DbSet<AppUser> Users => Set<AppUser>();
        public DbSet<TimeBankEntry> TimeBankEntries => Set<TimeBankEntry>();
        public DbSet<Leave> Leaves => Set<Leave>();
        public DbSet<DayOff> DayOffs => Set<DayOff>();
        public DbSet<OnCallPeriod> OnCallPeriods => Set<OnCallPeriod>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);
            b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            b.Entity<Employee>(eb =>
            {
                eb.HasKey(e => e.Id);

                eb.Property(e => e.Nome).IsRequired().HasMaxLength(80);

                eb.Property(e => e.Pin).IsRequired().HasMaxLength(6);
                eb.HasIndex(e => e.Pin)
                  .IsUnique()
                  .HasFilter("[IsDeleted] = 0 AND [Pin] IS NOT NULL");

                eb.Property(e => e.Cpf).IsRequired().HasMaxLength(11);
                eb.HasIndex(e => e.Cpf).IsUnique().HasFilter("[IsDeleted] = 0");

                eb.Property(e => e.Email).IsRequired().HasMaxLength(160);
                eb.HasIndex(e => e.Email).HasFilter("[IsDeleted] = 0");

                eb.Property(e => e.BirthDate).HasColumnType("date");
                eb.Property(e => e.PhotoPath).HasMaxLength(400);

                eb.Property(e => e.Jornada).HasConversion<int>().IsRequired();

                eb.Property(e => e.ShiftStart)
                  .HasConversion(
                      v => v.HasValue ? v.Value.ToTimeSpan() : (TimeSpan?)null,
                      v => v.HasValue ? TimeOnly.FromTimeSpan(v.Value) : (TimeOnly?)null
                  ).HasColumnType("time");

                eb.Property(e => e.ShiftEnd)
                  .HasConversion(
                      v => v.HasValue ? v.Value.ToTimeSpan() : (TimeSpan?)null,
                      v => v.HasValue ? TimeOnly.FromTimeSpan(v.Value) : (TimeOnly?)null
                  ).HasColumnType("time");

                eb.HasQueryFilter(e => !e.IsDeleted);

                eb.HasIndex(e => e.Nome);

                eb.Property(e => e.HourlyRate).HasPrecision(10, 2);
            });

            b.Entity<Punch>(pb =>
            {
                pb.HasKey(p => p.Id);

                pb.Property(p => p.Tipo).IsRequired().HasConversion<int>();
                pb.Property(p => p.DataHora).IsRequired().HasColumnType("datetimeoffset");
                pb.Property(p => p.EmployeeId).IsRequired();

                pb.HasOne(p => p.Employee)
                  .WithMany(e => e.Punches)
                  .HasForeignKey(p => p.EmployeeId)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Restrict);

                pb.Property(p => p.Justificativa).HasMaxLength(300);
                pb.Property(p => p.Origem).HasMaxLength(50);

                pb.HasQueryFilter(p => !p.Employee.IsDeleted);
            });

            b.Entity<TimeBankEntry>(tb =>
            {
                tb.HasKey(x => x.Id);
                tb.Property(x => x.Reason).HasMaxLength(200);
                tb.Property(x => x.Source).HasMaxLength(30);

                tb.Property(x => x.At).HasColumnType("date");
                tb.Property(x => x.EmployeeId).IsRequired();

                tb.HasOne(x => x.Employee)
                  .WithMany()
                  .HasForeignKey(x => x.EmployeeId)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Restrict);

                tb.HasIndex(x => new { x.EmployeeId, x.At });

                tb.HasQueryFilter(x => !x.Employee.IsDeleted);
            });

            b.Entity<Leave>(lv =>
            {
                lv.HasKey(x => x.Id);
                lv.Property(x => x.Type).HasConversion<int>().IsRequired();
                lv.Property(x => x.Status).HasConversion<int>().IsRequired();
                lv.Property(x => x.Notes).HasMaxLength(400);

                lv.Property(x => x.Start).HasColumnType("date");
                lv.Property(x => x.End).HasColumnType("date");

                lv.Property(x => x.EmployeeId).IsRequired();

                lv.HasOne(x => x.Employee)
                  .WithMany()
                  .HasForeignKey(x => x.EmployeeId)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Restrict);

                lv.HasIndex(x => new { x.EmployeeId, x.Start, x.End });

                lv.HasQueryFilter(x => !x.Employee.IsDeleted);
            });

            b.Entity<DayOff>(df =>
            {
                df.HasKey(x => x.Id);
                df.Property(x => x.Reason).HasMaxLength(200);

                df.Property(x => x.Date).HasColumnType("date");

                df.Property(x => x.EmployeeId).IsRequired();

                df.HasOne(x => x.Employee)
                  .WithMany()
                  .HasForeignKey(x => x.EmployeeId)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Restrict);

                df.HasIndex(x => new { x.EmployeeId, x.Date }).IsUnique();

                df.HasQueryFilter(x => !x.Employee.IsDeleted);
            });

            b.Entity<OnCallPeriod>(oc =>
            {
                oc.HasKey(x => x.Id);
                oc.Property(x => x.Notes).HasMaxLength(200);

                oc.Property(x => x.Start).HasColumnType("date");
                oc.Property(x => x.End).HasColumnType("date");

                oc.Property(x => x.EmployeeId).IsRequired();

                oc.HasOne(x => x.Employee)
                  .WithMany()
                  .HasForeignKey(x => x.EmployeeId)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Restrict);

                oc.HasIndex(x => new { x.EmployeeId, x.Start, x.End });

                oc.HasQueryFilter(x => !x.Employee.IsDeleted);
            });

        }
    }
}
