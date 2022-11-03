using System.Globalization;
using AnimeSearch.Core;
using AnimeSearch.Core.Extensions;
using AnimeSearch.Core.Models;
using AnimeSearch.Core.Models.Api;
using AnimeSearch.Core.Models.Search;
using AnimeSearch.Core.ViewsModel;
using AnimeSearch.Data;
using AnimeSearch.Data.Models;
using AnimeSearch.Services;
using AnimeSearch.Services.Background;
using AnimeSearch.Services.Database;
using AnimeSearch.Services.Mails;
using AnimeSearch.Services.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace AnimeSearch.Api.Controllers;

[Route("/api")]
[ApiController]
public class ApiController : BaseApiController
{
    private readonly UserManager<Users> _userManager;
    private readonly MailService _mailService;
    private readonly SiteSearchService _siteSearchService;
    private readonly ApiService _apiService;
    private readonly HttpClient _client;
    private readonly DatasUtilsService _datasUtilsService;

    public ApiController(AsmsearchContext database, 
        MailService ms, 
        UserManager<Users> userManager,
        SiteSearchService siteSearchService,
        ApiService apiService,
        HttpClient client,
        DatasUtilsService datasUtilsService): base(database)
    {
        _userManager = userManager;
        _mailService = ms;
        _siteSearchService = siteSearchService;
        _apiService = apiService;
        _client = client;
        _datasUtilsService = datasUtilsService;
    }

    /// <summary>
    ///     Recherche une série/anime sur les sites correspondant.
    /// </summary>
    /// <param name="search">texte de recherche</param>
    /// <returns>JSON d'un <see cref="ModelAPI"/></returns>
    [HttpGet("search/serie/{search}")]
    public async Task<ActionResult<ModelAPI>> SearchSerieAsync(string search)
    {
        if (string.IsNullOrEmpty(search))
            return BadRequest("Valeur Null");

        var res = await _siteSearchService.Search("serie=>" + search, HttpContext.Request.Cookies);

        if(res.Result.Url != null)
            await ApiUtils.CreateOrUpdateSearch(User?.Identity?.Name, res.Result.Name, _database);

        return Ok(res);
    }

    /// <summary>
    ///     Recherche films/séries/animes/hentai sur les sites correspondant.
    /// </summary>
    /// <param name="search">text de recherche</param>
    /// <returns>JSON d'un <see cref="ModelAPI"/></returns>
    [HttpGet("search/{search}")]
    public async Task<ActionResult<ModelAPI>> SearchAsync(string search)
    {
        if (string.IsNullOrEmpty(search))
            return BadRequest("Valeur Null");

        var res = await _siteSearchService.Search(search, HttpContext.Request.Cookies);

        if (res.Result.Url != null)
            await ApiUtils.CreateOrUpdateSearch(User?.Identity?.Name, res.Result.Name, _database);

        return Ok(res);
    }

    /// <summary>
    ///     Recherche un film/hentai sur les sites correspondant.
    /// </summary>
    /// <param name="search">une recherche</param>
    /// <returns>JSON d'un <see cref="ModelAPI"/></returns>
    [HttpGet("search/film/{search}")]
    public async Task<ActionResult<ModelAPI>> SearchFilmAsync(string search)
    {
        if (string.IsNullOrEmpty(search))
            return BadRequest("Valeur Null");

        var res = await _siteSearchService.Search("movie=>" + search, HttpContext.Request.Cookies);

        if (res.Result.Url != null)
            await ApiUtils.CreateOrUpdateSearch(User?.Identity?.Name, res.Result.Name, _database);

        return Ok(res);
    }

    [HttpGet("results/{search}")]
    public OkObjectResult GetResults(string search, int page = 1, ResultType type = ResultType.All)
    {
        var res = DataUtils.Apis.Select(a => a.SearchMany(search, page, type)).ToArray();

        Task.WaitAll(res);

        return Ok(res.SelectMany(r => r.Result.Results).OrderByDescending(r => r.Popularity));
    }

