using DocuTrack.Application.Abstractions.Authentication;
using DocuTrack.Application.Authentication;
using DocuTrack.Application.Documents;
using Microsoft.Extensions.DependencyInjection;

namespace DocuTrack.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(
        this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();

            return services;
        }
    }
}
