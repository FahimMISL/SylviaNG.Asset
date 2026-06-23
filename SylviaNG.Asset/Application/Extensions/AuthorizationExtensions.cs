using Microsoft.AspNetCore.Authorization;

namespace SylviaNG.Assets.Application.Extensions
{
    public static class AuthorizationExtensions
    {
        public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // Add asset-specific authorization policies here
            });

            return services;
        }
    }
}
