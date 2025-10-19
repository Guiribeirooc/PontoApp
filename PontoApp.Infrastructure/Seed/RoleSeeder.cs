using Microsoft.EntityFrameworkCore;
using PontoApp.Infrastructure.EF;
using PontoApp.Domain.Entities;

namespace PontoApp.Infrastructure.Seed;

public static class RoleSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // garante que a tabela exista
        await db.Database.EnsureCreatedAsync();

        if (!await db.Set<Role>().AnyAsync())
        {
            db.AddRange(
                new Role { Name = "Admin" },
                new Role { Name = "Employee" }
            );
            await db.SaveChangesAsync();
        }
    }
}
