using invmgmt.web.Models;
using invmgmt.web.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using invmgmt.web.Data;

namespace invmgmt.web.Utils
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

            try
            {
                // 1. Seed Roles if missing (Legacy, keep if other parts still use it)
                if (!await context.Roles.AnyAsync())
                {
                    context.Roles.AddRange(
                        new Role { Id = 1, Name = "User" },
                        new Role { Id = 2, Name = "Issuer" },
                        new Role { Id = 3, Name = "Admin" }
                    );
                    await context.SaveChangesAsync();
                }

                // 2. Seed Departments if missing
                if (!await context.Departments.AnyAsync())
                {
                    context.Departments.AddRange(
                        new Department { Id = 1, Name = "Admin" },
                        new Department { Id = 2, Name = "IT" },
                        new Department { Id = 3, Name = "HR" },
                        new Department { Id = 4, Name = "Finance" }
                    );
                    await context.SaveChangesAsync();
                }

                // 3. Seed Categories if missing
                if (!await context.Categories.AnyAsync())
                {
                    context.Categories.AddRange(
                        new Category { Name = "Stationary" },
                        new Category { Name = "IT Related" },
                        new Category { Name = "HouseKeeping" }
                    );
                    await context.SaveChangesAsync();
                    logger.LogInformation("Categories seeded successfully");
                }

                // Admin seeding is now handled securely in Program.cs using environment variables and BCrypt.
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during database seeding.");
            }
        }
    }
}
