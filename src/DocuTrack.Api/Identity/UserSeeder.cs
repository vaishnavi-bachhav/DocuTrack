using DocuTrack.Application.Authorization;
using DocuTrack.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace DocuTrack.Api.Identity
{
    public static class UserSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using IServiceScope scope = serviceProvider.CreateScope();

            UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            SeedAdminSettings settings = scope.ServiceProvider.GetRequiredService<IOptions<SeedAdminSettings>>().Value;

            if (string.IsNullOrWhiteSpace(settings.Email) ||
                string.IsNullOrWhiteSpace(settings.Password))
            {
                return;
            }

            ApplicationUser? admin = await userManager.FindByEmailAsync(settings.Email);

            if (admin is null)
            {
                admin = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    FullName = settings.FullName,
                    UserName = settings.Email,
                    Email = settings.Email,
                    EmailConfirmed = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                IdentityResult createResult = await userManager.CreateAsync(admin, settings.Password);

                if (!createResult.Succeeded)
                {
                    string errors = string.Join(
                       "; ",
                       createResult.Errors.Select(e => e.Description));

                    throw new InvalidOperationException($"Failed to create seeded admin user: {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(admin, ApplicationRoles.Admin))
            {
                IdentityResult roleResult = await userManager.AddToRoleAsync(admin, ApplicationRoles.Admin);

                if (!roleResult.Succeeded)
                {
                    string errors = string.Join(
                       "; ",
                       roleResult.Errors.Select(e => e.Description));

                    throw new InvalidOperationException($"Failed to assign Admin role: {errors}");
                }
            }
        }
    }
}
