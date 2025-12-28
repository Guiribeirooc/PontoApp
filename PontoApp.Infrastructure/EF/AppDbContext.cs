using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PontoApp.Domain.Entities;

namespace PontoApp.Infrastructure.EF
{
    public class AppDbContext(DbContextOptions<AppDbContext> opt, IHttpContextAccessor http) : DbContext(opt)
    {
        private readonly IHttpContextAccessor _http = http;

        public DbSet<Company> Companies => Set<Company>();
        public DbSet<AppUser> Users => Set<AppUser>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<WorkRule> WorkRules => Set<WorkRule>();
        public DbSet<Punch> Punches => Set<Punch>();
        public DbSet<TimeBankEntry> TimeBankEntries => Set<TimeBankEntry>();
        public DbSet<Leave> Leaves => Set<Leave>();
        public DbSet<DayOff> DayOffs => Set<DayOff>();
        public DbSet<OnCallPeriod> OnCallPeriods => Set<OnCallPeriod>();

        public DbSet<AdminInvite> AdminInvites => Set<AdminInvite>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);
            b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            b.Entity<Company>(c =>
            {
                c.HasKey(x => x.Id);
                c.Property(x => x.Name).IsRequired().HasMaxLength(200);
                c.Property(x => x.CNPJ).IsRequired().HasMaxLength(14).IsFixedLength();
                c.Property(x => x.IE).HasMaxLength(30);
                c.Property(x => x.IM).HasMaxLength(30);
                c.Property(x => x.Logradouro).HasMaxLength(150);
                c.Property(x => x.Numero).HasMaxLength(20);
                c.Property(x => x.Complemento).HasMaxLength(100);
                c.Property(x => x.Bairro).HasMaxLength(80);
                c.Property(x => x.Cidade).HasMaxLength(80);
                c.Property(x => x.UF).HasMaxLength(2).IsFixedLength();
                c.Property(x => x.CEP).HasMaxLength(8).IsFixedLength();
                c.Property(x => x.Pais).HasMaxLength(60);
                c.Property(x => x.Telefone).HasMaxLength(20);
                c.Property(x => x.EmailContato).HasMaxLength(190);
                c.HasIndex(x => x.CNPJ).IsUnique();
            });

            b.Entity<Role>(r =>
            {
                r.HasKey(x => x.Id);
                r.Property(x => x.Name).IsRequired().HasMaxLength(50);
                r.HasIndex(x => x.Name).IsUnique();
            });

