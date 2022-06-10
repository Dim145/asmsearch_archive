using AnimeSearch.Controllers.api;
using AnimeSearch.Database;
using AnimeSearch.Models;
using AnimeSearch.Models.Sites;
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
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using AnimeSearch.Core;
using AnimeSearch.Models.Results;
using AnimeSearch.Models.Search;
using static AnimeSearch.Models.AdvanceSearch;

namespace AnimeSearch.Controllers
{
    [Route("/")]
    public class HomeController : BaseController
    {
        private static readonly HttpClient client = new();

        private readonly MailService _mailService;
        private readonly RoleManager<Roles> _roleManager;
        private readonly UserManager<Users> _userManager;

        private Users userTMP;

        public HomeController(AsmsearchContext DataBase, MailService mailService, RoleManager<Roles> roleManager, UserManager<Users> userManager): base(DataBase)
        {
            _mailService = mailService;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await base.OnActionExecutionAsync(context, next);

            ViewData["heure"] = DateTime.Now;

            if (context?.HttpContext?.Request?.Cookies == null)
            {
                return;
            }

            if (Utilities.BASE_URL == null)
            {
                Utilities.BASE_URL = Request.Scheme + "://" + Request.Host.ToString();

                if (!Utilities.BASE_URL.EndsWith("/"))
                    Utilities.BASE_URL += "/";
            }

            ClaimsIdentity user = context.HttpContext.User.Identities.FirstOrDefault();

            string username = user.Name;

            if (string.IsNullOrWhiteSpace(username))
                username = User?.Identity?.Name;

            if (string.IsNullOrWhiteSpace(username))
                context.HttpContext.Request.Cookies.TryGetValue("userName", out username);

            if (string.IsNullOrWhiteSpace(username))
                username = Utilities.GUEST;

            if (!string.IsNullOrWhiteSpace(username))
                ViewData["username"] = username;
            
            try
            {
                userTMP = currentUser;

                if (userTMP == null)
                {
                    userTMP = await _database.Users.FirstOrDefaultAsync(item => item.UserName == username);

                    if (userTMP == null)
                    {
                        var userAgent = HttpContext.Request.Headers["User-Agent"];
                        string uaString = Convert.ToString(userAgent[0]);

                        userTMP = new() { UserName = username, Navigateur = uaString };

                        await _database.Users.AddAsync(userTMP);
                        await _database.SaveChangesAsync(); // afin d'obtenir l'id
                    }
                }

                if (userTMP != null && userTMP != currentUser)
                {
                    userTMP.Derniere_visite = DateTime.Now;

                    _database.Users.Update(userTMP);

                    var role = (await _userManager.GetRolesAsync(userTMP)).Select(r => _roleManager.FindByNameAsync(r).GetAwaiter().GetResult()).ToList();

                    if (role != null && role.Count > 0)
                    {
                        var r = role.MaxBy(r => r.NiveauAutorisation);
                        ViewData["na"] = r.NiveauAutorisation;
                        ViewData["role"] = r;
                    }

                    await _database.SaveChangesAsync();
                }

                if(userTMP != null) ViewData["user"] = userTMP;

                string ip = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();

                if (ip != null && userTMP != null)
                {
                    IP ipExist = await _database.IPs.FirstOrDefaultAsync(item => item.Adresse_IP == ip && item.Users_ID == userTMP.Id);

                    if (ipExist == null)
                    {
                        ipExist = new() { Adresse_IP = ip, Users_ID = userTMP.Id, Derniere_utilisation = DateTime.Now };

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

                    await _database.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                Utilities.AddExceptionError("Le OnExecuting de Home", e);
            }
        }

        [HttpGet]
        public IActionResult Index(bool old, string q = null, bool t = false)
        {
            if (!string.IsNullOrWhiteSpace(q))
                return SearchAsync(q, t, old).GetAwaiter().GetResult();

            IRequestCookieCollection r = HttpContext.Request.Cookies;

            bool isPresent = r.TryGetValue("languageOrder", out string cookies);

            List<CultureInfo> languageOrder = new();

            if (isPresent)
            {
                string[] languages = cookies.Split("|");

                foreach (string s in languages)
                    if (s != "")
                        languageOrder.Add(new(s));
            }
            else
            {
                languageOrder.Add(new("ja"));
                languageOrder.Add(new("en"));
                languageOrder.Add(new("us"));
                languageOrder.Add(new("fr"));
            }

            ViewData["hasOpenNodeAPIKEY"] = !string.IsNullOrWhiteSpace(_database.Settings.FirstOrDefaultAsync(s => s.Name == Utilities.SETTING_OPENNODE_APIKEY).GetAwaiter().GetResult()?.GetValueObject());
            ViewData["Paypal_mail"] = _database.Settings.FirstOrDefaultAsync(s => s.Name == Utilities.SETTING_PAYPAL_MAIL_NAME).GetAwaiter().GetResult()?.GetValueObject();
            ViewData["google_ads_client_id"] = _database.Settings.FirstOrDefaultAsync(s => s.Name == Utilities.SETTING_ADS_ID_NAME).GetAwaiter().GetResult()?.GetValueObject();
            ViewData["languages"]  = languageOrder.ToArray();
            ViewData["old"]        = old;
            ViewData["nb_db_site"] = _database.Sites.Where(s => s.Etat == EtatSite.VALIDER).CountAsync().GetAwaiter().GetResult();

            return View("Index");
        }

        [HttpPost]
        [HttpGet("search/{q}")]
        public async Task<IActionResult> SearchAsync(string q, bool t = false, bool old = false)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Index(old);

            if (t)
                return RedirectToAction(nameof(AdvanceSearch), new { q });

            Result res = SiteSearch.SearchResult(q);

            if(res == null || string.IsNullOrWhiteSpace(res.GetName()) || res.GetName() == "introuvable")
                return NotFound();

            var taskBa = ApiSearch.GetBandAnnonce(res);
            var taskInfos = SiteSearch.GetInfosAndNba(res, Request.Cookies);

            Task.WaitAll(taskBa, taskInfos);

            if (currentUser != null)
            {
                Recherche r = await _database.Recherches.FirstOrDefaultAsync(item => item.User_ID == currentUser.Id && item.recherche == res.GetName());

                if (r == null)
                {
                    r = new()
                    {
                        User_ID = currentUser.Id,
                        recherche = res.GetName(),
                        Nb_recherches = 1,
                        Derniere_Recherche = DateTime.Now,
                        Source = SearchSource.WEB
                    };

                    await _database.Recherches.AddAsync(r);
                }
                else
                {
                    r.Nb_recherches++;
                    r.Derniere_Recherche = DateTime.Now;
                    r.Source = SearchSource.WEB;

                    _database.Recherches.Update(r);
                }

                await _database.SaveChangesAsync();
            }

            ViewData["search"] = q;
            ViewData["response"] = res;
            ViewData["searchInfos"] = taskInfos.Result[0];
            ViewData["ba"] = taskInfos.Result[1] ?? taskBa.Result;

            return View("Search");
        }

        [HttpGet("{type}/{id}")]
        public async Task<IActionResult> SearchAsync(string type, int id)
        {
            if (!Utilities.Tab("tv", "movie", "tvmovie").Contains(type))
                return NotFound();

            if (id <= 0)
                return RedirectToAction("index", "home");

            Result res = SiteSearch.SearchResult(id, type == "tv" ? 0 : type == "movie" ? 1 : type == "tvmovie" ? 2 : -1);

            if (res == null || string.IsNullOrWhiteSpace(res.GetName()) || res.GetName() == "introuvable")
                return RedirectToAction("DefaultError", "home", new { c = id == 0 ? -1 : 404 });

            var taskBa = ApiSearch.GetBandAnnonce(res);
            var taskInfos = SiteSearch.GetInfosAndNba(res, Request.Cookies);

            Task.WaitAll(taskBa, taskInfos);

            if (currentUser != null && res != null)
            {
                Recherche r = await _database.Recherches.FirstOrDefaultAsync(item => item.User_ID == currentUser.Id && item.recherche == res.GetName());

                if (r == null)
                {
                    r = new()
                    {
                        User_ID = currentUser.Id,
                        recherche = res.GetName(),
                        Nb_recherches = 1,
                        Derniere_Recherche = DateTime.Now,
                        Source = SearchSource.WEB
                    };

                    await _database.Recherches.AddAsync(r);
                }
                else
                {
                    r.Nb_recherches++;
                    r.Derniere_Recherche = DateTime.Now;
                    r.Source = SearchSource.WEB;

                    _database.Recherches.Update(r);
                }

                await _database.SaveChangesAsync();
            }

            if(res == null)
            {
                res = new();
                res.SetName("introuvable");
            }

            ViewData["search"] = id;
            ViewData["response"] = res;
            ViewData["searchInfos"] = taskInfos.Result[0];
            ViewData["type"] = type;
            ViewData["ba"] = taskInfos.Result[1] ?? taskBa.Result;

            return View("Search");
        }

        [HttpGet("MultiSearch")]
        [HttpPost("MultiSearch")]
        public IActionResult MultiSearch(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Index(false);

            ViewData["results"] = SiteSearch.MultiSearch(q);

            return View("MultiSearch");
        }

        [Obsolete("Méthode crée dans les premiers jours du projet. N'utilise pas la plupart des fonctionnalités développées ensuite.")]
        [HttpGet("serieSearch")]
        public async Task<IActionResult> SearchSerie(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Index(true);

            q = q.Trim();

            IRequestCookieCollection r = HttpContext.Request.Cookies;

            ViewData["search"] = q;
            Task<HttpResponseMessage> task = client.GetAsync(Utilities.URL_ANIME_SINGLE_SEARCH + q);

            bool isPresent = r.TryGetValue("languageOrder", out string cookies);

            List<CultureInfo> languageOrder = new();

            if (isPresent)
            {
                string[] languages = cookies.Split("|");

                foreach (string s in languages)
                    if (s != "")
                        languageOrder.Add(new(s));
            }

            task.Wait();

            HttpResponseMessage response = task.Result;

            TvMazeResult serie = null;

            Task TVMazeSearch = null;

            if (response.IsSuccessStatusCode)
            {
                string json = (await response.Content.ReadAsStringAsync()).Replace("Chinese", "zh-Hans");

                serie = JsonConvert.DeserializeObject<TvMazeResult>(json);

                if (serie != null)
                {
                    TVMazeSearch = Task.Run(async () =>
                    {
                        response = await client.GetAsync("https://api.tvmaze.com/shows/" + serie.Id + "/akas");

                        Newtonsoft.Json.Linq.JArray languages = (Newtonsoft.Json.Linq.JArray)JsonConvert.DeserializeAnonymousType<object>(await response.Content.ReadAsStringAsync(), new
                        {
                            Name = "",
                            Country = new Dictionary<string, string>()
                        });

                        foreach (Newtonsoft.Json.Linq.JToken t in languages)
                        {
                            string name = t.Value<string>("name");

                            Newtonsoft.Json.Linq.JObject counry = t.Value<Newtonsoft.Json.Linq.JObject>("country");

                            CultureInfo culture;
                            try
                            {
                                string code = counry?.Value<string>("code");

                                if (code != null && code.ToLower().Equals("jp"))
                                    code = "ja";

                                culture = code != null ? new(code, false) : CultureInfo.InvariantCulture;
                            }
                            catch (Exception)
                            {
                                culture = CultureInfo.InvariantCulture;
                            }

                            List<string> list = serie.OthersName.GetValueOrDefault(culture);

                            if (list == null)
                            {
                                list = new();
                                serie.OthersName.Add(culture, list);
                            }

                            list.Add(name);
                        }

                        serie.AddNameInLanguage();
                    });
                }
            }
            else if (response.StatusCode.Equals(System.Net.HttpStatusCode.NotFound))
            {
                serie = new()
                {
                    Name = q
                };

                ViewData["result"] = "Serie introuvable => recherche selon ce qui est tapé";
            }

            Task infos = Task.Run(async () =>
            {
                NautiljonSearch nautiljon = new(NautiljonSearch.FILTER_ANIME);

                await nautiljon.SearchAsync(serie.Name);

                if (nautiljon.GetNbResult() == 0)
                {
                    if (TVMazeSearch != null)
                        TVMazeSearch.Wait();

                    foreach (string otherName in serie.GetAllOtherNamesList(languageOrder))
                    {
                        if (!isPresent && otherName == serie.Name) continue;

                        await nautiljon.SearchAsync(otherName);

                        if (nautiljon.GetNbResult() > 0)
                            break;
                    }
                }

                if (nautiljon.GetNbResult() != 0 && serie.Image == null)
                {
                    serie.Image = new();

                    bool succes = Uri.TryCreate(await nautiljon.GetImageResultAsync(), UriKind.RelativeOrAbsolute, out Uri u);

                    if (succes)
                        serie.Image.Add("original", u);
                }

                if (nautiljon.GetNbResult() == 0)
                {
                    WikiPediaSearch wiki = new();
                    await wiki.SearchAsync(serie.Name);
                    wiki.GetNbResult();

                    string javascript = wiki.GetJavaScriptClickEvent();
                    int start = javascript.IndexOf("\"") + 1;

                    ViewData["searchInfos"] = javascript[start..javascript.LastIndexOf("\"")];
                }
                else
                {
                    string javascript = nautiljon.GetJavaScriptClickEvent();
                    int start = javascript.IndexOf("\"") + 1;

                    ViewData["searchInfos"] = javascript[start..javascript.LastIndexOf("\"")];
                }
            });

            List<Search> listSiteToSearch = new();

            foreach (Type t in Utilities.AllSearchSite)
            {
                string type = (string)t.GetField("TYPE").GetValue(null);

                if (serie.Type == "Animation" && type.Contains("animes") || serie.Type == "Scripted" && type.Contains("séries") || type.Contains("all"))
                    listSiteToSearch.Add((Search)t.GetConstructor(Array.Empty<Type>()).Invoke(null));
            }

            if (!isPresent)
                Task.WaitAll(listSiteToSearch.Select(item => item.SearchAsync(serie.Name)).ToArray());

            if (TVMazeSearch != null)
                TVMazeSearch.Wait();

            Dictionary<string, ModelSearchResult> dicNbResultSite = new();

            List<Task> tabTask = new();
            foreach (Search s in listSiteToSearch)
            {
                if (s.GetNbResult() <= 0)
                    tabTask.Add(Task.Run(async () =>
                    {
                        foreach (string otherName in serie.GetAllOtherNamesList(languageOrder))
                        {
                            if (!isPresent && otherName == serie.Name) continue;

                            await s.SearchAsync(otherName);

                            if (s.GetNbResult() > 0)
                                break;
                        }
                    }));
            }

            Task.WaitAll(tabTask.ToArray());

            foreach (Search s in listSiteToSearch)
            {
                if (s.GetNbResult() > -1)
                {
                    ModelSearchResult model = new()
                    {
                        NbResults = s.GetNbResult(),
                        SiteUrl = s.GetBaseURL(),
                        IconUrl = s.GetUrlImageIcon(),
                        OpenJavaScript = s.GetJavaScriptClickEvent(),
                        Type = s.GetTypeSite()
                    };

                    dicNbResultSite.Add(s.GetSiteTitle(), model);
                }
            }

            ViewData["nbResults"] = dicNbResultSite;

            infos.Wait();

            if (serie.GetImage() == null)
                serie.SetImage(serie.Image.GetValueOrDefault("original"));

            ViewData["response"] = serie;

            return View("Search");
        }

        [HttpGet("Privacy")]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet("Historique")]
        public async Task<IActionResult> Historique()
        {
            var listSites = await _database.Sites.Where(s => s.Etat == EtatSite.VALIDER).Select(s =>(Search) new SiteDynamiqueGet(s)).ToListAsync();

            listSites.AddRange(Utilities.AllSearchSite.Select(t => (Search)t.GetConstructor(Array.Empty<Type>()).Invoke(null)).Where(s => !listSites.Contains(s))); // peut importe get/post, on veut juste les infos de base

            ViewData["sites"] = listSites.ToArray();

            return View();
        }

