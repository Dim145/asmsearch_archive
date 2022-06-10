using AnimeSearch.Controllers.api;
using AnimeSearch.Database;
using AnimeSearch.Models;
using AnimeSearch.Models.Sites;
using AnimeSearch.Services;
using Microsoft.AspNetCore.Http;
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

namespace AnimeSearch.Controllers
{
    [Route("/")]
    public class HomeController : Controller
    {
        private static readonly HttpClient client = new();

        private readonly AsmsearchContext _database;
        private readonly MailService _mailService;

        private Users userTMP;

        public HomeController(AsmsearchContext DataBase, MailService mailService)
        {
            _database = DataBase;
            _mailService = mailService;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            ViewData["heure"] = DateTime.Now;

            if (context == null || context.HttpContext == null || context.HttpContext.Request == null || context.HttpContext.Request.Cookies == null)
            {
                await base.OnActionExecutionAsync(context, next);
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
                if (userTMP == null)
                {
                    userTMP = await _database.Users.FirstOrDefaultAsync(item => item.Name == username);

                    if (userTMP == null)
                    {
                        var userAgent = HttpContext.Request.Headers["User-Agent"];
                        string uaString = Convert.ToString(userAgent[0]);

                        userTMP = new() { Name = username, Navigateur = uaString };

                        await _database.Users.AddAsync(userTMP);
                        await _database.SaveChangesAsync(); // afin d'obtenir l'id
                    }
                }

                string ip = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();

                if (ip != null)
                {
                    IP ipExist = await _database.IPs.FirstOrDefaultAsync(item => item.Adresse_IP == ip && item.Users_ID == userTMP.Id);

                    if (ipExist == null)
                    {
                        ipExist = new() { Adresse_IP = ip, Users_ID = userTMP.Id, Derniere_utilisation = DateTime.Now };

                        await Utilities.Update_IP_Localisation(ipExist);

                        await _database.IPs.AddAsync(ipExist);
                    }
                    else
                    {
                        ipExist.Derniere_utilisation = DateTime.Now;

                        if(string.IsNullOrWhiteSpace(ipExist.Localisation))
                            await Utilities.Update_IP_Localisation(ipExist);

                        _database.IPs.Update(ipExist);
                    }
                }

                if (userTMP != null)
                {
                    userTMP.Derniere_visite = DateTime.Now;

                    _database.Users.Update(userTMP);
                }

                await _database.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Utilities.AddExceptionError("Le OnExecuting de Home", e);
            }

            await base.OnActionExecutionAsync(context, next);
        }

        [Route("/")]
        public IActionResult Index(bool old)
        {
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

            ViewData["Paypal_mail"] = Utilities.PAYPAL_MAIL;
            ViewData["google_ads_client_id"] = Utilities.GOOGLE_ADS_ID;
            ViewData["languages"]  = languageOrder.ToArray();
            ViewData["old"]        = old;
            ViewData["nb_db_site"] = _database.Sites.Where(s => s.Etat == EtatSite.VALIDER).CountAsync().GetAwaiter().GetResult();

            return View("Index");
        }

        [HttpGet("/")]
        public async Task<IActionResult> SearchAsync(string q, bool t = false, bool old = false)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Index(old);

            if (t)
                return RedirectToAction("MultiSearch", "Home", new { q});

            ModelAPI model = APIController.Search(q, HttpContext.Request.Cookies, await _database.Sites.Where(s => s.Etat == EtatSite.VALIDER).ToListAsync());

            if (userTMP != null)
            {
                Recherche r = await _database.Recherches.FirstOrDefaultAsync(item => item.User_ID == userTMP.Id && item.recherche == model.Result.GetName());

                if (r == null)
                {
                    r = new()
                    {
                        User_ID = userTMP.Id,
                        recherche = model.Result.GetName(),
                        Nb_recherches = 1,
                        Derniere_Recherche = DateTime.Now
                    };

                    await _database.Recherches.AddAsync(r);
                }
                else
                {
                    r.Nb_recherches++;
                    r.Derniere_Recherche = DateTime.Now;

                    _database.Recherches.Update(r);
                }

                await _database.SaveChangesAsync();
            }

            ViewData["search"] = q;
            ViewData["response"] = model.Result;
            ViewData["nbResults"] = model.SearchResults;
            ViewData["searchInfos"] = model.InfoLink;
            ViewData["ba"] = model.Bande_Annone;

            return View("Search");
        }

