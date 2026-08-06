using DocuTrack.Application.Authorization;
using Microsoft.AspNetCore.Identity;

namespace DocuTrack.Api.Identity
{
    public static class RoleSeeder
    {
        private static readonly string[] Roles =
        [
            ApplicationRoles.Admin,
            ApplicationRoles.Reviewer,
            ApplicationRoles.Employee
        ];

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using IServiceScope scope = serviceProvider.CreateScope();

            RoleManager<IdentityRole<Guid>> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            foreach (var role in Roles)
            {
                if (await roleManager.RoleExistsAsync(role))
                {
                    continue;
                }

                IdentityResult result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create role '{role}'.");
                }
            }
        }
    }
}
