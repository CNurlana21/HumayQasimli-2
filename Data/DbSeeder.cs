using HumayQasimli.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Data
{
    /// <summary>
    /// Seeds roles, a default admin account, default categories and default site settings.
    /// Admin credentials are read from configuration (User Secrets / environment variables),
    /// never hard-coded. If SeedAdmin:Email / a password secret is missing, seeding of the
    /// admin account is skipped and a warning is logged so the app still starts.
    /// </summary>
    public static class DbSeeder
    {
        public const string AdminRole = "Admin";
        public const string UserRole = "User";

        public static async Task SeedAsync(IServiceProvider services)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var configuration = services.GetRequiredService<IConfiguration>();
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

            await db.Database.MigrateAsync();

            // ---- Roles ----
            foreach (var role in new[] { AdminRole, UserRole })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // ---- Default admin ----
            var adminEmail = configuration["SeedAdmin:Email"];
            var adminPassword = configuration["SeedAdmin:Password"]; // supply via User Secrets / env var
            var adminUserName = configuration["SeedAdmin:UserName"] ?? "admin";

            if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
            {
                var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
                if (existingAdmin == null)
                {
                    var admin = new ApplicationUser
                    {
                        UserName = adminUserName,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        FullName = "Humay Qasimli",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    var result = await userManager.CreateAsync(admin, adminPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(admin, AdminRole);
                        logger.LogInformation("Default admin account created for {Email}.", adminEmail);
                    }
                    else
                    {
                        logger.LogWarning("Failed to create default admin: {Errors}",
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
            else
            {
                logger.LogWarning("SeedAdmin:Email / SeedAdmin:Password not configured. " +
                                   "Skipping default admin creation. Set them via User Secrets " +
                                   "(see README) then restart the app.");
            }

            // ---- Default categories ----
            if (!await db.Categories.AnyAsync())
            {
                db.Categories.AddRange(
                    new Category { Name = "Karyera", Slug = "karyera", Description = "Karyera inkisafi ile bagli mezmun" },
                    new Category { Name = "Sexsi inkisaf", Slug = "sexsi-inkisaf", Description = "Sexsi inkisaf ve motivasiya" },
                    new Category { Name = "Liderlik", Slug = "liderlik", Description = "Liderlik ve idareetme" },
                    new Category { Name = "Texnologiya", Slug = "texnologiya", Description = "Texnologiya ve reqemsal bacariqlar" }
                );
                await db.SaveChangesAsync();
            }

            // ---- Default site settings (single row) ----
            if (!await db.SiteSettings.AnyAsync())
            {
                db.SiteSettings.Add(new SiteSetting
                {
                    SiteName = "Humay Qasimli",
                    HeroTitle = "Humay Qasimli",
                    HeroSubtitle = "Mentor | Kocluq | Sexsi brend",
                    AboutTitle = "Haqqimda",
                    AboutContent = "Bu bolme Admin Panelden redakte edile biler.",
                    InstagramUrl = "https://instagram.com/",
                    LinkedInUrl = "https://linkedin.com/",
                    WhatsAppNumber = "",
                    ContactEmail = adminEmail ?? "info@example.com",
                    FooterText = $"© {DateTime.UtcNow.Year} Humay Qasimli. Butun huquqlar qorunur.",
                    UpdatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
        }
    }
}
