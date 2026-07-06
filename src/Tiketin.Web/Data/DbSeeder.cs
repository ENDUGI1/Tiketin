using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tiketin.Web.Domain;

namespace Tiketin.Web.Data;

/// <summary>
/// Idempotent seeder. Roles and categories are always ensured; demo users and demo
/// content are seeded in Development only.
/// </summary>
public static class DbSeeder
{
    public const string DevPassword = "Tiketin123!";

    public static readonly string[] Roles = ["Admin", "Technician", "Employee"];

    public static async Task SeedAsync(IServiceProvider services, bool isDevelopment)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        await db.Database.MigrateAsync();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        await SeedCategoriesAsync(db);

        var configuration = services.GetRequiredService<IConfiguration>();

        if (isDevelopment)
        {
            await SeedUsersAsync(userManager);

            // Integration tests set SkipDemoSeed: they need users, not demo content.
            if (!configuration.GetValue<bool>("SkipDemoSeed"))
            {
                await DemoDataSeeder.SeedAsync(db);
            }
        }
        else
        {
            await BootstrapAdminAsync(userManager, configuration);
        }
    }

    /// <summary>
    /// Production first-run: creates the initial admin from Bootstrap:AdminEmail /
    /// Bootstrap:AdminPassword when no user holds the Admin role yet.
    /// </summary>
    private static async Task BootstrapAdminAsync(UserManager<AppUser> userManager, IConfiguration configuration)
    {
        if ((await userManager.GetUsersInRoleAsync("Admin")).Count > 0)
        {
            return;
        }

        var email = configuration["Bootstrap:AdminEmail"];
        var password = configuration["Bootstrap:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var admin = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Administrator",
            Department = "IT",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed creating bootstrap admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRoleAsync(admin, "Admin");
    }

    private static async Task SeedCategoriesAsync(AppDbContext db)
    {
        // (name, sla response minutes, sla resolution minutes)
        (string Name, int Response, int Resolution)[] categories =
        [
            ("Hardware", 60, 480),
            ("Printer", 60, 240),
            ("Jaringan", 30, 240),
            ("Aplikasi", 120, 960),
            ("Akun & Akses", 60, 240),
            ("Lainnya", 120, 960)
        ];

        foreach (var (name, response, resolution) in categories)
        {
            if (!await db.Categories.AnyAsync(c => c.Name == name))
            {
                db.Categories.Add(new Category
                {
                    Name = name,
                    SlaResponseMinutes = response,
                    SlaResolutionMinutes = resolution
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(UserManager<AppUser> userManager)
    {
        (string Email, string FullName, string Department, string Role)[] users =
        [
            ("admin@tiketin.local", "Rachmat Hidayat", "IT", "Admin"),
            ("bagus.prasetyo@tiketin.local", "Bagus Prasetyo", "IT", "Technician"),
            ("dewi.anggraini@tiketin.local", "Dewi Anggraini", "IT", "Technician"),
            ("yoga.firmansyah@tiketin.local", "Yoga Firmansyah", "IT", "Technician"),
            ("andini.puspitasari@tiketin.local", "Andini Puspitasari", "Finance", "Employee"),
            ("fajar.ramadhan@tiketin.local", "Fajar Ramadhan", "Marketing", "Employee"),
            ("rina.wulandari@tiketin.local", "Rina Wulandari", "HSSE", "Employee"),
            ("hendra.gunawan@tiketin.local", "Hendra Gunawan", "Operations", "Employee"),
            ("maya.kartika@tiketin.local", "Maya Kartika", "HR", "Employee"),
            ("taufik.nugroho@tiketin.local", "Taufik Nugroho", "Finance", "Employee"),
            ("lestari.handayani@tiketin.local", "Lestari Handayani", "Procurement", "Employee"),
            ("aditya.saputra@tiketin.local", "Aditya Saputra", "Marketing", "Employee")
        ];

        foreach (var (email, fullName, department, role) in users)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
            {
                continue;
            }

            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                Department = department,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var result = await userManager.CreateAsync(user, DevPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed seeding user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            await userManager.AddToRoleAsync(user, role);
        }
    }
}
