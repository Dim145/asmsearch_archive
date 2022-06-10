using AnimeSearch.Database;
using AnimeSearch.Models;
using AnimeSearch.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnimeSearch.Controllers
{
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly AsmsearchContext _database;
        private readonly string mdp;

        public AdminController(AsmsearchContext database, IConfiguration configRoot)
        {
            _database = database;
            mdp = configRoot["admin_mdp"];
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            ViewData["heure"] = DateTime.Now;

            if (Utilities.BASE_URL == null)
            {
                Utilities.BASE_URL = Request.Scheme + "://" + Request.Host.ToString();

                if (!Utilities.BASE_URL.EndsWith("/"))
                    Utilities.BASE_URL += "/";
            }

            string token = HttpContext.Session.GetString("adtoken");

            if ("Login" != context.ActionDescriptor.RouteValues["action"] && string.IsNullOrWhiteSpace(token))
                context.Result = RedirectToAction("Login", "admin");

            if (!string.IsNullOrWhiteSpace(token) && context.ActionDescriptor.RouteValues["action"] == "Login")
                context.Result = RedirectToAction("Index", "admin");

            if (Request.Cookies.TryGetValue("userName", out string username))
                ViewData["username"] = username;

            if (!string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(username))
            {
                try
                {
                    Users user = await _database.Users.FirstOrDefaultAsync(item => item.Name == username);

                    if (user == null)
                    {
                        var userAgent = HttpContext.Request.Headers["User-Agent"];
                        string uaString = Convert.ToString(userAgent[0]);

                        user = new() { Name = username, Navigateur = uaString };

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

                            await Utilities.Update_IP_Localisation(ipExist);

                            await _database.IPs.AddAsync(ipExist);
                        }
                        else
                        {
                            ipExist.Derniere_utilisation = DateTime.Now;

                            if (string.IsNullOrWhiteSpace(ipExist.Localisation))
                                await Utilities.Update_IP_Localisation(ipExist);

                            _database.IPs.Update(ipExist);
                        }
                    }

                    if (user != null)
                    {
                        user.Derniere_visite = DateTime.Now;
                        user.Dernier_Acces_Admin = DateTime.Now;

                        _database.Users.Update(user);
                    }

                    await _database.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    Utilities.AddExceptionError("Le OnActionExcecuting de l'Admin", e);
                }
            }

            await base.OnActionExecutionAsync(context, next);
        }

        [HttpGet()]
        public async Task<IActionResult> IndexAsync()
        {
            ViewData["users"] = await _database.Users.OrderByDescending(u => u.Derniere_visite).ToArrayAsync();

            return View();
        }

        [HttpGet("sites")]
        public async Task<IActionResult> SitesAsync()
        {
            ViewData["sites"] = await _database.Sites.ToArrayAsync();
            ViewData["time_before_cs_service"] = Utilities.GetTimeBeforeNextCheckSiteService();

            ViewData["aideHTML"] = "<p>Liste des sites avec quelques informations basiques. De la même façon que pour les citations, il faut faire un clic droit pour modifier un site ou pour mettre l'état du site en erreur du type 404 (site introuvable)</p>" +
                "<p>Pour plus d'informations sur ce système d'erreur, se référer à la page de modification.</p>";

            return View();
        }

        [HttpPost("CSVS")]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> ChangeSiteValidateState([FromForm] string url, [FromForm] EtatSite etat)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                Sites s = await _database.Sites.FirstOrDefaultAsync(c => c.Url == url);

                if (s != null && s.Etat != etat)
                {
                    s.Etat = etat;
                    _database.Sites.Update(s);
                    await _database.SaveChangesAsync();
                }
            }

            return RedirectToAction("sites", "admin", null, url);
        }

        [HttpPost("sites/edit")]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> EditerSite([FromForm] ModelSiteBdd s)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.Url))
                return RedirectToAction("sites", "admin");

            ViewData["types"] = await _database.TypeSites.ToArrayAsync();

            ViewData["aideHTML"] = "<p>Page permettant de modifier les données d'un site. La page contient moins de restrictions que lors de l'ajout d'un site. (Notamment sur les données \"POST\").</p>" +
                "<p>Le changement d'état permet d'indiquer aux autres admins ou au super-admin l'état du site et empêche le serveur d'utiliser ce site lors de des recherches.</p>" +
                "<p>Voici la signification des erreurs de bases:" +
                "<ul>" +
                "<li>L'erreur 404 signifie que le site est introuvable (adresse URL changée) => Il faut changer l'adresse URL, seul le super-admin peut faire cela. (le contacter)</li>" +
                "<li>L'erreur CloudFlare signifie que le site est fonctionnel mais que CloudFlare a bloqué l'adresse IP du serveur (temporairement ou pas). => Rien à faire à part attendre si le blocage est temporaire.</li>" +
                "<li>L'erreur 0 résultat signifie que le serveur ne trouve plus aucun résultat sur le site. => La structure du site a dû changer, il faut mettre à jour le champ \"chemin vers balise à</li>" +
                "</ul></p>" +
                "<p>Le bouton testé permet de demander au serveur d'exécuter une recherche qu'avec les données saisies du site (celle dans les champs). Pour le moment seul la recherche One Piece est possible.</p>";

            Sites site = new()
            {
                Url = s.Url,
                UrlSearch = s.UrlSearch,
                Title = s.Title,
                CheminBaliseA = s.CheminBaliseA,
                IdBase = s.IdBase,
                Etat = s.Etat,
                Is_inter = s.Is_inter,
                TypeSite = s.TypeSite,
                UrlIcon = s.UrlIcon
            };

            if (!string.IsNullOrWhiteSpace(s.SpostValues))
            {
                site.PostValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(s.SpostValues);
                s.PostValues = site.PostValues;
            }

            if (site.UrlSearch == null)
                site.UrlSearch = string.Empty;

            if (string.IsNullOrWhiteSpace(site.Title))
            {
                bool ok = !string.IsNullOrWhiteSpace(site.Url);

                if (ok)
                {
                    ViewData["site"] = await _database.Sites.FirstOrDefaultAsync(s => s.Url == site.Url);

                    ok = ViewData["site"] != null;
                }

                return ok ? View("SitesEditer") : RedirectToAction("sites", "admin");
            }
            else
            {
                if (site.PostValues != null && site.PostValues.Count == 0)
                    site.PostValues = null;

                if (!await _database.Sites.AnyAsync(s => s.Url == site.Url) || (!TypeEnum.TabTypes.Contains(site.TypeSite) && !await _database.TypeSites.AnyAsync(t => t.Name == site.TypeSite)) || string.IsNullOrWhiteSpace(site.CheminBaliseA))
                {
                    ViewData["erreurs"] = "Une des valeurs saisies n'est pas corrects.";

                    return await EditerSite(new() { Url = site.Url });
                }

                _database.Sites.Update(site);
                await _database.SaveChangesAsync();

                ViewData["site"] = site;

                return View("SitesEditer");
            }
        }

        [HttpGet("citations")]
        public async Task<IActionResult> CitationsAsync()
        {
            ViewData["citations"] = await _database.Citations.OrderByDescending(c => c.DateAjout).ToArrayAsync();

            ViewData["aideHTML"] = "<p>Liste des citations triées par date d'ajout. Pour valider ou invalider une citation, il faut faire un clic droit dessus.</p>";

            return View();
        }

        [HttpPost("CCVS")]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> ChangeCitationValidateState([FromForm] int id, [FromForm] bool isValidated)
        {
            if (id > 0)
            {
                Citations c = await _database.Citations.FirstOrDefaultAsync(c => c.Id == id);

                if (c != null && c.IsValidated != isValidated)
                {
                    c.IsValidated = isValidated;
                    _database.Citations.Update(c);
                    await _database.SaveChangesAsync();
                }
            }

            return RedirectToAction("citations", "admin", null, id > 0 ? id.ToString() : string.Empty);
        }

        [HttpGet("types")]
        public async Task<IActionResult> TypeSites()
        {
            ViewData["types"] = await _database.TypeSites.OrderByDescending(t => t.Id).ToArrayAsync();

            ViewData["aideHTML"] = "<p>Cette page permet d'éditer les propositions de types de sites.</p>" +
                "<p>Tous les types ne sont pas directement relier aux sites. Donc supprimer un type ne cause aucuns problèmes et " +
                "ne change pas le type des sites qui l'utilisent. Cela ne change que les propositions lors de l'ajout ou de la modification de sites.</p>";

            return View();
        }

        [HttpGet("suppType")]
        public async Task<IActionResult> SuppType(int id)
        {
            if (id > 0)
            {
                try
                {
                    _database.TypeSites.Remove(new() { Id = id });

                    await _database.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    Utilities.AddExceptionError($"la suppression de Types({id})", e);
                }
            }

            return RedirectToAction("types", "admin");
        }

        [HttpPost("addType")]
        public async Task<IActionResult> AddType([FromForm] string name)
        {
            if (!string.IsNullOrWhiteSpace(name) && !await _database.TypeSites.AnyAsync(t => t.Name == name))
            {
                try
                {
                    await _database.TypeSites.AddAsync(new() { Name = name });
                    await _database.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    Utilities.AddExceptionError($"l'ajout de Types({name})", e);
                }
            }

            return RedirectToAction("types", "admin");
        }

        [HttpGet("recherches/{userId}")]
        public async Task<IActionResult> Recherches(int userId)
        {
            if (userId <= 0)
                return BadRequest("Id Invalide");

            ViewData["selected user"] = await _database.Users
                .Where(u => u.Id == userId)
                .FirstOrDefaultAsync();

            ViewData["recherches"] = await _database.Recherches
                .Where(r => r.User_ID == userId)
                .OrderByDescending(r => r.Derniere_Recherche)
                .ThenByDescending(r => r.Nb_recherches)
                .Select(r => new string[] { r.recherche, r.Nb_recherches.ToString(), r.Derniere_Recherche.GetValueOrDefault().ToString("dd/MM/yyyy HH:mm:ss") })
                .ToArrayAsync();

            return View();
        }

        [HttpGet("ips/{userId}")]
        public async Task<IActionResult> Ips(int userId)
        {
            if (userId <= 0)
                return BadRequest("Id Invalide");

            ViewData["selected user"] = await _database.Users.FirstOrDefaultAsync(u => u.Id == userId);
            ViewData["ips"] = await _database.IPs.Where(r => r.Users_ID == userId).OrderByDescending(r => r.Derniere_utilisation).ToArrayAsync();

            ViewData["aideHTML"] = "<p>Liste des adresses IP d'un utilisateur avec la localisation de chaque adresse. Il est possible de cliquer sur les localisations pour ouvrir une page google maps.</p>";

            return View();
        }

        [HttpGet("dons/{userId}")]
        public async Task<IActionResult> Dons(int userId)
        {
            if (userId <= 0)
                return BadRequest("Id Invalide");

            ViewData["selected user"] = await _database.Users.FirstOrDefaultAsync(u => u.Id == userId);
            ViewData["dons"] = await _database.Dons.Where(d => d.User_id == userId).ToArrayAsync();
            ViewData["pp"] = Utilities.LAST_DONS_SERVICE.GetTempsProchainPassage();

            ViewData["aideHTML"] = "<p>Cette page montre tous les evntuels dons d'un utilisateur.<br /> Ceux-ci ne sont modifiable que si ils ne sont pas validée depuis plus d'une heure et que le service n'est pas encore passé.</p>";

            return View();
        }

        [HttpPost("rdons")]
        public async Task<IActionResult> DeleteDon([FromForm] Guid id)
        {
            if (Guid.Empty != id)
            {
                Don don = await _database.Dons.FirstOrDefaultAsync(d => d.Id == id);

                if (don != null && !don.Done && DateTime.Now.Subtract(don.Date) >= TimeSpan.FromHours(1))
                {
                    _database.Dons.Remove(don);
                    await _database.SaveChangesAsync();
                }

                if (don != null)
                    return RedirectToAction("dons", "admin", new { userId = don.User_id });
            }

            return RedirectToAction("index", "admin");
        }

        [HttpGet("services")]
        [HttpPost("services")]
        public async Task<IActionResult> ServicesAsync([FromForm] int id = -1, [FromForm] int val = -1)
        {
            if (id > -1 && id < Utilities.SERVICES.Count && val > -1 && val < 3)
            {
                BaseService currentService = Utilities.SERVICES[id];

                switch (val)
                {
                    case 0: await currentService.StartAsync(new()); break;
                    case 1: await currentService.StopAsync(new()); break;
                    case 2: await currentService.ExecutionCode(); break;
                }
            }

            ViewData["services"] = Utilities.SERVICES.ToArray();

            return View();
        }

        [HttpGet("Login")]
        [HttpPost("Login")]
        public IActionResult Login([FromForm] Dictionary<string, string> datas)
        {
            string username = datas?.GetValueOrDefault("username");
            string password = datas?.GetValueOrDefault("password");

            if (Request.Cookies.TryGetValue("userName", out string pseudo))
            {
                ViewData["username"] = pseudo;
            }
            else if (!string.IsNullOrWhiteSpace(username))
            {
                Response.Cookies.Append("userName", username);
                ViewData["username"] = username;
            }

            if (!string.IsNullOrWhiteSpace(password) && mdp == password)
            {
                HttpContext.Session.SetString("adtoken", Guid.NewGuid().ToString());

                return RedirectToAction("Index", "admin");
            }

            if (username != null)
            {
                ViewBag.error = "Identifiant invalides";
            }

            return View();
        }

        [HttpGet("Logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("adtoken");

            return RedirectToAction("Login");
        }

        [Route("Error")]
        public IActionResult Error()
        {
            ViewData["errors"] = Utilities.Errors.ToArray();

            return View();
        }
        public class ModelSiteBdd : Sites
        {
            public string SpostValues { get; set; }
        }
    }
}
