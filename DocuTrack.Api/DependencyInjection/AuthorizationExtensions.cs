using DocuTrack.Application.Authorization;

namespace DocuTrack.Api.DependencyInjection;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddApiAuthorization(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicies.ReviewDocuments,
                policy => policy.RequireRole(
                    ApplicationRoles.Reviewer,
                    ApplicationRoles.Admin));

            options.AddPolicy(
                AuthorizationPolicies.DeleteDocuments,
                policy => policy.RequireRole(
                    ApplicationRoles.Admin));
        });

        return services;
    }
}