        [Route("Pokemon")]
        public IActionResult Pokemon()
        {
            return View();
        }

        [HttpGet("AddSites")]
        public async Task<IActionResult> AddSitesAsync()
        {
            ViewData["types"] = await _database.TypeSites.ToArrayAsync();

            return View();
        }

        [HttpGet("domaines")]
        public IActionResult Domaines()
        {
            return View();
        }

        [HttpGet("Contact")]
        [HttpPost("Contact")]
        public async Task<IActionResult> ContactAsync([FromForm] MailRequest mail)
        {
            if(mail != null && !string.IsNullOrWhiteSpace(mail.Email) && !string.IsNullOrWhiteSpace(mail.Subject) && !string.IsNullOrWhiteSpace(mail.Message))
            {
                try
                {
                    await _mailService.SendEmailAsync(mail);

                    ViewData["succes"] = "Mail envoyer !";
                }
                catch(Exception e)
                {
                    Utilities.AddExceptionError($"l'envoie de mail à {mail.Email}", e);

                    ViewData["errors"] = "le mail n'as pas pus être envoyé.";
                    ViewData["datas"] = mail;
                }
            }
            else if(!string.IsNullOrWhiteSpace(mail.Subject))
            {
                ViewData["errors"] = "Un des champs saisie n'est pas correct.";
                ViewData["datas"] = mail;
            }

            bool haveUsername = Request.Cookies.TryGetValue("username", out _);

            if (!haveUsername && mail != null && !string.IsNullOrWhiteSpace(mail.Pseudo))
                Response.Cookies.Append("username", mail.Pseudo);

            return View();
        }

