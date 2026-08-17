using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using SylviaNG.Assets.Application.Extensions;
using SylviaNG.Assets.Infrastructure.Extensions;
using SylviaNG.Assets.Middlewares;
using SylviaNG.Assets.SharedKernel.Utils;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddGrpcServices(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddControllers();



builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SylviaNG Asset API",
        Version = "v1",
        Description = "Asset Management API with Keycloak Authentication"
    });

    // Add JWT Bearer Authentication
    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(document =>
    new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = new List<string>()
    });
});


// Add Keycloak Authentication
builder.Services.AddKeycloakJwtAuthentication(builder.Configuration);

builder.Services.AddAuthorizationPolicies();

// Add global authorization policy - all endpoints require authentication by default
builder.Services.AddControllers(options =>
{
    // Global authorization filter - all endpoints require authentication
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new LocalDateTimeJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableLocalDateTimeJsonConverter());
    });




var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // DEV-ONLY: see RmsDevelopmentSeeder / CurrentUserService.FallbackDevUser - real login
    // isn't wired yet, so this seeds the identity Feature 1/2 writes get attributed to.
    using var scope = app.Services.CreateScope();
    var rmsDb = scope.ServiceProvider.GetRequiredService<RMS.Infrastructure.Data.RmsDbContext>();
    await RMS.Infrastructure.Data.RmsDevelopmentSeeder.SeedAsync(rmsDb);
}

app.UseCors("AllowAll");

app.UseMiddleware<ResponseWrappingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.MapControllers();

app.Run();
