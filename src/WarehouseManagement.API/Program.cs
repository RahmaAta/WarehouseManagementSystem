using System.Text;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using WarehouseManagement.API.Filters;
using WarehouseManagement.API.Services;
using WarehouseManagement.Application;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Application and Infrastructure layer dependencies
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 2. Add HTTP Context Accessor & Current User Service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// 3. Configure JWT Authentication
var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? "WarehouseManagementSystemSuperSecretKeyForJwtAuthentication2026!";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "WarehouseManagementAPI";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "WarehouseManagementClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

builder.Services.AddControllers();

// 4. Configure native .NET OpenAPI specification
builder.Services.AddOpenApi();

var app = builder.Build();

// Global Exception Handling Middleware
app.UseMiddleware<WarehouseManagement.API.Middlewares.ExceptionHandlingMiddleware>();

// Configure OpenAPI & Scalar API reference documentation
if (app.Environment.IsDevelopment())
{
    // Native .NET OpenAPI endpoint (/openapi/v1.json)
    app.MapOpenApi();

    // Scalar Interactive API Explorer (/scalar)
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Warehouse & Inventory Management API")
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });

    // Developer convenience redirects
    app.MapGet("/swagger", () => Results.Redirect("/scalar/v1"));
    app.MapGet("/scalar", () => Results.Redirect("/scalar/v1"));
}

app.UseHttpsRedirection();

// Authentication MUST come before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard and Recurring Jobs
if (!string.IsNullOrEmpty(builder.Configuration.GetConnectionString("DefaultConnection")))
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        DashboardTitle = "Warehouse Management WMS - Job Scheduler",
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });

    using var scope = app.Services.CreateScope();
    var recurringJobManager = scope.ServiceProvider.GetService<IRecurringJobManager>();
    if (recurringJobManager != null)
    {
        recurringJobManager.AddOrUpdate<ILowStockNotifierJob>(
            "low-stock-check-hourly",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Hourly);

        recurringJobManager.AddOrUpdate<IStaleOrderCleanupJob>(
            "stale-order-cleanup-daily",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Daily);

        recurringJobManager.AddOrUpdate<IDailyInventorySnapshotJob>(
            "daily-inventory-snapshot",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Daily(23, 0));
    }
}

app.MapControllers();

app.Run();