            b.Entity<UserRole>(ur =>
            {
                ur.HasKey(x => new { x.UserId, x.RoleId });
                ur.HasOne(x => x.User).WithMany(u => u.Roles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
                ur.HasOne(x => x.Role).WithMany(r => r.Users).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<AppUser>(u =>
            {
                u.HasKey(x => x.Id);

                u.Property(x => x.Email).IsRequired().HasMaxLength(190);
                u.Property(x => x.Name).IsRequired().HasMaxLength(200);
                u.Property(x => x.PasswordHash).IsRequired();
                u.Property(x => x.PasswordSalt).IsRequired();

                u.HasIndex(x => new { x.CompanyId, x.Email }).HasDatabaseName("UX_Users_Company_Email");

                u.HasOne<Company>()
                    .WithMany()
                    .HasForeignKey(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                u.HasOne(x => x.Employee)
                    .WithMany()
                    .HasForeignKey(x => x.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

            });

            b.Entity<Employee>(eb =>
            {
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

                eb.HasIndex(e => new { e.CompanyId, e.Pin }).IsUnique().HasFilter("[IsDeleted]=0 AND [Pin] IS NOT NULL").HasDatabaseName("UX_Employees_Company_Pin");
                eb.HasIndex(e => new { e.CompanyId, e.Cpf }).IsUnique().HasFilter("[IsDeleted]=0").HasDatabaseName("UX_Employees_Company_Cpf");
                eb.HasIndex(e => new { e.CompanyId, e.Email }).HasFilter("[IsDeleted]=0").HasDatabaseName("IX_Employees_Company_Email");
                eb.HasIndex(e => e.Nome).HasDatabaseName("IX_Employees_Company_Nome");
            });

            b.Entity<WorkRule>(wr =>
            {
                wr.HasKey(x => x.Id);
                wr.Property(x => x.Nome).IsRequired().HasMaxLength(100);
                wr.Property(x => x.CargaDiariaMin).IsRequired();
                wr.Property(x => x.ToleranciaMin).IsRequired();

                wr.HasOne<Company>()
                  .WithMany()
                  .HasForeignKey(x => x.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);

                wr.HasIndex(x => new { x.CompanyId, x.Nome }).IsUnique().HasDatabaseName("UX_WorkRules_Company_Name");
            });

            b.Entity<Punch>(pb =>
            {
                pb.HasKey(p => p.Id);
                pb.Property(p => p.Tipo).IsRequired().HasConversion<int>();
                pb.Property(p => p.DataHora).IsRequired().HasColumnType("datetime2");
                pb.Property(p => p.Justificativa).HasMaxLength(300);
                pb.Property(p => p.Origem).HasMaxLength(50);
                pb.Property(p => p.SourceIp).HasMaxLength(45);
                pb.Property(p => p.Notes).HasMaxLength(500);

                pb.HasOne(p => p.Employee)
                  .WithMany(e => e.Punches)
                  .HasForeignKey(p => p.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);

                pb.HasOne<Company>()
                  .WithMany()
                  .HasForeignKey(p => p.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);

                pb.HasIndex(p => new { p.CompanyId, p.EmployeeId, p.DataHora }).HasDatabaseName("IX_Punches_Company_Employee_Ts");

            });

            b.Entity<TimeBankEntry>(tb =>
            {
                tb.HasKey(x => x.Id);
                tb.Property(x => x.At).HasColumnType("date");
                tb.Property(x => x.Reason).HasMaxLength(200);
                tb.Property(x => x.Source).HasMaxLength(30);

                tb.HasOne(x => x.Employee)
                  .WithMany()
                  .HasForeignKey(x => x.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);

                tb.HasOne<Company>()
                  .WithMany()
                  .HasForeignKey(x => x.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);

                tb.HasIndex(x => new { x.EmployeeId, x.At }).HasDatabaseName("IX_TimeBank_Employee_At");
            });

            b.Entity<Leave>(lv =>
            {
                lv.HasKey(x => x.Id);
                lv.Property(x => x.Type).HasConversion<int>().IsRequired();
                lv.Property(x => x.Status).HasConversion<int>().IsRequired();
                lv.Property(x => x.Notes).HasMaxLength(400);
                lv.Property(x => x.Start).HasColumnType("date");
                lv.Property(x => x.End).HasColumnType("date");

                lv.HasOne(x => x.Employee)
                  .WithMany()
                  .HasForeignKey(x => x.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);

                lv.HasOne<Company>()
                  .WithMany()
                  .HasForeignKey(x => x.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);

                lv.HasIndex(x => new { x.EmployeeId, x.Start, x.End }).HasDatabaseName("IX_Leaves_Employee_Period");
            });

            b.Entity<DayOff>(df =>
            {
                df.HasKey(x => x.Id);
                df.Property(x => x.Reason).HasMaxLength(200);
                df.Property(x => x.Date).HasColumnType("date");

                df.HasOne(x => x.Employee)
                  .WithMany()
                  .HasForeignKey(x => x.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);

                df.HasOne<Company>()
                  .WithMany()
                  .HasForeignKey(x => x.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);

                df.HasIndex(x => new { x.EmployeeId, x.Date }).IsUnique().HasDatabaseName("UX_DayOffs_Employee_Date");
            });

            b.Entity<OnCallPeriod>(oc =>
            {
                oc.HasKey(x => x.Id);
                oc.Property(x => x.Notes).HasMaxLength(200);
                oc.Property(x => x.Start).HasColumnType("date");
                oc.Property(x => x.End).HasColumnType("date");

                oc.HasOne(x => x.Employee)
                  .WithMany()
                  .HasForeignKey(x => x.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);

                oc.HasOne<Company>()
                  .WithMany()
                  .HasForeignKey(x => x.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);

                oc.HasIndex(x => new { x.EmployeeId, x.Start, x.End }).HasDatabaseName("IX_OnCall_Employee_Period");
            });

            b.Entity<AdminInvite>(ai =>
            {
                ai.HasKey(x => x.Id);
                ai.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);
                ai.Property(x => x.CompanyName).IsRequired().HasMaxLength(200);
                ai.Property(x => x.CompanyDocument).IsRequired().HasMaxLength(32);
                ai.Property(x => x.ExpiresAtUtc).IsRequired();
                ai.Property(x => x.MaxUses).HasDefaultValue(1);
                ai.Property(x => x.UsedCount).HasDefaultValue(0);
                ai.HasIndex(x => x.TokenHash).IsUnique();
            });
        }

        public override int SaveChanges()
        {
            StampCompanyId();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            StampCompanyId();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void StampCompanyId()
        {
            var cid = CurrentCompanyId();
            if (cid <= 0) return;

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State != EntityState.Added) continue;

                var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CompanyId");
                if (prop is { CurrentValue: null or 0 })
                    prop.CurrentValue = cid;
            }
        }

        private int CurrentCompanyId()
        {
            var claim = _http?.HttpContext?.User?.FindFirst("CompanyId")?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }

    internal static class EF
    {
        private static IHttpContextAccessor? _http;
        public static void UseHttp(IHttpContextAccessor accessor) => _http = accessor;

        public static int CompanyId()
        {
            var cid = _http?.HttpContext?.User?.FindFirst("CompanyId")?.Value;
            return int.TryParse(cid, out var id) ? id : 0;
        }
    }
}
