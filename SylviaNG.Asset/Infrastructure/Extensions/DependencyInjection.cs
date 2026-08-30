using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Finbuckle.MultiTenant.Extensions;
using Microsoft.EntityFrameworkCore;
using RMS.Application.Interfaces;
using RMS.Infrastructure.Data;
using RMS.Infrastructure.Repositories;
using RMS.Infrastructure.Services;
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

            // Requisition module (Feature 1/2) uses its own bounded-context DbContext,
            // same physical database, same provider switch as ApplicationDBContext above.
            services.AddDbContext<RmsDbContext>((sp, options) =>
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

                options.AddInterceptors(sp.GetRequiredService<UtcDateTimeInterceptor>());
            });

            // Register your repositories here
            // Adding DI of repositories
            services.AddScoped<IAssetRepository, AssetRepository>();
            services.AddScoped<IRequisitionRepository, RequisitionRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ICostCenterRepository, CostCenterRepository>();
            services.AddScoped<IRequisitionExistenceChecker, RequisitionExistenceChecker>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IAuditLogger, AuditLogger>();
            services.AddScoped<IFileStorageService, LocalFileStorageService>();

            // Feature 3 - Approval Workflow Management
            services.AddScoped<IApprovalWorkflowRepository, ApprovalWorkflowRepository>();
            services.AddScoped<IRequisitionApprovalRepository, RequisitionApprovalRepository>();
            services.AddScoped<IApprovalDelegationRepository, ApprovalDelegationRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<INotificationService, NoOpNotificationService>();
            services.AddHostedService<SlaBreachEscalationService>();

            // Feature 4 - Eligibility & Policy Management
            services.AddScoped<IEligibilityPolicyRepository, EligibilityPolicyRepository>();

            // Register Unit of Work
            services.AddScoped<SylviaNG.Assets.SharedKernel.Generic.IUnitOfWork, UnitOfWork>();
            services.AddScoped<RMS.Application.Interfaces.IUnitOfWork>(sp => sp.GetRequiredService<RmsDbContext>());

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
