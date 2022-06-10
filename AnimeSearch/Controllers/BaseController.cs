using AnimeSearch.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AnimeSearch.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly AsmsearchContext _database;

        protected Users currentUser;
        protected Roles[] currentRoles;

        public BaseController(AsmsearchContext database)
        {
            _database = database;
            currentUser = null;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            ViewData["google-site-verification"] = (await _database.Settings.FirstOrDefaultAsync(s => s.Name == Utilities.SETTING_GOOGLE_SEARCH_ID_NAME))?.GetValueObject();


            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                currentUser = await _database.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

                currentUser.Derniere_visite = DateTime.Now;

                var entry = _database.Users.Update(currentUser);

                var rolesids = await _database.UserRoles.Where(ur => ur.UserId == currentUser.Id).Select(ur => ur.RoleId).ToListAsync();
                currentRoles = await _database.Roles.Where(r => rolesids.Contains(r.Id)).ToArrayAsync();

                if (currentRoles is {Length: > 0})
                {
                    var r = currentRoles.MaxBy(r => r.NiveauAutorisation);
                    ViewData["na"] = r.NiveauAutorisation;
                    ViewData["role"] = r;
                }

                await _database.SaveChangesAsync();
            }

            await base.OnActionExecutionAsync(context, next);
        }
    }
}
