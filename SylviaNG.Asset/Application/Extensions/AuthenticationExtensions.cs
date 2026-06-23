using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace SylviaNG.Assets.Application.Extensions
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddKeycloakJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var keycloakSection = configuration.GetSection("Keycloak");
            if (!keycloakSection.Exists())
            {
                throw new ArgumentException("Keycloak configuration section is missing in appsettings.json");
            }

            var authority = keycloakSection.GetValue<string>("Authority") ?? throw new ArgumentNullException("Keycloak:Authority");
            var clientId = keycloakSection.GetValue<string>("ClientId") ?? throw new ArgumentNullException("Keycloak:ClientId");
            var requireHttps = keycloakSection.GetValue<bool>("RequireHttpsMetadata", false);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = clientId;
                options.RequireHttpsMetadata = requireHttps;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidAudience = clientId,
                    ValidateIssuer = true,
                    ValidIssuer = authority,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5),
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine("Token validated successfully");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        var response = new
                        {
                            hasError = true,
                            decentMessage = "Unauthorized. Authentication is required.",
                            errorDetails = context.ErrorDescription ?? "Invalid or missing authentication token.",
                            content = (object?)null
                        };

                        var json = System.Text.Json.JsonSerializer.Serialize(response);
                        return context.Response.WriteAsync(json);
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";

                        var response = new
                        {
                            hasError = true,
                            decentMessage = "Forbidden. You don't have permission to access this resource.",
                            errorDetails = "Access denied. Insufficient permissions.",
                            content = (object?)null
                        };

                        var json = System.Text.Json.JsonSerializer.Serialize(response);
                        return context.Response.WriteAsync(json);
                    }
                };
            });

            return services;
        }
    }
}
