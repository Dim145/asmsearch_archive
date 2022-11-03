using AnimeSearch.Data;
using AnimeSearch.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AnimeSearch.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]

public class LevelAuthorize : Attribute, IAsyncAuthorizationFilter
{
    private readonly int niveauActuel;

    public LevelAuthorize(int niveauActuel = 0) => this.niveauActuel = niveauActuel;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if(!context.HttpContext.User.Identity.IsAuthenticated)
        {
            var accept = context.HttpContext.Request.Headers.Accept.ToString();

            if (accept.Contains("html") || accept.Equals("*/*"))
                context.Result = new RedirectToActionResult("LoginGet", "Identity", new {returnUrl = context.HttpContext.Request.Path});
            else
                context.Result = new UnauthorizedResult();

            return;
        }

        var _userManager = (UserManager<Users>) context.HttpContext.RequestServices.GetService(typeof(UserManager<Users>));
        var _roleManager = (RoleManager<Roles>) context.HttpContext.RequestServices.GetService(typeof(RoleManager<Roles>));

        Users currentUser = await _userManager.FindByNameAsync(context.HttpContext.User.Identity.Name);
        List<Roles> roles = (await _userManager.GetRolesAsync(currentUser)).Select(r => _roleManager.FindByNameAsync(r).GetAwaiter().GetResult()).ToList();

        if (roles.Contains(DataUtils.SuperAdminRole))
        {
            return;
        }

        if (roles.Max(r => r.NiveauAutorisation) < niveauActuel)
            context.Result = new UnauthorizedResult();
    }
}