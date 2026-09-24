using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace WarehouseManagement.API.Filters;

/// <summary>
/// Secures the Hangfire dashboard by verifying that requests originate either from local
/// development loopback or from authenticated users with the 'Admin' role.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize([NotNull] DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return Authorize(httpContext);
    }

    public bool Authorize(HttpContext? httpContext)
    {
        if (httpContext == null)
        {
            return false;
        }

        // Allow localhost loopback access for development convenience
        var host = httpContext.Request.Host.Host;
        if (host is "localhost" or "127.0.0.1" or "::1")
        {
            return true;
        }

        // In staging/production, require authenticated user with Admin role
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole("Admin");
    }
}