        [HttpGet("Dons")]
        public async Task<IActionResult> Dons()
        {
            Don[] allDons = await _database.Dons.Where(d => d.Done).Include(d => d.User).ToArrayAsync();

            ViewData["totalYear"] = allDons.Where(d => d.Date.Year == DateTime.Now.Year).Sum(d => d.Amout);
            ViewData["totalMonth"] = allDons.Where(d => d.Date.Month == DateTime.Now.Month && d.Date.Year == DateTime.Now.Year).Sum(d => d.Amout);
            ViewData["last_dons"] = allDons.OrderBy(d => d.Date).Take(4).ToArray();
            ViewData["Paypal_mail"] = (await _database.Settings.FirstOrDefaultAsync(s => s.Name == Utilities.SETTING_PAYPAL_MAIL_NAME))?.GetValueObject();
            ViewData["obj dons"] = (await _database.Settings.FirstOrDefaultAsync(s => s.Name == Utilities.SETTING_DON_NAME)).GetValueObject();
            var test = await _database.Settings.FirstOrDefaultAsync(s => s.Name == Utilities.SETTING_OPENNODE_APIKEY);
            ViewData["hasOpenNodeAPIKEY"] = !string.IsNullOrWhiteSpace((await _database.Settings.FirstOrDefaultAsync(s => s.Name == Utilities.SETTING_OPENNODE_APIKEY))?.GetValueObject());

            return View();
        }

