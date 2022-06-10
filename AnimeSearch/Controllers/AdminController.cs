using AnimeSearch.Attributes;
using AnimeSearch.Database;
using AnimeSearch.Models;
using AnimeSearch.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using static AnimeSearch.Utilities;

namespace AnimeSearch.Controllers
{
    [Route("admin")]
    [LevelAuthorize(1)]
    public class AdminController : BaseController
    {
        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<Roles> _roleManager;

        public AdminController(AsmsearchContext database, RoleManager<Roles> roleManager, UserManager<Users> userManager): base(database)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            ViewData["heure"] = DateTime.Now;

            if (BASE_URL == null)
            {
                BASE_URL = Request.Scheme + "://" + Request.Host.ToString();

                if (!BASE_URL.EndsWith("/"))
                    BASE_URL += "/";
            }

            IIdentity userIdentity = User.Identity;

            string username = userIdentity?.Name;

            if (!string.IsNullOrWhiteSpace(username))
            {
                try
                {
                    Users user = await _database.Users.FirstOrDefaultAsync(item => item.UserName == username);

                    if (user == null)
                    {
                        var userAgent = HttpContext.Request.Headers["User-Agent"];
                        string uaString = Convert.ToString(userAgent[0]);

                        user = new() { UserName = username, Navigateur = uaString };

                        await _database.Users.AddAsync(user);
                        await _database.SaveChangesAsync(); // afin d'obtenir l'id
                    }

                    string ip = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();

                    if (ip != null)
                    {
                        IP ipExist = await _database.IPs.FirstOrDefaultAsync(item => item.Adresse_IP == ip && item.Users_ID == user.Id);

                        if (ipExist == null)
                        {
                            ipExist = new() { Adresse_IP = ip, Users_ID = user.Id, Derniere_utilisation = DateTime.Now };

                            await ipExist.UpdateLocalisation();

                            await _database.IPs.AddAsync(ipExist);
                        }
                        else
                        {
                            ipExist.Derniere_utilisation = DateTime.Now;

                            if (string.IsNullOrWhiteSpace(ipExist.Localisation))
                                await ipExist.UpdateLocalisation();

                            _database.IPs.Update(ipExist);
                        }
                    }

                    if (user != null)
                    {
                        user.Derniere_visite = DateTime.Now;
                        user.Dernier_Acces_Admin = DateTime.Now;

                        _database.Users.Update(user);

                        var role = (await _userManager.GetRolesAsync(user)).Select(r => _roleManager.FindByNameAsync(r).GetAwaiter().GetResult()).ToList();

                        if (role != null && role.Count > 0)
                        {
                            var r = role.MaxBy(r => r.NiveauAutorisation);
                            ViewData["na"] = r.NiveauAutorisation;
                            ViewData["role"] = r;
                        }

                        ViewData["user"] = user;
                    }

                    await _database.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    AddExceptionError("Le OnActionExcecuting de l'Admin", e);
                }
            }

            await base.OnActionExecutionAsync(context, next);
        }

        [HttpGet]
        [LevelAuthorize(2)]
        public async Task<IActionResult> IndexAsync()
        {
            ViewData["users"] = await _database.Users.OrderByDescending(u => u.Derniere_visite).ToArrayAsync();
            ViewData["isSuperAdmin"] = await _userManager.IsInRoleAsync(await _userManager.FindByNameAsync(User.Identity.Name), SUPER_ADMIN_ROLE.Name);

            return View();
        }

        [HttpGet("sites")]
        public IActionResult SitesAsync()
        {
            ViewData["time_before_cs_service"] = GetTimeBeforeNextCheckSiteService();

            ViewData["aideHTML"] = "<p>Liste des sites avec quelques informations basiques. De la même façon que pour les citations, il faut faire un clic droit pour modifier un site ou pour mettre l'état du site en erreur du type 404 (site introuvable)</p>" +
                "<p>Pour plus d'informations sur ce système d'erreur, se référer à la page de modification.</p>";

            return View();
        }

