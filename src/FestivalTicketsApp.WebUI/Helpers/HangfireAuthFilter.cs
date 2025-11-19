using FestivalTicketsApp.Shared;
using Hangfire.Dashboard;

namespace FestivalTicketsApp.WebUI.Helpers;

public class HangfireAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        return httpContext.User.IsInRole(UserRolesConstants.Admin);
    }
}
