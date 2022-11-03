using AnimeSearch.Data;
using AnimeSearch.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace AnimeSearch.Api.Controllers;

public abstract class BaseApiController : ControllerBase, IAsyncActionFilter
{
    protected readonly AsmsearchContext _database;

    protected Users currentUser;
    protected Roles[] currentRoles;
    protected int Na;
    protected Roles MaxRole;

    protected BaseApiController(AsmsearchContext database)
    {
        _database = database;
        currentUser = null;
    }

    [NonAction]
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (User.Identity is {IsAuthenticated: true})
        {
            currentUser = await _database.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            currentUser.Derniere_visite = DateTime.Now;

            var entry = _database.Users.Update(currentUser);

            var rolesids = await _database.UserRoles.Where(ur => ur.UserId == currentUser.Id).Select(ur => ur.RoleId).ToListAsync();
            currentRoles = await _database.Roles.Where(r => rolesids.Contains(r.Id)).ToArrayAsync();

            if (currentRoles is {Length: > 0})
            {
                var r = currentRoles.MaxBy(r => r.NiveauAutorisation);
                Na = r.NiveauAutorisation;
                MaxRole = r;
            }

            await _database.SaveChangesAsync();
        }

        if (context.Result == null)
            await next();
        
        // after action...
    }
}