        [HttpPost("sites/edit")]
        [AutoValidateAntiforgeryToken]
        [LevelAuthorize(4)]
        public async Task<IActionResult> EditerSite([FromForm] string Url)
        {
            if (string.IsNullOrWhiteSpace(Url))
                return RedirectToAction("sites", "admin");

            bool canModifyURL = ViewData["na"] is not null and int na && na >= DROIT_ADD;

            ViewData["types"] = await _database.TypeSites.ToArrayAsync();
            ViewData["isSuperAdmin"] = canModifyURL;

            ViewData["aideHTML"] = "<p>Page permettant de modifier les données d'un site. La page contient moins de restrictions que lors de l'ajout d'un site. (Notamment sur les données \"POST\").</p>" +
                "<p>Le changement d'état permet d'indiquer aux autres admins ou au super-admin l'état du site et empêche le serveur d'utiliser ce site lors de des recherches.</p>" +
                "<p>Voici la signification des erreurs de bases:" +
                "<ul>" +
                "<li>L'erreur 404 signifie que le site est introuvable (adresse URL changée) => Il faut changer l'adresse URL, seul le super-admin peut faire cela. (le contacter)</li>" +
                "<li>L'erreur CloudFlare signifie que le site est fonctionnel mais que CloudFlare a bloqué l'adresse IP du serveur (temporairement ou pas). => Rien à faire à part attendre si le blocage est temporaire.</li>" +
                "<li>L'erreur 0 résultat signifie que le serveur ne trouve plus aucun résultat sur le site. => La structure du site a dû changer, il faut mettre à jour le champ \"chemin vers balise à</li>" +
                "</ul></p>" +
                "<p>Le bouton testé permet de demander au serveur d'exécuter une recherche qu'avec les données saisies du site (celle dans les champs). Pour le moment seul la recherche One Piece est possible.</p>";


            ViewData["site"] = await _database.Sites.FirstOrDefaultAsync(s => s.Url == Url);

            bool ok = ViewData["site"] is not null;

            return ok ? View("SitesEditer") : RedirectToAction("sites", "admin");

        }

        [HttpGet("citations")]
        public IActionResult CitationsAsync()
        {
            ViewData["aideHTML"] = "<p>Liste des citations triées par date d'ajout. Pour valider ou invalider une citation, il faut faire un clic droit dessus.</p>";

            return View();
        }

        [HttpGet("types")]
        [LevelAuthorize(4)]
        public IActionResult TypeSites()
        {
            ViewData["aideHTML"] = "<p>Cette page permet d'éditer les propositions de types de sites.</p>" +
                "<p>Tous les types ne sont pas directement relier aux sites. Donc supprimer un type ne cause aucuns problèmes et " +
                "ne change pas le type des sites qui l'utilisent. Cela ne change que les propositions lors de l'ajout ou de la modification de sites.</p>";

            return View();
        }

        [HttpGet("recherches/{userId}")]
        [LevelAuthorize(2)]
        public async Task<IActionResult> Recherches(int userId, int page = 0)
        {
            if (userId <= 0)
                return BadRequest("Id Invalide");

            ViewData["selected user"] = await _database.Users.FirstOrDefaultAsync(u => u.Id == userId);
            ViewData["page"] = page;

            return View();
        }

        [HttpGet("ips/{userId}")]
        [LevelAuthorize(2)]
        public async Task<IActionResult> Ips(int userId)
        {
            if (userId <= 0)
                return BadRequest("Id Invalide");

            ViewData["selected user"] = await _database.Users.FirstOrDefaultAsync(u => u.Id == userId);
            ViewData["aideHTML"] = "<p>Liste des adresses IP d'un utilisateur avec la localisation de chaque adresse. Il est possible de cliquer sur les localisations pour ouvrir une page google maps.</p>";

            return View();
        }

