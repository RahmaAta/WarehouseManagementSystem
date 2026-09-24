using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WarehouseManagement.Application.Common.Interfaces;
using WarehouseManagement.Infrastructure.Jobs;
using WarehouseManagement.Infrastructure.Persistence;

namespace WarehouseManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, b =>
                b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // Security services
        services.AddSingleton<IPasswordHasher, Services.PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, Services.JwtTokenGenerator>();

        // Background Job Services
        services.AddScoped<ILowStockNotifierJob, LowStockNotifierJob>();
        services.AddScoped<IStaleOrderCleanupJob, StaleOrderCleanupJob>();
        services.AddScoped<IDailyInventorySnapshotJob, DailyInventorySnapshotJob>();

        // Hangfire Distributed Job Scheduling with SQL Server Storage
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                }));

            services.AddHangfireServer(options =>
            {
                options.WorkerCount = Math.Max(Environment.ProcessorCount * 2, 4);
                options.Queues = new[] { "critical", "default", "low" };
            });
        }

        return services;
    }
}
