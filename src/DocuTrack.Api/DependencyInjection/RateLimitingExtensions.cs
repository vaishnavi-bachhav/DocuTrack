using System.Threading.RateLimiting;

namespace DocuTrack.Api.DependencyInjection
{
    public static class RateLimitingExtensions
    {
        public const string LoginPolicy = "login";
        public const string RegistrationPolicy = "registration";
        public const string RefreshPolicy = "refresh";

        public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode =
                    StatusCodes.Status429TooManyRequests;

                options.AddPolicy(
                    LoginPolicy,
                    context =>
                        RateLimitPartition
                            .GetFixedWindowLimiter(
                                GetClientKey(context),
                                _ =>
                                    new FixedWindowRateLimiterOptions
                                    {
                                        PermitLimit = 5,
                                        Window =
                                            TimeSpan.FromMinutes(1),
                                        QueueLimit = 0,
                                        AutoReplenishment = true
                                    }));

                options.AddPolicy(
                    RegistrationPolicy,
                    context =>
                        RateLimitPartition
                            .GetFixedWindowLimiter(
                                GetClientKey(context),
                                _ =>
                                    new FixedWindowRateLimiterOptions
                                    {
                                        PermitLimit = 3,
                                        Window =
                                            TimeSpan.FromHours(1),
                                        QueueLimit = 0,
                                        AutoReplenishment = true
                                    }));

                options.AddPolicy(
                    RefreshPolicy,
                    context =>
                        RateLimitPartition
                            .GetFixedWindowLimiter(
                                GetClientKey(context),
                                _ =>
                                    new FixedWindowRateLimiterOptions
                                    {
                                        PermitLimit = 20,
                                        Window =
                                            TimeSpan.FromMinutes(1),
                                        QueueLimit = 0,
                                        AutoReplenishment = true
                                    }));
            });

            return services;
        }

        private static string GetClientKey(
            HttpContext context)
        {
            return context.Connection
                       .RemoteIpAddress?
                       .ToString()
                   ?? "unknown-client";
        }
    }
}