        [HttpPost("Error")]
        [HttpGet("Error")]
        public IActionResult DefaultError(int c = -1)
        {
            ViewData["code"] = c;

            if(c > 0) HttpContext.Response.StatusCode = c;

            return View("Erreur");
        }

        [HttpGet("BotDiscord")]
        public IActionResult BotDiscord()
        {
            return View();
        }

        [HttpGet("BotTelegram")]
        public IActionResult BotTelegram()
        {
            return View();
        }

        [Authorize]
        [HttpGet("savedSearch/{q}")]
        public async Task<IActionResult> UseSavedSearch(string q)
        {
            var user = await _database.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            SavedSearch ss = await _database.SavedSearch.FirstOrDefaultAsync(ss => ss.Search == q && ss.UserId == user.Id);

            if (ss == null)
                return NotFound();

            ViewData["search"] = ss.Search;
            ViewData["response"] = ss.Results.Result;
            ViewData["searchInfos"] = ss.Results.InfoLink;
            ViewData["ba"] = ss.Results.Bande_Annone;
            ViewData["DateSauv"] = ss.DateSauvegarde;

            return View("Search");
        }

        [HttpGet("advanceSearch")]
        [HttpPost("advanceSearch")]
        public async Task<IActionResult> AdvanceSearch([FromForm] AdvanceSearch advSearch, string q = "", int page = 1, DateTime? Before = null, DateTime? After = null, int sortBy = -1, SearchType? type = null)
        {
            if(advSearch != null)
            {
                if(string.IsNullOrWhiteSpace(advSearch.Q))
                    advSearch.Q = q;

                if(advSearch.Page <= 1)
                    advSearch.Page = Math.Max(page, 1);

                if (advSearch.Before == null)
                    advSearch.Before = Before;

                if(advSearch.After == null)
                    advSearch.After = After;

                if(advSearch.SortBy == -1)
                    advSearch.SortBy = sortBy;

                if (advSearch.SearchIn == SearchType.ALL && type != null)
                    advSearch.SearchIn = type.Value;
            }

            if(advSearch != null && !advSearch.IsEmpty())
                ViewData["results"] = await ApiSearch.RechercheByGenresAsync(advSearch);

            ViewData["advs"] = advSearch;

            return View(nameof(AdvanceSearch));
        }
    }
}
