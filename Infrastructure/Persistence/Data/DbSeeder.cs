using Microsoft.Extensions.DependencyInjection;
using Application.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Persistence.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager =
            services.GetRequiredService<RoleManager<IdentityRole<int>>>();

        var userManager =
            services.GetRequiredService<UserManager<User>>();

        string[] roles = { "Admin", "Organizer", "User" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(
                    new IdentityRole<int>(role));
            }
        }

        const string email = "admin@mail.com";

        var admin = await userManager.FindByEmailAsync(email);

        if (admin == null)
        {
            admin = new User
            {
                FirstName = "Admin",
                LastName = "Admin",
                BirthDate = new DateOnly(1990, 1, 1),
                Email = email,
                UserName = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(
                admin, "Admin123");

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}
