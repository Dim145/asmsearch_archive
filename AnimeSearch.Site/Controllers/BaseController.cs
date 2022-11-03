using AnimeSearch.Core;
using AnimeSearch.Data;
using AnimeSearch.Data.Models;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace AnimeSearch.Site.Controllers;

public abstract class BaseController : Controller
{
    private static readonly Type HtmlType = typeof(HtmlNode);
    
    protected readonly AsmsearchContext _database;

    protected Users currentUser;
    protected Roles[] currentRoles;
    
    private HtmlNodeConverter HtmlNodeConverter { get; }

    protected BaseController(AsmsearchContext database)
    {
        _database = database;
        currentUser = null;

        HtmlNodeConverter = new();
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ViewData["google-site-verification"] = (await _database.Settings.FirstOrDefaultAsync(s => s.Name == DataUtils.SettingGoogleSearchIdName))?.GetValueObject();
        ViewData["settings_balises"] = (await _database.Settings.Where(s => s.TypeValue == HtmlType.FullName).ToListAsync()).Select(s => s.GetValueObject<HtmlNode>(converters: HtmlNodeConverter)).ToArray();

        if (User?.Identity != null && User.Identity.IsAuthenticated)
        {
            currentUser = await _database.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            currentUser!.Derniere_visite = DateTime.Now;

            var entry = _database.Users.Update(currentUser);

            var rolesids = await _database.UserRoles.Where(ur => ur.UserId == currentUser.Id).Select(ur => ur.RoleId).ToListAsync();
            currentRoles = await _database.Roles.Where(r => rolesids.Contains(r.Id)).ToArrayAsync();

            if (currentRoles is {Length: > 0})
            {
                var r = currentRoles.MaxBy(r => r.NiveauAutorisation);
                ViewData["na"] = r?.NiveauAutorisation ?? 0;
                ViewData["role"] = r;
            }

            await _database.SaveChangesAsync();
        }

        await base.OnActionExecutionAsync(context, next);
    }
}