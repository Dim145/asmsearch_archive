using AnimeSearch.Data;
using AnimeSearch.Data.Models;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AnimeSearch.Site.Authorizations;

public class HangfireDashboardAuth: IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (!(httpContext.User.Identity?.IsAuthenticated ?? false))
            return false;

        var userManager = httpContext.RequestServices.GetService<UserManager<Users>>();

        var user = userManager!.GetUserAsync(httpContext.User).GetAwaiter().GetResult();
        var roles = userManager!.GetRolesAsync(user).GetAwaiter().GetResult();

        return roles.Contains(DataUtils.SuperAdminRole.Name) || roles.Contains(DataUtils.AdminRole.Name);
    }
}