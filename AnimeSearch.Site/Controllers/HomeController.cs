using System.Globalization;
using System.Net;
using System.Security.Claims;
using AnimeSearch.Core;
using AnimeSearch.Core.Extensions;
using AnimeSearch.Core.Models.Api;
using AnimeSearch.Core.Models.Search;
using AnimeSearch.Core.ViewsModel;
using AnimeSearch.Data;
using AnimeSearch.Data.Models;
using AnimeSearch.Services;
using AnimeSearch.Services.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MailService = AnimeSearch.Services.Mails.MailService;

namespace AnimeSearch.Site.Controllers;

[Route("/")]
public class HomeController : BaseController
{
    private readonly MailService _mailService;
    private readonly RoleManager<Roles> _roleManager;
    private readonly UserManager<Users> _userManager;
    private readonly ApiService _apiService;
    private readonly SiteSearchService _siteSearchService;
    private readonly DuckDuckGoSearch _duckDuckGo;
    private readonly IStringLocalizer<HomeController> _localizer;

    private Users userTMP;

    public HomeController(AsmsearchContext dataBase, 
        MailService mailService, 
        RoleManager<Roles> roleManager, 
        UserManager<Users> userManager,
        ApiService apiService,
        SiteSearchService siteSearchService,
        DuckDuckGoSearch duckDuckGo,
        IStringLocalizer<HomeController> localizer): base(dataBase)
    {
        _mailService = mailService;
        _userManager = userManager;
        _roleManager = roleManager;
        _duckDuckGo = duckDuckGo;
        _localizer = localizer;
        _apiService = apiService;
        _siteSearchService = siteSearchService;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        ViewData["heure"] = DateTime.Now;
        ViewData["hotJarId"] = _database.Settings.GetValueOrDefault<double>(DataUtils.SettingHotJarId);

        if (CoreUtils.BaseUrl == null)
        {
            CoreUtils.BaseUrl = Request.Scheme + "://" + Request.Host.ToString();

            if (!CoreUtils.BaseUrl.EndsWith("/"))
                CoreUtils.BaseUrl += "/";
        }

        ClaimsIdentity user = context.HttpContext.User.Identities.FirstOrDefault();

        string username = user?.Name;

        if (string.IsNullOrWhiteSpace(username))
            username = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(username))
            context.HttpContext.Request.Cookies.TryGetValue("userName", out username);

        if (string.IsNullOrWhiteSpace(username))
            username = DataUtils.Guest;

        if (!string.IsNullOrWhiteSpace(username))
            ViewData["username"] = username;
            