        [HttpGet("dons/{userId}")]
        [LevelAuthorize(6)]
        public async Task<IActionResult> Dons(int userId)
        {
            if (userId <= 0)
                return BadRequest("Id Invalide");

            ViewData["selected user"] = await _database.Users.FirstOrDefaultAsync(u => u.Id == userId);
            ViewData["pp"] = LAST_DONS_SERVICE.GetTempsProchainPassage();

            ViewData["aideHTML"] = "<p>Cette page montre tous les evntuels dons d'un utilisateur.<br /> Ceux-ci ne sont modifiable que si ils ne sont pas validée depuis plus d'une heure et que le service n'est pas encore passé.</p>";

            return View();
        }

        [HttpGet("services")]
        [HttpPost("services")]
        [LevelAuthorize(4)]
        public async Task<IActionResult> ServicesAsync([FromForm] int id = -1, [FromForm] int val = -1)
        {
            if (id > -1 && id < SERVICES.Count && val > -1 && val < 3)
            {
                BaseService currentService = SERVICES[id];

                switch (val)
                {
                    case 0: await currentService.StartAsync(new());break;
                    case 1: await currentService.StopAsync(new());break;
                    case 2: await currentService.ExecutionCode();break;
                }
            }

            ViewData["services"] = SERVICES.ToArray();

            return View();
        }

        [Route("Error")]
        [LevelAuthorize(2)]
        public IActionResult Error()
        {
            ViewData["errors"] = Errors.ToArray();

            return View();
        }

        [Authorize(Roles = "Super-Admin")]
        [HttpGet("Roles")]
        public IActionResult Roles()
        {
            ViewData["aideHTML"] = "<p>Les numéro associé au rôle correspond au niveau du droit de celui-ci. Ce numéro va de 1 à 6, six étant le niveau maximum de droits*</p>" +
                "<p>les numéros correspondent aux droits suivants:" +
                "<ul>" +
                    "<li>1: droit de vue basique. (ex: citations /sites)</li>" +
                    "<li>2: droits de vues complètes. Permets de voir toutes les pages admin. (sauf celle-ci)</li>" +
                    "<li>3: droit de modifications basique. Équivalant aux droits \"modérateurs\". (ex: valider des citations)</li>" +
                    "<li>4: droits de modifications complètes. (ex: modifier les données des sites. Excepté l'adresse URL)</li>" +
                    "<li>5: droit d'ajout de données (ex: types de sites etc...)</li>" +
                    "<li>6: droit de suppression des données. (ex: sites/users) presque équivalent au Super-Admin. N'ont pas les droits d'écriture sur les rôles</li>" +
                "</ul>";

            return View();
        }

        [HttpGet("SavedSearchsForUser/{id}")]
        [LevelAuthorize(2)]
        public async Task<IActionResult> SavedSearchsForUserAsync(int id)
        {
            ViewData["selected user"] = await _database.Users.FirstOrDefaultAsync(u => u.Id == id);

            return View();
        }

        [HttpGet("SavedSearch/{userId}/{search}")]
        [LevelAuthorize(2)]
        public async Task<IActionResult> SavedSearchAsync(int userId, string search)
        {
            SavedSearch ss = await _database.SavedSearch.Include(ss => ss.User).FirstOrDefaultAsync(ss => ss.Search == search && ss.UserId == userId);

            if (ss == null)
                return RedirectToAction("DefaultError", "home", new { c = 404 });

            ViewData["search"] = ss.Search;
            ViewData["response"] = ss.Results.Result;
            ViewData["searchInfos"] = ss.Results.InfoLink;
            ViewData["ba"] = ss.Results.Bande_Annone;
            ViewData["DateSauv"] = ss.DateSauvegarde;
            ViewData["username"] = ss.User.UserName;

            return View("../Home/Search");
        }
    }
}