    /// <summary>
    ///     Route qui permet de savoir si le serveur à accès à un site (dépend des parametres réseaux et proxy du serveur)
    /// </summary>
    /// <param name="url">URL complète d'un site. Doit commencer par "http"</param>
    /// <returns></returns>
    [HttpGet("/testURL")]
    public async Task<IActionResult> TestUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("http"))
            return BadRequest();

        return await ServiceUtils.TestUrl(url, _client) ? Ok() : NotFound();
            
    }

    /// <summary>
    ///     Récupère le code html d'un site sans les scripts et les balise "link".
    /// </summary>
    /// <param name="url"></param>
    /// <param name="postValues"></param>
    /// <returns></returns>
    [HttpPost("/GetHTMLSite")]
    public async Task<ActionResult<string>> GetHtmlSiteAsync(string url, [FromForm] Dictionary<string, string> postValues)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("http"))
            return BadRequest();

        var response = await (postValues == null || postValues.Count == 0 ? _client.GetAsync(url) : _client.PostAsync(url, new FormUrlEncodedContent(postValues)));

        if (!response.IsSuccessStatusCode) return NotFound();
            
        var html = await response.Content.ReadAsStringAsync();

        html = html.Replace("class=\"footer\"", ""); // pas important, mais cette class gene l'utilisation sur un dialog => plus simple

        int indexStart = html.IndexOf(ApiUtils.BALISE_SCRIPT_DEBUT, StringComparison.InvariantCulture); // enleve les scripts
        while(indexStart > -1)
        {
            int indexEnd = html.IndexOf(ApiUtils.BALISE_SCRIPT_FIN, StringComparison.InvariantCulture) + ApiUtils.BALISE_SCRIPT_FIN.Length;

            html = string.Concat(html.AsSpan(0, indexStart), html.AsSpan()[indexEnd..]);
            indexStart = html.IndexOf(ApiUtils.BALISE_SCRIPT_DEBUT, StringComparison.InvariantCulture);
        }

        indexStart = html.IndexOf(ApiUtils.BALISE_LINK_DEBUT, StringComparison.InvariantCulture); // enleve le css
        while(indexStart > -1)
        {
            int indexEnd = html.IndexOf(ApiUtils.BALISE_LINK_FIN, indexStart, StringComparison.InvariantCulture) + ApiUtils.BALISE_LINK_FIN.Length;

            html = string.Concat(html.AsSpan(0, indexStart), html.AsSpan()[indexEnd..]);
            indexStart = html.IndexOf(ApiUtils.BALISE_LINK_DEBUT, StringComparison.InvariantCulture);
        }

        html = html.Replace("\"/", string.Concat("\"", url.AsSpan(0, url.LastIndexOf("/", StringComparison.InvariantCulture)+1))); // permet de garder la plupart des images ect...
        html = html.Replace("data-src", "src"); // permet de bypass l'inclusion dynamique des images.

        return Ok(html);


    }

    /// <summary>
    ///     Ajoute un site dans la base de donnée si les données sont valides. 
    ///     Le site ne seras pas utilisé directement et devras être validé par un administrateur.
    /// </summary>
    /// <param name="site"></param>
    /// <returns></returns>
    [HttpPost("addSite")]
    public async Task<IActionResult> AddSite([FromForm] Sites site)
    {
        if (site == null || string.IsNullOrWhiteSpace(site.Url) || string.IsNullOrWhiteSpace(site.CheminBaliseA) || !await _database.TypeSites.AnyAsync(t => t.Name == site.TypeSite))
            return BadRequest("Données invalides");

        if (!site.Url.EndsWith("/"))
            site.Url += "/";

        if (site.UrlSearch == null)
            site.UrlSearch = "";

        if ( site.UrlSearch.StartsWith("/"))
            site.UrlSearch = site.UrlSearch[1..];

        if (await _database.Sites.AnyAsync(s => s.Url.Equals(site.Url)))
            return BadRequest("Site déjà dans la base de données");

        if (!await ServiceUtils.TestUrl(site.Url, _client))
            return BadRequest("Le site proposé est innacessible");

        site.Etat = EtatSite.NON_VALIDER; // par sécurité

        await _database.Sites.AddAsync(site);
        await _database.SaveChangesAsync();

        return Ok(site.Url + " proposé avec succes. L'ajout seras effectué sur confirmation d'un administrateur.");
    }

    [HttpPost("citation")]
    public async Task<IActionResult> AddCitation([FromForm] Citations c)
    {
        if (c == null || string.IsNullOrWhiteSpace(c.AuthorName) || string.IsNullOrWhiteSpace(c.Contenue))
            return BadRequest("Les valeurs ne doivent pas être vide!");

        c.IsValidated = false;
        c.DateAjout = DateTime.Now;

        if (User.Identity != null)
            c.UserId = (await _database.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name))?.Id;

        try
        {
            await _database.Citations.AddAsync(c);
            await _database.SaveChangesAsync();

            return Ok("Citation proposée avec succès. Elle pourras être choisie par le serveur après validation d'un administrateur.");
        }
        catch(Exception e)
        {
            CoreUtils.AddExceptionError($"l'ajout de citations ({c.AuthorName} - {c.Contenue})", e, currentUser?.UserName);

            return Conflict("Erreur interne lors de l'ajout");
        }
    }

    [HttpPost("TestSiteSearch")]
    public async Task<IActionResult> TestSitesSearch([FromForm] Sites s, string q = "One Piece")
    {
        if (s == null || string.IsNullOrWhiteSpace(s.Url) || string.IsNullOrWhiteSpace(s.CheminBaliseA) || string.IsNullOrWhiteSpace(s.TypeSite))
            return BadRequest("Donnée invalides");

        Search search = s.ToDynamicSite(s.PostValues is {Count: > 0});

        await search.SearchAsync(q);

        return Ok(new
        {
            nb_result = search.GetNbResult(),
            url = search.GetBaseURL(),
            search = "One piece",
            pageHTML = search.GetNbResult() <= 0 ? search.SearchResult : string.Empty,
            js = search.GetJavaScriptClickEvent()
        });
    }

    [HttpGet("donate")]
    [HttpPost("donate")]
    public async Task<IActionResult> MakeDonation(string amount, int type)
    {
        double amountValue = Math.Abs(amount.ToDouble(1.0));

        if(type == 0) // PayPal
        {
            try
            {
                var email = (await _database.Settings.FirstOrDefaultAsync(s => s.Name == DataUtils.SettingPaypalMailName))?.GetValueObject<string>();

                Guid guid = Guid.NewGuid();

                // par sécurité...
                while (await _database.Dons.AnyAsync(d => d.Id == guid))
                    guid = Guid.NewGuid();

                string username = User?.Identity?.Name;

                if (string.IsNullOrWhiteSpace(username))
                    Request.Cookies.TryGetValue("username", out username);

                if (string.IsNullOrWhiteSpace(username))
                    username = ApiUtils.GUEST;

                Don d = new() { Id = guid, Amout = amountValue, Date = DateTime.Now, Done = false, User_id = (await _userManager.FindByNameAsync(username)).Id };
                await _database.Dons.AddAsync(d);
                await _database.SaveChangesAsync();

                Response.Cookies.Append("dgid", guid.ToString(), new() { MaxAge = TimeSpan.FromMinutes(30), IsEssential = true, SameSite = SameSiteMode.Lax });

                string rootUrl = Url.ActionLink("Index", "Home");

                return Redirect($"https://www.paypal.com/cgi-bin/webscr?item_name=Donation pour soutenir le site. Couvrir les frais du serveur/domaines ! Et migrer vers un serveur plus puissant avec suffisament de dons.&" +
                                $"amount={amountValue.ToString(CultureInfo.InvariantCulture)}&business={email}&currency_code=EUR&tax=0&" +
                                $"cmd=_donations&image_url={rootUrl + "ressources/images/full-logo.svg"}&cancel_return={Url.ActionLink("dppd", "API")}&return={Url.ActionLink("fppd", "API")}");
            }
            catch (Exception e)
            {
                CoreUtils.AddExceptionError("l'initialisation de nouveaux dons", e, currentUser?.UserName);

                return Problem("Erreur interne lors de l'initialisation du don...", statusCode: StatusCodes.Status500InternalServerError);
            }
        }
        else if(type == 1) // BTC
        {
            amountValue = Math.Floor(amountValue);

            using var request = new HttpRequestMessage(HttpMethod.Post, DataUtils.OpennodeApiUrl);
            request.Headers.Add("Authorization", (await _database.Settings.FirstOrDefaultAsync(s => s.Name == DataUtils.SettingOpennodeApikey))?.GetValueObject());
            request.Headers.Accept.Add(new("application/json"));
            request.Content = new StringContent(JsonConvert.SerializeObject(new
            {
                amount = amountValue,
                currency = "btc",
                notif_email = _mailService.DestMail,
                order_id = Guid.NewGuid().ToString(),
                callback_url = Url.ActionLink("OpenNodeCallBack", "APIController"),
                success_url = Url.ActionLink("Index", "HomeController"),
                ttl = 1440
            }), System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = JsonConvert.DeserializeAnonymousType(await response.Content.ReadAsStringAsync(), new
                    {
                        data = new
                        {
                            id = Guid.Empty,
                            description = string.Empty,
                            created_at = 0,
                            status = string.Empty,
                            amount = 0,
                            currency = string.Empty,
                            source_fiat_value = 0,
                            address = string.Empty,
                            uri = string.Empty,
                            ttl = 0,
                            order_id = Guid.Empty,
                            hosted_checkout_url = string.Empty
                        }
                    });

                    if (responseData != null) 
                        return Redirect(responseData.data.hosted_checkout_url);
                }
                else
                {
                    CoreUtils.AddExceptionError("la réponse de l'api d'Open Node", new Exception(await response.Content.ReadAsStringAsync()), currentUser?.UserName);

                    return Problem($"L'api OpenNode à mal répondu... Message: '{response.ReasonPhrase}'", statusCode: StatusCodes.Status422UnprocessableEntity);
                }
            }
            catch (Exception ex)
            {
                CoreUtils.AddExceptionError($"Dans l'envoi de la requete à OpenNode ({DataUtils.OpennodeApiUrl}) avec les données: '{await request.Content.ReadAsStringAsync()}'", ex, currentUser?.UserName);

                return Problem("Erreur lors de la requêtes à l'api OpenNode", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        return BadRequest(new { message = "Type inconnue" });
    }

    [HttpGet("dppd")]
    public async Task<IActionResult> Cancel_DonAsync() // méthode appelé automatiquement par le callback de paypal. => ne peut etre en delete
    {
        bool idExist = Request.Cookies.TryGetValue("dgid", out string id);

        if (!idExist) 
            return RedirectToAction("index", "home");
            
        Response.Cookies.Delete("dgid");

        try
        {
            if (id != null) 
                _database.Dons.Remove(new() {Id = Guid.Parse(id)});
            await _database.SaveChangesAsync();
        }
        catch(Exception e)
        {
            CoreUtils.AddExceptionError("le cancel de dons", e, currentUser?.UserName);
        }

        return RedirectToAction("index", "home");
    }

    [HttpGet("fppd")]
    public async Task<IActionResult> Dons_doneAsync() // méthode appelé automatiquement par le callback de paypal. => ne peut etre en patch
    {
        try
        {
            bool idExist = Request.Cookies.TryGetValue("dgid", out string id);

            if(idExist)
            {
                Guid guid = Guid.Parse(id);

                Don don = await _database.Dons.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == guid);
                string mail = (await _database.Settings.FirstOrDefaultAsync(s => s.Name == DataUtils.SettingPaypalMailName))?.GetValueObject();

                Response.Cookies.Delete("dgid");

                if (don != null && !don.Done)
                {
                    don.Done = true;
                    don.Date = DateTime.Now;

                    _database.Dons.Update(don);
                    await _database.SaveChangesAsync();

                    MailRequest mr = new()
                    {
                        Email = string.IsNullOrWhiteSpace(mail) ? _mailService.DestMail : mail,
                        Subject = "Dons effectué",
                        Pseudo = "PayPal (API)",
                        Message = $"Un don de {don.Amout} € à été effectué par {don.User.UserName}"
                    };

                    await _mailService.SendEmailAsync(mr);
                }
            }

            return RedirectToAction("index", "home");
        }
        catch(Exception e)
        {
            CoreUtils.AddExceptionError("la finalisation de dons", e, currentUser?.UserName);

            return RedirectToAction("index", "home");
        }
    }

    [HttpPost("oncb")]
    public async Task<IActionResult> OpenNodeCallBack([FromForm] OpenNodeCallBack callback)
    {
        if(callback != null && Guid.Empty != callback.Id)
        {
            Don d = null;

            if(callback.Status == "paid")
            {
                d = await _database.Dons.FirstOrDefaultAsync(don => don.Id == callback.Id);
            }

            if (d == null)
            {
                d = new()
                {
                    Id = callback.Id,
                    Date = DateTime.Now,
                    Amout = callback.Price - callback.Fee,
                    User_id = (await _userManager.FindByNameAsync(ApiUtils.GUEST)).Id,
                    Done = false
                };

                await _database.Dons.AddAsync(d);
            }

            if (callback.Status == "paid")
            {
                d.Done = true;

                MailRequest mr = new()
                {
                    Email = _mailService.DestMail,
                    Subject = "Don (BitCoin)",
                    Pseudo = "OpenNode (API)",
                    Message = $"Un don de {callback.Price} à été éffectué. Les frais d'open node s'élèves à {callback.Fee} et " +
                              $"la description est: '{callback.Description}'. Il est possible de retrouver la transaction avec le hash suivant: " +
                              $"'{callback.Hashed_order}'"
                };

                await _mailService.SendEmailAsync(mr);

                _database.Update(d);
            }

            await _database.SaveChangesAsync();

            return Ok();
        }

        return BadRequest();
    }

    [HttpGet("recherchesdatas")]
    public async Task<IActionResult> GetRecherchesDatasAsync()
    {
        return Ok(await _datasUtilsService.GetRecherchesDatas());
    }

    [Authorize]
    [HttpGet("searchsForCurrent")]
    public async Task<ActionResult<List<IP>>> GetAllRecherchesForCurrentUser()
    {
        var user = _userManager.FindByNameAsync(User.Identity?.Name).GetAwaiter().GetResult();

        return Ok(await _database.Recherches
            .Where(ip => ip.User_ID == (user != null ? user.Id : -1))
            .Select(ip => new { ip.recherche, ip.Nb_recherches, ip.Derniere_Recherche})
            .OrderByDescending(ip => ip.Derniere_Recherche)
            .ToListAsync());
    }

    [Authorize]
    [HttpPost("saveSearchResult")]
    public async Task<IActionResult> SaveRechercheResult([FromForm] ModelAPI model)
    {
        if (model?.Result == null || model.SearchResults == null || model.SearchResults.Count == 0)
            return BadRequest("Une des valeurs obligatoire est vide (Result, SearchResults)");

        return Ok(await _datasUtilsService.SaveRechercheResult(User.Identity?.Name, model));
    }

    [Authorize]
    [HttpGet("savedSearchs")]
    public async Task<ActionResult<List<SavedSearch>>> GetSavedSearch()
    {
        var user = await _userManager.FindByNameAsync(User.Identity?.Name);

        return Ok(await _datasUtilsService.GetAllSavedSearchs(user.Id));
    }

    [Authorize]
    [HttpGet("savedSearch")]
    public async Task<ActionResult<SavedSearch>> GetSavedSearch(string search)
    {
        var user = await _userManager.FindByNameAsync(User.Identity?.Name);

        return Ok(await _datasUtilsService.GetSavedSearch(search, user.Id));
    }

    [Authorize]
    [HttpGet("DeleteSave")]
    private async Task<ActionResult<bool>> DeleteSave(string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return BadRequest();

        var user = await _userManager.FindByNameAsync(User.Identity?.Name);

        return Ok(await _datasUtilsService.DeleteSave(search, user.Id) > 0);
    }

    [HttpPost("advance_search")]
    public async Task<IActionResult> AdvanceSearchs([FromForm] AdvanceSearch advanceSearch)
    {
        return Ok(await _apiService.RechercheByGenresAsync(advanceSearch));
    }

    [HttpGet("count")]
    public IActionResult GetNbApi() => Ok(DataUtils.Apis.Count);

    [HttpPost("siteClick")]
    public async Task<IActionResult> SiteClick([FromForm] string url)
    {
        return await _datasUtilsService.SiteClick(url) ? Ok() : NotFound();
    }
}