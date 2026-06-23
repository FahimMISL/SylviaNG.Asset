using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Finbuckle.MultiTenant.Extensions;
using Microsoft.EntityFrameworkCore;
using SylviaNG.Assets.Application.Interfaces.Repositories;
using SylviaNG.Assets.Infrastructure.Data;
using SylviaNG.Assets.Infrastructure.Interceptors;
using SylviaNG.Assets.Infrastructure.Kafka;
using SylviaNG.Assets.Infrastructure.Repositories;
using SylviaNG.Assets.SharedKernel.Generic;
using SylviaNG.Assets.SharedKernel.Utils;

namespace SylviaNG.Assets.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Add your infrastructure services here

            var databaseProvider = configuration["Database:Provider"];
            var connectionString = configuration["Database:ConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Database connection string is not configured.");

            // Initialize timezone from configuration
            var timezoneId = configuration["RegionalSettings:TimezoneId"]
                ?? throw new InvalidOperationException("RegionalSettings:TimezoneId is not configured.");
            DateTimeUtility.Initialize(timezoneId);

            // Configure Finbuckle Multi-Tenant with Claim strategy (extracts tenant_id from JWT)
            services.AddMultiTenant<MultiTenancy.TenantInfo>()
                .WithClaimStrategy("tenant_id")  // Extract tenant from JWT claim 'tenant_id'
                .WithInMemoryStore(options =>
                {
                    // Default tenant for fallback
                    options.IsCaseSensitive = false;
                });

            // Register Audit Infrastructure (database-agnostic)
            services.AddHttpContextAccessor();
            services.AddSingleton<UtcDateTimeInterceptor>();

            // Configure database provider with audit interceptor
            services.AddDbContext<ApplicationDBContext>((sp, options) =>
            {
                var provider = NormalizeDatabaseProvider(databaseProvider);

                switch (provider)
                {
                    case "postgresql":
                        options.UseNpgsql(connectionString);
                        break;
                    case "sqlserver":
                        options.UseSqlServer(connectionString);
                        break;
                    case "oracle":
                        options.UseOracle(connectionString);
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Unsupported database provider: {databaseProvider}. Supported providers: PostgreSQL, SqlServer, Oracle.");
                }

                // Apply audit interceptor once (works with any database)
                options.AddInterceptors(sp.GetRequiredService<UtcDateTimeInterceptor>());
            });

            // Register your repositories here
            // Adding DI of repositories
            services.AddScoped<IAssetRepository, AssetRepository>();

            // Register Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Kafka
            services.Configure<KafkaSettings>(configuration.GetSection("Kafka"));
            services.AddHostedService<EmployeeEventConsumer>();

            return services;
        }

        private static string NormalizeDatabaseProvider(string? provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
                throw new ArgumentNullException(nameof(provider), "Database provider is not specified.");

            return provider.Trim().ToLowerInvariant();
        }
    }
}