        try
        {
            userTMP = currentUser;
            
            if(username != DataUtils.Guest)
                Response.Cookies.Append("userName", username);

            if (userTMP == null)
            {
                userTMP = _database.Users.FirstOrDefault(item => item.UserName == username);

                if (userTMP == null)
                {
                    var userAgent = HttpContext.Request.Headers["User-Agent"];
                    string uaString = Convert.ToString(userAgent[0]);

                    userTMP = new() { UserName = username, Navigateur = uaString };

                    _database.Users.Add(userTMP);
                    _database.SaveChanges(); // afin d'obtenir l'id
                }
            }

            if (userTMP != null && userTMP != currentUser)
            {
                userTMP.Derniere_visite = DateTime.Now;

                _database.Users.Update(userTMP);

                var role = (_userManager.GetRolesAsync(userTMP).GetAwaiter().GetResult()).Select(r => _roleManager.FindByNameAsync(r).GetAwaiter().GetResult()).ToList();

                if (role is {Count: > 0})
                {
                    var r = role.MaxBy(r => r.NiveauAutorisation);
                    ViewData["na"] = r?.NiveauAutorisation ?? 0;
                    ViewData["role"] = r;
                }

                _database.SaveChanges();
            }

            if(userTMP != null) ViewData["user"] = userTMP;

            string ip = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();

            if (ip != null && userTMP != null)
            {
                IP ipExist = _database.IPs.FirstOrDefault(item => item.Adresse_IP == ip && item.Users_ID == userTMP.Id);

                if (ipExist == null)
                {
                    ipExist = new() { Adresse_IP = ip, Users_ID = userTMP.Id, Derniere_utilisation = DateTime.Now };

                    ipExist.UpdateLocalisation().Wait();

                    _database.IPs.Add(ipExist);
                }
                else
                {
                    ipExist.Derniere_utilisation = DateTime.Now;

                    if (string.IsNullOrWhiteSpace(ipExist.Localisation))
                        ipExist.UpdateLocalisation().Wait();

                    _database.IPs.Update(ipExist);
                }

                _database.SaveChanges();
            }
        }
        catch (Exception e)
        {
            CoreUtils.AddExceptionError("Le OnExecuting de Home", e, currentUser.UserName);
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

        ViewData["hasOpenNodeAPIKEY"] = !string.IsNullOrWhiteSpace(_database.Settings.GetValueOrDefault<string>(DataUtils.SettingOpennodeApikey));
        ViewData["Paypal_mail"] = _database.Settings.GetValueOrDefault(DataUtils.SettingPaypalMailName);
        ViewData["google_ads_client_id"] = _database.Settings.GetValueOrDefault(DataUtils.SettingAdsIdName);
        ViewData["languages"]  = languageOrder.ToArray();
        ViewData["old"]        = old;
        ViewData["nb_db_site"] = _database.Sites.Where(s => s.Etat == EtatSite.VALIDER).CountAsync().GetAwaiter().GetResult();
        ViewData["citation"] = _database.Citations.FirstOrDefault(c => c.IsCurrent);

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

        Result res = await _apiService.SearchResult(q);

        if(res == null || string.IsNullOrWhiteSpace(res.Name))
            return NotFound();

        if (res.Name == "introuvable")
            return RedirectToAction("DefaultError", new {c = 4040, v = q});

        var (taskInfos, ba) = await SearchValuesAndSaveDb(res);

        ViewData["search"] = q;
        ViewData["response"] = res;
        ViewData["searchInfos"] = taskInfos;
        ViewData["ba"] = ba;
        ViewData["eps"] = await _database.EpisodesUrls
            .By(res.IdApiFrom, res.Id)
            .Where(eu => eu.Valid)
            .ToArrayAsync();

        return View("Search");
    }

