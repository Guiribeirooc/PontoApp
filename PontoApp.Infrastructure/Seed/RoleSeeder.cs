using Microsoft.EntityFrameworkCore;
using PontoApp.Domain.Entities;
using PontoApp.Infrastructure.EF;

namespace PontoApp.Infrastructure.Seed;

public static class RoleSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {

        if (!await db.Set<Role>().AnyAsync(ct))
        {
            db.AddRange(
                new Role { Name = "Admin" },
                new Role { Name = "Employee" }
            );
            await db.SaveChangesAsync(ct);
        }
    }
}