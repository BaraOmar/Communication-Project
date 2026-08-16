using CommunicationProject.Models;
using CommunicationProject.Security;
using Microsoft.AspNetCore.Identity;

namespace CommunicationProject.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            var roleManager =
                serviceProvider.GetRequiredService<
                    RoleManager<IdentityRole>>();

            var userManager =
                serviceProvider.GetRequiredService<
                    UserManager<ApplicationUser>>();

            await CreateRolesAsync(roleManager);

            await CreateAdminAsync(
                userManager,
                configuration);
        }

        private static async Task CreateRolesAsync(
            RoleManager<IdentityRole> roleManager)
        {
            foreach (string roleName in AppRoles.All)
            {
                bool roleExists =
                    await roleManager.RoleExistsAsync(roleName);

                if (roleExists)
                {
                    continue;
                }

                var result = await roleManager.CreateAsync(
                    new IdentityRole(roleName));

                if (!result.Succeeded)
                {
                    string errors = string.Join(
                        "; ",
                        result.Errors.Select(error =>
                            error.Description));

                    throw new InvalidOperationException(
                        $"Could not create role '{roleName}': " +
                        errors);
                }
            }
        }

        private static async Task CreateAdminAsync(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            string? email =
                configuration["SeedAdmin:Email"];

            string? password =
                configuration["SeedAdmin:Password"];

            string fullName =
                configuration["SeedAdmin:FullName"]
                ?? "System Administrator";

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "SeedAdmin email and password are not configured.");
            }

            ApplicationUser? admin =
                await userManager.FindByEmailAsync(email);

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    EmailConfirmed = true
                };

                IdentityResult createResult =
                    await userManager.CreateAsync(
                        admin,
                        password);

                if (!createResult.Succeeded)
                {
                    string errors = string.Join(
                        "; ",
                        createResult.Errors.Select(error =>
                            error.Description));

                    throw new InvalidOperationException(
                        "Could not create the initial Admin: " +
                        errors);
                }
            }

            bool isAdmin =
                await userManager.IsInRoleAsync(
                    admin,
                    AppRoles.Admin);

            if (!isAdmin)
            {
                IdentityResult roleResult =
                    await userManager.AddToRoleAsync(
                        admin,
                        AppRoles.Admin);

                if (!roleResult.Succeeded)
                {
                    string errors = string.Join(
                        "; ",
                        roleResult.Errors.Select(error =>
                            error.Description));

                    throw new InvalidOperationException(
                        "Could not assign the Admin role: " +
                        errors);
                }
            }
        }
    }
}