    [HttpPost("forceSearch")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForceSearch(string search, ResultType type)
    {
        if (string.IsNullOrWhiteSpace(search) || type == ResultType.All)
            return RedirectToAction("Index");

        var res = new Result
        {
            Name = search,
            Type = type.ToTypeString()
        };

        var (taskInfos, ba) = await SearchValuesAndSaveDb(res);
        
        ViewData["search"] = search;
        ViewData["response"] = res;
        ViewData["searchInfos"] = taskInfos;
        ViewData["ba"] = ba;
        
        return View("Search");
    }

    private async Task<(string, string)> SearchValuesAndSaveDb(Result res)
    {
        var taskBa = _duckDuckGo.GetBandeAnnonce(res);
        var taskInfos = _siteSearchService.GetInfosAndNba(res, Request.Cookies);

        await Task.WhenAll(taskBa, taskInfos);

        userTMP ??= currentUser ?? ViewData["user"] as Users;

        if (userTMP != null)
        {
            var r = await _database.Recherches.FirstOrDefaultAsync(item => item.User_ID == userTMP.Id && item.recherche == res.Name);

            if (r == null)
            {
                r = new()
                {
                    User_ID = userTMP.Id,
                    recherche = res.Name,
                    Nb_recherches = 1,
                    Derniere_Recherche = DateTime.Now,
                    Source = SearchSource.Web
                };

                await _database.Recherches.AddAsync(r);
            }
            else
            {
                r.Nb_recherches++;
                r.Derniere_Recherche = DateTime.Now;
                r.Source = SearchSource.Web;

                _database.Recherches.Update(r);
            }

            await _database.SaveChangesAsync();
        }
        
        return (taskInfos.Result[0], taskInfos.Result[1] ?? taskBa.Result);
    }

    [HttpGet("{idApi:int}/{type}/{id:int}")]
    public async Task<IActionResult> SearchAsync(string type, int id, int idApi)
    {
        if (!CoreUtils.Tab("tv", "movie", "anime").Contains(type))
            return NotFound();

        if (id <= 0)
            return RedirectToAction("index", "home");

        var searchType = type switch
        {
            "tv" => ResultType.Series,
            "movie" => ResultType.Movies,
            "anime" => ResultType.Anime,
            _ => ResultType.All
        };

        Result res = await _apiService.SearchResult(id, searchType, idApi);

        if (res == null || string.IsNullOrWhiteSpace(res.Name) || res.Name == "introuvable")
            return RedirectToAction("DefaultError", "home", new { c = 404 });

        var taskBa = _duckDuckGo.GetBandeAnnonce(res);
        var taskInfos = _siteSearchService.GetInfosAndNba(res, Request.Cookies);

        await Task.WhenAll(taskBa, taskInfos);

        var userTmp = currentUser;

        if (currentUser == null)
            userTmp = await _database.Users.FirstOrDefaultAsync(u => u.UserName == DataUtils.Guest);

        if (userTmp != null)
        {
            Recherche r = await _database.Recherches.FirstOrDefaultAsync(item => item.User_ID == userTmp.Id && item.recherche == res.Name);

            if (r == null)
            {
                r = new()
                {
                    User_ID = userTmp.Id,
                    recherche = res.Name,
                    Nb_recherches = 1,
                    Derniere_Recherche = DateTime.Now,
                    Source = SearchSource.Web
                };

                await _database.Recherches.AddAsync(r);
            }
            else
            {
                r.Nb_recherches++;
                r.Derniere_Recherche = DateTime.Now;
                r.Source = SearchSource.Web;

                _database.Recherches.Update(r);
            }

            await _database.SaveChangesAsync();
        }

        ViewData["search"] = id;
        ViewData["response"] = res;
        ViewData["searchInfos"] = taskInfos.Result[0];
        ViewData["type"] = searchType;
        ViewData["ba"] = taskInfos.Result[1] ?? taskBa.Result;
        ViewData["idapi"] = idApi;

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
        var listSites = await _database.Sites.Where(s => s.Etat == EtatSite.VALIDER).Select(s => s.ToDynamicSite(false, null)).ToListAsync();

        listSites.AddRange(CoreUtils.AllSearchSite.Select(t => (Search)t.GetConstructor(Array.Empty<Type>()).Invoke(null)).Where(s => !listSites.Contains(s))); // peut importe get/post, on veut juste les infos de base

        ViewData["sites"] = listSites.ToArray();

        return View();
    }

    [HttpGet("Pokemon")]
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
    [HttpPost("domaines")]
    public async Task<IActionResult> Domaines([FromForm] Domains domain)
    {
        if ((ViewData["na"] as int? ?? 0) >= DataUtils.DroitAdd && !string.IsNullOrWhiteSpace(domain?.Url?.ToString()))
        {
            if (!await _database.Domains.AnyAsync(d => d.Url == domain.Url) && await ServiceUtils.IsDomainUrl(domain.Url.ToString(), new()))
            {
                domain.LastSeen = DateTime.Now;
                var tracking = await _database.Domains.AddAsync(domain);
                await _database.SaveChangesAsync();

                tracking.State = EntityState.Detached;

                ModelState.Clear();
            }
            else
            {
                ModelState.AddModelError("bad_domain", _localizer["bad_domain_error_message"]);
            }
        }
        
        var limit = DateTime.Now.Subtract(TimeSpan.FromDays(7));
            
        ViewData["domains"] = await _database.Domains.Where(d =>  d.LastSeen >= limit).ToArrayAsync();
        
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

                ViewData["succes"] = _localizer["mail_sended"];
            }
            catch(Exception e)
            {
                CoreUtils.AddExceptionError($"l'envoie de mail à {mail.Email}", e, currentUser.UserName);

                ViewData["errors"] = _localizer["contact_error"];
                ViewData["datas"] = mail;
            }
        }
        else if(!string.IsNullOrWhiteSpace(mail?.Subject))
        {
            ViewData["errors"] = _localizer["contact_incorrect"];
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
        ViewData["Paypal_mail"] = (await _database.Settings.FirstOrDefaultAsync(s => s.Name == DataUtils.SettingPaypalMailName))?.GetValueObject();
        ViewData["obj dons"] = (await _database.Settings.FirstOrDefaultAsync(s => s.Name == DataUtils.SettingDonName))?.GetValueObject();
        ViewData["hasOpenNodeAPIKEY"] = !string.IsNullOrWhiteSpace((await _database.Settings.FirstOrDefaultAsync(s => s.Name == DataUtils.SettingOpennodeApikey))?.GetValueObject());

        return View();
    }