        [HttpGet("/{type}/{id}")]
        public async Task<IActionResult> SearchAsync(string type, int id)
        {
            if ( id < 0)
                return Index(false);

            ModelAPI model = APIController.Search(id, HttpContext.Request.Cookies, type == "tv" ? 0 : type == "movie" ? 1 : type == "tvmovie" ? 2 : -1, await _database.Sites.Where(s => s.Etat == EtatSite.VALIDER).ToListAsync());

            if (userTMP != null && model.Result != null)
            {
                Recherche r = await _database.Recherches.FirstOrDefaultAsync(item => item.User_ID == userTMP.Id && item.recherche == model.Result.GetName());

                if (r == null)
                {
                    r = new()
                    {
                        User_ID = userTMP.Id,
                        recherche = model.Result.GetName(),
                        Nb_recherches = 1,
                        Derniere_Recherche = DateTime.Now
                    };

                    await _database.Recherches.AddAsync(r);
                }
                else
                {
                    r.Nb_recherches++;
                    r.Derniere_Recherche = DateTime.Now;

                    _database.Recherches.Update(r);
                }

                await _database.SaveChangesAsync();
            }

            if(model.Result == null)
            {
                model.Result = new();
                model.Result.SetName("introuvable");
            }

            ViewData["search"] = id;
            ViewData["response"] = model.Result;
            ViewData["nbResults"] = model.SearchResults;
            ViewData["searchInfos"] = model.InfoLink;
            ViewData["ba"] = model.Bande_Annone;

            return View("Search");
        }

        [HttpGet("/MultiSearch")]
        public IActionResult MultiSearch(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Index(false);

            ViewData["results"] = APIController.MultiSearch(q);

            return View();
        }

        [HttpGet("/serieSearch")]
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

            TVMazeResult serie = null;

            Task TVMazeSearch = null;

            if (response.IsSuccessStatusCode)
            {
                string json = (await response.Content.ReadAsStringAsync()).Replace("Chinese", "zh-Hans");

                serie = JsonConvert.DeserializeObject<TVMazeResult>(json);

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

                nautiljon.SearchResult = await nautiljon.SearchAsync(serie.Name);

                if (nautiljon.GetNbResult() == 0)
                {
                    if (TVMazeSearch != null)
                        TVMazeSearch.Wait();

                    foreach (string otherName in serie.GetAllOtherNamesList(languageOrder))
                    {
                        if (!isPresent && otherName == serie.Name) continue;

                        nautiljon.SearchResult = await nautiljon.SearchAsync(otherName);

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
                    wiki.SearchResult = await wiki.SearchAsync(serie.Name);
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

                            s.SearchResult = await s.SearchAsync(otherName);

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

        [Route("Privacy")]
        public IActionResult Privacy()
        {
            return View();
        }

        [Route("Historique")]
        public async Task<IActionResult> Historique()
        {
            Recherche[] all = await _database.Recherches.OrderByDescending(r => r.Nb_recherches).ToArrayAsync();

            Dictionary<string, int> recherches = new();

            foreach (Recherche recherche in all)
            {
                int nb = 0;

                if (recherches.ContainsKey(recherche.recherche))
                {
                    nb = recherches.GetValueOrDefault(recherche.recherche);
                    recherches.Remove(recherche.recherche);
                }

                nb += recherche.Nb_recherches;

                recherches.Add(recherche.recherche, nb);
            }

            ViewData["allSearch"] = recherches.OrderBy(keyValue => keyValue.Key).OrderByDescending(keyValue => keyValue.Value).ToDictionary(r => r.Key, r => r.Value); ;

            List<Search> listSites = Utilities.AllSearchSite.Select(t => (Search)t.GetConstructor(Array.Empty<Type>()).Invoke(null)).ToList();

            listSites.AddRange((await _database.Sites.Where(s => s.Etat == EtatSite.VALIDER).ToArrayAsync()).Select(s => new SiteDynamiqueGet(s))); // peut importe get/post, on veut juste les infos de base

            ViewData["sites"] = listSites.ToArray();

            return View();
        }

        [Route("/Pokemon")]
        public IActionResult Pokemon()
        {
            return View();
        }

        [Route("/AddSites")]
        public async Task<IActionResult> AddSitesAsync()
        {
            ViewData["types"] = await _database.TypeSites.ToArrayAsync();

            return View();
        }

        [HttpGet("/domaines")]
        public IActionResult Domaines()
        {
            return View();
        }

        [Route("/Contact")]
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

            bool haveUsername = Request.Cookies.TryGetValue("username", out string username);

            if (!haveUsername && mail != null && !string.IsNullOrWhiteSpace(mail.Pseudo))
                Response.Cookies.Append("username", mail.Pseudo);

            return View();
        }

        [HttpGet("/Dons")]
        public async Task<IActionResult> Dons()
        {
            Don[] allDons = await _database.Dons.Where(d => d.Done).ToArrayAsync();

            ViewData["totalYear"] = allDons.Where(d => d.Date.Year == DateTime.Now.Year).Sum(d => d.Amout);
            ViewData["totalMonth"] = allDons.Where(d => d.Date.Month == DateTime.Now.Month && d.Date.Year == DateTime.Now.Year).Sum(d => d.Amout);
            ViewData["last_dons"] = allDons.OrderBy(d => d.Date).Take(4).ToArray();
            ViewData["Paypal_mail"] = Utilities.PAYPAL_MAIL;

            return View();
        }
    }
}
