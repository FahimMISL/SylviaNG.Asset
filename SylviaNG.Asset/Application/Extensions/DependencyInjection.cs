using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using RMS.Application.Features.ApprovalWorkflows.Services;
using RMS.Application.Features.EligibilityPolicies.Services;
using SylviaNG.Assets.Application.Interfaces.Services;
using SylviaNG.Assets.Application.Services;
using System.Reflection;

namespace SylviaNG.Assets.Application.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Add your application services here

            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly())
            );

            services.AddFluentValidationAutoValidation()
               .AddValidatorsFromAssembly(typeof(Program).Assembly);

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            // Register your services here
            // Adding DI of services

            #region Services -> Business Classes
            // Add asset-specific services here

            services.AddScoped<IAssetService, AssetService>();

            // Feature 3 - Approval Workflow Management
            services.AddScoped<ApprovalWorkflowEngine>();

            // Feature 4 - Eligibility & Policy Management
            services.AddScoped<PolicyEvaluationService>();

            // Provide access to HttpContext for request metadata enrichment
            services.AddHttpContextAccessor();
            #endregion

            return services;
        }
    }
}