    [HttpPost("Error")]
    [HttpGet("Error")]
    public IActionResult DefaultError(int c = -1, string v = null)
    {
        ViewData["code"] = c;

        while (Enum.GetValues<HttpStatusCode>().All(s => (int) s != c) && c > 0)
            c /= 10;

        if (c > 0)
            HttpContext.Response.StatusCode = c;

        ViewData["value"] = v;

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
    
    /// <summary>
    ///     Page d'acceuil avec exemple et explication de l'API.
    /// </summary>
    /// <returns></returns>
    [HttpGet("/api")]
    public IActionResult Api()
    {
        return View("../API/Index");
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
    public async Task<IActionResult> AdvanceSearch([FromForm] AdvanceSearch advSearch, string q = "", int page = 1, DateTime? Before = null, DateTime? After = null, Sort? sortBy = null, SearchType? type = null)
    {
        if(advSearch != null)
        {
            if(string.IsNullOrWhiteSpace(advSearch.Q))
                advSearch.Q = q;

            if(advSearch.Page <= 1)
                advSearch.Page = Math.Max(page, 1);

            advSearch.Before ??= Before;
            advSearch.After  ??= After;
            advSearch.SortBy ??= sortBy;

            if (advSearch.SearchIn == SearchType.All && type != null)
                advSearch.SearchIn = type.Value;
        }

        if(advSearch != null && !advSearch.IsEmpty())
            ViewData["results"] = await _apiService.RechercheByGenresAsync(advSearch);

        ViewData["advs"] = advSearch;
        ViewData["genres"] = await _database.Genres.ToArrayAsync();

        return View(nameof(AdvanceSearch));
    }

    [HttpPost("save_episodes")]
    public async Task<IActionResult> SaveEpisodes([FromForm] int aId, int sId, Dictionary<int, Dictionary<string, string[]>> datas)
    {
        var result = await _apiService.SearchResult(sId, idApi: aId);

        if (result is null)
            return BadRequest("unknown ids");

        var list = new List<EpisodesUrls>();
        
        foreach (var season in datas.Keys)
        {
            foreach (var episode in datas[season].Keys)
            {
                foreach (var url in datas[season][episode])
                {
                    if (int.TryParse(episode, out var epNb))
                    {
                        list.Add(new()
                        {
                            ApiId = result.IdApiFrom,
                            SearchId = result.Id,
                            SeasonNumber = season,
                            EpisodeNumber = epNb,
                            Url = new(url),
                            Valid = true
                        });
                    }
                }
            }
        }

        await _database.EpisodesUrls.AddRangeAsync(list.Where(eu => 
            !_database.EpisodesUrls.Any(e =>
                e.ApiId == eu.ApiId &&
                e.SearchId == eu.SearchId &&
                e.SeasonNumber == eu.SeasonNumber &&
                e.EpisodeNumber == eu.EpisodeNumber &&
                e.Url == eu.Url)
            )
        );

        await _database.SaveChangesAsync();
        
        return Ok();
    }

    [HttpGet("changelanguage")]
    public IActionResult SetCulture(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            id = SiteUtils.SupportedCultures.First();

        var cookie = new
        {
            Name = CookieRequestCultureProvider.DefaultCookieName,
            Value = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(id)),
            Options = new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
        };

        if (Request.Headers.Accept.ToString().Contains("text/html"))
        {
            Response.Cookies.Append(
                cookie.Name,
                cookie.Value,
                cookie.Options
            );

            return string.IsNullOrWhiteSpace(Request.Headers.Origin.ToString()) ? RedirectToAction("Index") : Redirect(Request.Headers.Origin);
        }
        else
        {
            return Ok(cookie);
        }
    }
}