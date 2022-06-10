using AnimeSearch.Database;
using AnimeSearch.Models;
using AnimeSearch.Models.Sites;
using AnimeSearch.Services;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AnimeSearch.Controllers.api
{
    [Route("/api")]
    [ApiController]
    public class APIController : Controller
    {
        private static readonly HttpClient client = new();

        private readonly AsmsearchContext _database;
        private readonly MailService _mailService;

        public APIController(AsmsearchContext database, MailService ms)
        {
            _database = database;
            _mailService = ms;
        }

        /// <summary>
        ///     Page d'acceuil avec exemple et explication de l'API.
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        ///     Recherche une série/anime sur les sites correspondant.
        /// </summary>
        /// <param name="search">texte de recherche</param>
        /// <returns>JSON d'un <see cref="ModelAPI"/></returns>
        [HttpGet("search/serie/{search}")]
        public ActionResult<ModelAPI> SearchSerieAsync(string search)
        {
            if (string.IsNullOrEmpty(search))
                return BadRequest("Valeur Null");

            return Ok(Search("serie=>" + search, HttpContext.Request.Cookies));
        }

        /// <summary>
        ///     Recherche films/séries/animes/hentai sur les sites correspondant.
        /// </summary>
        /// <param name="search">text de recherche</param>
        /// <returns>JSON d'un <see cref="ModelAPI"/></returns>
        [HttpGet("search/{search}")]
        public ActionResult<ModelAPI> SearchAsync(string search)
        {
            if (string.IsNullOrEmpty(search))
                return BadRequest("Valeur Null");

            return Ok(Search(search, HttpContext.Request.Cookies));
        }

        /// <summary>
        ///     Recherche un film/hentai sur les sites correspondant.
        /// </summary>
        /// <param name="search">une recherche</param>
        /// <returns>JSON d'un <see cref="ModelAPI"/></returns>
        [HttpGet("search/film/{search}")]
        public ActionResult<ModelAPI> SearchFilmAsync(string search)
        {
            if (string.IsNullOrEmpty(search))
                return BadRequest("Valeur Null");

            return Ok(Search("movie" + search, HttpContext.Request.Cookies));
        }

        /// <summary>
        ///     Execute une recherche multiple et retourne le json de la liste des résultats.
        /// </summary>
        /// <param name="search">search text</param>
        /// <returns>JSON d'un tableau de <see cref="ModelMultiSearch"/></returns>
        [HttpGet("msearch/{search}")]
        public ActionResult<ModelMultiSearch[]> MSearch(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return BadRequest("Valeur Null");

            return Ok(MultiSearch(search));
        }

        /// <summary>
        ///     Récupère une serie selon le titre anglais.
        /// </summary>
        /// <param name="search">Texte de "recherche"</param>
        /// <returns>le JSON d'un <see cref="TVMazeResult"/></returns>
        [HttpGet("serie/{search}")]
        public async Task<ActionResult<TVMazeResult>> GetSerieAsync(string search) => Ok(await GetSerie(search));

        /// <summary>
        ///     Récupère une liste de films selon un titre.
        /// </summary>
        /// <param name="search">titre d'un film complet ou pas.</param>
        /// <returns>le JSON d'une <see cref="List{TheMovieDBResult}"/></returns>
        [HttpGet("films/{search}")]
        public async Task<ActionResult<List<TheMovieDBResult>>> GetFilmsAsync(string search) => Ok(await GetFilms(search));

        /// <summary>
        ///     Récupère le premier film trouver sur TheMovieDB par le titre.
        /// </summary>
        /// <param name="search">Titre d'un film</param>
        /// <returns>Le JSON d'un <see cref="TheMovieDBResult"/></returns>
        [HttpGet("film/{search}")]
        public async Task<ActionResult<TheMovieDBResult>> GetFilmAsync(string search) => Ok(await GetFilm(search));

        /// <summary>
        ///     Route qui permet de savoir si le serveur à accès à un site (dépend des parametres réseaux et proxy du serveur)
        /// </summary>
        /// <param name="url">URL complète d'un site. Doit commencer par "http"</param>
        /// <returns></returns>
        [HttpGet("/testURL")]
        public async Task<IActionResult> TestURLAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("http"))
                return BadRequest();

            return await TestUrl(url) ? Ok() : NotFound();
            
        }

        /// <summary>
        ///     Récupère le code html d'un site sans les scripts et les balise "link".
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        [HttpPost("/GetHTMLSite")]
        public async Task<ActionResult<string>> GetHTMLSiteAsync(string url, [FromForm] Dictionary<string, string> postValues)
        {
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("http"))
                return BadRequest();

            HttpResponseMessage response = await (postValues == null || postValues.Count == 0 ? client.GetAsync(url) : client.PostAsync(url, new FormUrlEncodedContent(postValues)));

            if(response.IsSuccessStatusCode)
            {
                string html = await response.Content.ReadAsStringAsync();

                html = html.Replace("class=\"footer\"", ""); // pas important, mais cette class gene l'utilisation sur un dialog => plus simple

                int indexStart = html.IndexOf(Utilities.BALISE_SCRIPT_DEBUT); // enleve les scripts
                while(indexStart > -1)
                {
                    int indexEnd = html.IndexOf(Utilities.BALISE_SCRIPT_FIN) + Utilities.BALISE_SCRIPT_FIN.Length;

                    html = html.Substring(0, indexStart) + html[indexEnd..];
                    indexStart = html.IndexOf(Utilities.BALISE_SCRIPT_DEBUT);
                }

                indexStart = html.IndexOf(Utilities.BALISE_LINK_DEBUT); // enleve le css
                while(indexStart > -1)
                {
                    int indexEnd = html.IndexOf(Utilities.BALISE_LINK_FIN, indexStart) + Utilities.BALISE_LINK_FIN.Length;

                    html = html.Substring(0, indexStart) + html[indexEnd..];
                    indexStart = html.IndexOf(Utilities.BALISE_LINK_DEBUT);
                }

                html = html.Replace("\"/","\"" + url.Substring(0, url.LastIndexOf("/")+1)); // permet de garder la plupart des images ect...
                html = html.Replace("data-src", "src"); // permet de bypass l'inclusion dynamique des images.

                return Ok(html);
            }


            return NotFound();
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

            if (!await TestUrl(site.Url))
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

            try
            {
                await _database.Citations.AddAsync(c);
                await _database.SaveChangesAsync();

                return Ok("Citation proposée avec succès. Elle pourras être choisie par le serveur après validation d'un administrateur.");
            }
            catch(Exception e)
            {
                Utilities.AddExceptionError($"l'ajout de citations ({c.AuthorName} - {c.Contenue})", e);

                return Conflict("Erreur interne lors de l'ajout");
            }
        }

        [HttpPost("TestSiteSearch")]
        public async Task<IActionResult> TestSitesSearch([FromForm] Sites s, string q = "One Piece")
        {
            if (s == null || string.IsNullOrWhiteSpace(s.Url) || string.IsNullOrWhiteSpace(s.CheminBaliseA) || string.IsNullOrWhiteSpace(s.TypeSite))
                return BadRequest("Donnée invalides");

            Search search = s.PostValues == null || s.PostValues.Count == 0 ? new SiteDynamiqueGet(s) : new SiteDynamiquePost(s);

            search.SearchResult = await search.SearchAsync(q);

            return Ok(new
            {
                nb_result = search.GetNbResult(),
                url = search.GetBaseURL(),
                search = "One piece",
                pageHTML = search.GetNbResult() <= 0 ? search.SearchResult : string.Empty,
                js = search.GetJavaScriptClickEvent()
            });
        }

        [HttpPost("ippd")]
        public async Task<IActionResult> Init_donAsync([FromForm] string amount)
        {
            double amount_value = Math.Abs(Utilities.GetDouble( amount, 1.0));

            try
            {
                Guid guid = Guid.NewGuid();

                // par sécurité...
                while (await _database.Dons.AnyAsync(d => d.Id == guid))
                    guid = Guid.NewGuid();

                bool usernameExist = Request.Cookies.TryGetValue("username", out string username);

                // Todo récupéré le pseudo/mail/identifiant si dispo
                await _database.Dons.AddAsync(new() { Id = guid, Amout = amount_value, Date = DateTime.Now, Done = false, User = await _database.Users.FirstOrDefaultAsync(u => u.Name == (usernameExist ? username : Utilities.GUEST)) });
                await _database.SaveChangesAsync();

                Response.Cookies.Append("dgid", guid.ToString(), new() { MaxAge = TimeSpan.FromMinutes(30), IsEssential = true, SameSite = SameSiteMode.Lax });
            }
            catch(Exception e)
            {
                Utilities.AddExceptionError("l'initialisation de nouveaux dons", e);

                return RedirectToAction("index", "home");
            }

            return RedirectPermanentPreserveMethod("https://www.paypal.com/cgi-bin/webscr");

        }

        [HttpGet("dppd")]
        public async Task<IActionResult> Cancel_DonAsync() // méthode appelé automatiquement par le callback de paypal. => ne peut etre en delete
        {
            bool idExist = Request.Cookies.TryGetValue("dgid", out string id);

            if(idExist)
            {
                Response.Cookies.Delete("dgid");

                try
                {
                    _database.Dons.Remove(new() { Id = Guid.Parse(id) });
                    await _database.SaveChangesAsync();
                }
                catch(Exception e)
                {
                    Utilities.AddExceptionError("le cancel de dons", e);
                }
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

                    Response.Cookies.Delete("dgid");

                    if (don != null && !don.Done)
                    {
                        don.Done = true;
                        don.Date = DateTime.Now;

                        _database.Dons.Update(don);
                        await _database.SaveChangesAsync();

                        MailRequest mr = new()
                        {
                            Email = Utilities.PAYPAL_MAIL,
                            Subject = "Dons effectué",
                            Pseudo = "PayPal (API)",
                            Message = $"Un don de {don.Amout} € à été effectué par {don.User.Name}"
                        };

                        await _mailService.SendEmailAsync(mr);
                    }
                }

                return RedirectToAction("index", "home");
            }
            catch(Exception e)
            {
                Utilities.AddExceptionError("la finalisation de dons", e);

                return RedirectToAction("index", "home");
            }
        }

        // ----------------------------------------------------------------------- static méthodes-----------------------------------------------------------------------------------------

        /// <summary>
        ///     Exécute une requête sur une adresse URL puis renvoi la réponse de celui-ci.
        /// </summary>
        /// <param name="url">Une URL (ex = "https://google.com")</param>
        /// <returns>True si le site répond, false sinon</returns>
        public static async Task<bool> TestUrl(string url)
        {
            try
            {
                var client = APIController.client;

                if(!url.StartsWith("https"))
                {
                    HttpClientHandler handler = new()
                    {
                        ClientCertificateOptions = ClientCertificateOption.Manual,
                        ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) =>
                        {
                            return true;
                        }
                    };

                    client = new(handler);
                }

                HttpResponseMessage response = await client.GetAsync(url);

                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        ///     Récupère les données d'une série dans la base de donnée des séries/animes. Utilise l'algorithme de recherche optimisé de l'api 
        /// </summary>
        /// <param name="search">Le nom d'une série ou d'un anime</param>
        /// <param name="addOtherName">Ajoute les autres nom de la série (prend plus de temps car requête suplémentaire à l'api)</param>
        /// <returns>Un Objet de type <see cref="TVMazeResult"/></returns>
        public static async Task<TVMazeResult> GetSerie(string search, bool addOtherName = true)
        {
            HttpResponseMessage response = await client.GetAsync(Utilities.URL_ANIME_SINGLE_SEARCH + search);

            TVMazeResult serie = null;

            if (response.IsSuccessStatusCode)
            {
                string json = (await response.Content.ReadAsStringAsync()).Replace("Chinese", "zh-Hans");

                serie = JsonConvert.DeserializeObject<TVMazeResult>(json);

                if (serie != null && addOtherName)
                    await AddAkasToTVMazeAsync(serie);
            }

            return serie;
        }

        public static async Task<TVMazeResult> GetSerie(int id, bool addOtherName = true)
        {
            HttpResponseMessage response = await client.GetAsync(Utilities.URL_ANIME_SHOWS + id);

            TVMazeResult serie = null;

            if (response.IsSuccessStatusCode)
            {
                string json = (await response.Content.ReadAsStringAsync()).Replace("Chinese", "zh-Hans");

                serie = JsonConvert.DeserializeObject<TVMazeResult>(json);

                if (serie.GetImage() == null)
                    serie.SetImage(serie.Image?.GetValueOrDefault("original"));

                if (serie != null && addOtherName)
                    await AddAkasToTVMazeAsync(serie);
            }

            return serie;
        }

        public static async Task<TheMovieDBResult> GetFilm(int id, bool type, bool addOtherName = true)
        {
            string url = (type ? Utilities.URL_MOVIEDB_TV_SHOWS : Utilities.URL_FILMS_SHOWS) + id + "?api_key=" + Utilities.MOVIEDB_API_KEY;
            HttpResponseMessage response = await client.GetAsync(url);

            TheMovieDBResult movie = null;

            if(response.IsSuccessStatusCode)
            {
                string json = (await response.Content.ReadAsStringAsync()).Replace("Chinese", "zh-Hans");

                movie = JsonConvert.DeserializeObject<TheMovieDBResult>(json);

                if (movie != null && addOtherName)
                {
                    movie.SetTypeResult(type ? "tv" : "movie"); // todo Changer ou automatiser cette valeur. La recherche avec id se fait forcement pour des films^pour le moment.

                    movie.SetUrl(new("https://themoviedb.org/" + movie.GetTypeResult() + "/" + movie.GetId()));

                    HttpResponseMessage responseLanguage = await client.GetAsync("https://api.themoviedb.org/3/" + movie.GetTypeResult() + "/" + movie.GetId() + "/alternative_titles?api_key=" + Utilities.MOVIEDB_API_KEY);

                    if (responseLanguage.IsSuccessStatusCode)
                    {
                        var languages = JsonConvert.DeserializeAnonymousType(await responseLanguage.Content.ReadAsStringAsync(), new
                        {
                            id = 0,
                            results = new List<Dictionary<string, string>>(),
                            titles = new List<Dictionary<string, string>>()
                        });

                        List<Dictionary<string, string>> dicToUse = languages.results ?? languages.titles;

                        foreach (Dictionary<string, string> vals in dicToUse)
                        {
                            try
                            {
                                CultureInfo lang = CultureInfo.CreateSpecificCulture(vals.GetValueOrDefault("iso_3166_1"));
                                movie.AddOtherName(lang, vals.GetValueOrDefault("title"));
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                }
            }

            return movie;
        }

        public static async Task<List<TVMazeResult>> GetSeries(string search, bool addOtherNames = true)
        {
            HttpResponseMessage response = await client.GetAsync(Utilities.URL_ANIME_SEARCH + search);

            List<TVMazeResult> listSeries = null;

            if (response.IsSuccessStatusCode)
            {
                string json = (await response.Content.ReadAsStringAsync()).Replace("Chinese", "zh-Hans");

                Newtonsoft.Json.Linq.JArray jsonArray = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JArray>(json);

                listSeries = new();

                foreach (Newtonsoft.Json.Linq.JObject obj in jsonArray)
                {

                    TVMazeResult res = JsonConvert.DeserializeObject<TVMazeResult>(obj.GetValue("show").ToString());

                    if (res.GetImage() == null)
                        res.SetImage(res.Image?.GetValueOrDefault("original"));

                    listSeries.Add(res);
                }

                if (addOtherNames && listSeries != null && listSeries.Count > 0)
                {
                    Task[] tasks = new Task[listSeries.Count];

                    for (int cpt = 0; cpt < tasks.Length; cpt++)
                        tasks[cpt] = AddAkasToTVMazeAsync(listSeries[cpt]);

                    Task.WaitAll(tasks);
                }
            }

            return listSeries;
        }

        public static async Task<TheMovieDBResult> GetFilm(string search)
        {
            List<TheMovieDBResult> list = (await GetFilms(search));

            if (list != null && list.Count > 0)
            {
                TheMovieDBResult selectedResult = list.Where(item => item.GetAllOtherNamesList().Contains(search))?.FirstOrDefault();

                return selectedResult ?? list.FirstOrDefault();
            }

            return null;
        }

        public static async Task<List<TheMovieDBResult>> GetFilms(string search, bool addOtherNames = true)
        {
            HttpResponseMessage response = await client.GetAsync(Utilities.URL_FILMS_SEARCH + search + "&api_key=" + Utilities.MOVIEDB_API_KEY);

            if (response.IsSuccessStatusCode)
            {
                var tmp = JsonConvert.DeserializeAnonymousType(await response.Content.ReadAsStringAsync(), new { page = 0, results = new List<TheMovieDBResult>() });

                List<Task> listTask = new();
                foreach(TheMovieDBResult movie in tmp.results)
                {
                    movie.SetUrl(new("https://themoviedb.org/" + movie.GetTypeResult() + "/" + movie.GetId()));

                    if(addOtherNames) listTask.Add(Task.Run(async () =>
                    {
                        HttpResponseMessage responseLanguage = await client.GetAsync("https://api.themoviedb.org/3/" + movie.GetTypeResult() + "/" + movie.GetId() + "/alternative_titles?api_key=" + Utilities.MOVIEDB_API_KEY);

                        if (responseLanguage.IsSuccessStatusCode)
                        {
                            var languages = JsonConvert.DeserializeAnonymousType(await responseLanguage.Content.ReadAsStringAsync(), new
                            {
                                id = 0,
                                results = new List<Dictionary<string, string>>(),
                                titles = new List<Dictionary<string, string>>()
                            });

                            List<Dictionary<string, string>> dicToUse = languages.results ?? languages.titles;

                            foreach (Dictionary<string, string> vals in dicToUse)
                            {
                                CultureInfo lang = null;

                                try
                                {
                                    lang = CultureInfo.CreateSpecificCulture(vals.GetValueOrDefault("iso_3166_1"));
                                    movie.AddOtherName(lang, vals.GetValueOrDefault("title"));
                                }
                                catch (Exception)
                                {

                                }
                            }
                        }
                    }));
                }

                Task.WaitAll(listTask.ToArray());

               return tmp.results;
            }

            return new List<TheMovieDBResult>();
        }

        public static ModelAPI Search(Result searchResult, IRequestCookieCollection r, List<Sites> databaseSites = null)
        {
            var result = new ModelAPI()
            {
                Search = searchResult?.GetName()
            };

            if (searchResult == null || r == null)
                return result;

            bool isPresent = r.TryGetValue("languageOrder", out string cookies);

            List<CultureInfo> languageOrder = new();

            if (isPresent)
            {
                string[] languages = cookies.Split("|");

                foreach (string s in languages)
                    if (s != "")
                        languageOrder.Add(new(s));
            }

            Task infos = Task.Run(async () =>
            {
                NautiljonSearch nautiljon = new(searchResult is TheMovieDBResult r && r.IsHentai()? NautiljonSearch.FILTER_NONE : searchResult.IsAnime() ? NautiljonSearch.FILTER_ANIME : searchResult.IsFilm() ? NautiljonSearch.FILTER_FILM : NautiljonSearch.FILTER_NONE);
                string javascript = "";

                if (searchResult.IsAnime())
                {
                    nautiljon.SearchResult = await nautiljon.SearchAsync(searchResult.GetName());

                    if (nautiljon.GetNbResult() == 0)
                    {
                        foreach (string otherName in searchResult.GetAllOtherNamesList(languageOrder))
                        {
                            if (!isPresent && otherName == searchResult.GetName()) continue;

                            nautiljon.SearchResult = await nautiljon.SearchAsync(otherName);

                            if (nautiljon.GetNbResult() > 0)
                                break;
                        }
                    }

                    if (nautiljon.GetNbResult() != 0 && searchResult.GetImage() == null)
                    {
                        bool succes = Uri.TryCreate(await nautiljon.GetImageResultAsync(), UriKind.RelativeOrAbsolute, out Uri u);

                        if (succes) searchResult.SetImage(u);
                    }
                }
                
                if (nautiljon.GetNbResult() <= 0)
                {
                    AlloCineSearch allo = new();

                    if (searchResult is TheMovieDBResult)
                        allo.SearchResult = await allo.SearchAsync(searchResult.GetName());

                    if (allo.GetNbResult() <= 0)
                    {
                        WikiPediaSearch wiki = new();
                        wiki.SearchResult = await wiki.SearchAsync(searchResult.GetName());
                        wiki.GetNbResult();

                        javascript = wiki.GetJavaScriptClickEvent();
                    }
                    else
                    {
                        javascript = allo.GetJavaScriptClickEvent();
                    }
                }
                else
                {
                    javascript = nautiljon.GetJavaScriptClickEvent();
                    result.Bande_Annone = await nautiljon.GetBandeAnnonceVideoURL();
                }


                int start = javascript.IndexOf("\"") + 1;
                result.InfoLink = javascript[start..javascript.LastIndexOf("\"")];
            });

            Task bandeAnnonce = Task.Run(async () =>
            {
                Dictionary<string, string> listValue = new();
                listValue.Add("q", searchResult.GetName() + " bande annonce");

                HttpResponseMessage response = await client.PostAsync(Utilities.URL_DUCKDUCKGO_SEARCH, new FormUrlEncodedContent(listValue));

                if(response.IsSuccessStatusCode)
                {
                    HtmlDocument doc = new();

                    string html = await response.Content.ReadAsStringAsync();
                    doc.LoadHtml(html);

                    HtmlNode mainDiv = doc.GetElementbyId("links");

                    if(string.IsNullOrWhiteSpace(result.Bande_Annone))
                    {
                        HtmlNodeCollection otherResults = mainDiv?.SelectNodes("div/div/h2/a");

                        if(otherResults != null)
                        {
                            string url = otherResults.Select(node => node.Attributes["href"].Value).Where(url => url.Contains("youtube", StringComparison.InvariantCultureIgnoreCase) || url.Contains("allocine", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

                            if( url != null && url.Contains("youtube") )
                                url = url.Replace("watch", "embed").Replace("?v=", "/");

                            if (url != null)
                                result.Bande_Annone = url;
                        }
                    }
                }
            });

            List<Search> listSiteToSearch = new();

            listSiteToSearch.AddRange( Utilities.AllSearchSite
                .Where (t => IsTypeSiteForResult(searchResult, t.GetField("TYPE")?.GetValue(null)?.ToString())) // filtre les type pour garder les bons
                .Select(t => (Search) t.GetConstructor(Array.Empty<Type>()).Invoke(null))); // apelle les constructeurs et l'ajoute a la liste

            if (databaseSites != null)
            {
                listSiteToSearch.AddRange( databaseSites
                    .Where (s => IsTypeSiteForResult(searchResult, s.TypeSite)) // filtre les types
                    .Select(s => (Search) (s.PostValues == null || s.PostValues.Count == 0 ? new SiteDynamiqueGet(s) : new SiteDynamiquePost(s))) // creer les sites
                    .Where (s => !listSiteToSearch.Contains(s)) ); // verifie qu'il n'y as pas de doublons
            }

            if (!isPresent)
                Task.WaitAll(listSiteToSearch.Select(item => item.SearchAsync(searchResult.GetName())).Where(task => task != null).ToArray());

            List<Task> tabTask = new();
            foreach (Search s in listSiteToSearch)
            {
                if (s.GetNbResult() == 0) tabTask.Add(Task.Run(async () =>
                {
                    foreach (string otherName in searchResult.GetAllOtherNamesList(languageOrder))
                    {
                        if (!isPresent && otherName == searchResult.GetName()) continue;

                        Task<string> t = s.SearchAsync(otherName);

                        if (t != null)
                            s.SearchResult = await t;

                        if (s.GetNbResult() > 0)
                            break;
                    }
                }));
            }

            Task.WaitAll(tabTask.ToArray());

            foreach (Search s in listSiteToSearch)
                if (s.GetNbResult() > -1)
                {
                    string url = null;

                    if (s is SearchGet)
                    {
                        string javascript = s.GetJavaScriptClickEvent();
                        int start = javascript.IndexOf("\"") + 1;

                        url = javascript[start..javascript.LastIndexOf("\"")];
                    }

                    ModelSearchResult model = new()
                    {
                        NbResults = s.GetNbResult(),
                        SiteUrl = s.GetBaseURL(),
                        IconUrl = s.GetUrlImageIcon(),
                        Url = url,
                        OpenJavaScript = s.GetJavaScriptClickEvent(),
                        Type = s.GetTypeSite()
                    };

                    result.SearchResults.Add(s.GetSiteTitle(), model);
                }

            infos.Wait();
            bandeAnnonce.Wait();

            result.Result = searchResult;

            return result;
        }

        public static ModelAPI Search(string search, IRequestCookieCollection r, List<Sites> databaseSites = null)
        {
            if (string.IsNullOrEmpty(search))
                return null;

            bool serieFirst = true;

            if(search.Contains("=>"))
            {
                int index = search.IndexOf("=>");

                string before = search.Substring(0, index);
                search = search[(index + 2)..];

                serieFirst = before != "movie";
            }

            Task<TheMovieDBResult> taskMovie = GetFilm(search);
            Task<TVMazeResult> taskSerie = GetSerie(search);

            Task.WaitAll(new Task[] { taskMovie, taskSerie });

            Result searchResult = null;

            Result serie = taskSerie.Result;
            Result movie = taskMovie.Result;

            searchResult = serieFirst ? (serie ?? movie) : (movie ?? serie);

            if (searchResult == null) // ne devrais jamais arriver
            {
                searchResult = new();
                searchResult.SetName(search);
            }
            else if( searchResult.GetImage() == null && serie != null)
            {
                searchResult.SetImage(((TVMazeResult) serie).Image?.GetValueOrDefault("original"));
            }

            return Search(searchResult, r, databaseSites);
        }

        public static ModelAPI Search(int id, IRequestCookieCollection r, int type = -1, List<Sites> databaseSites = null)
        {
            Result res;

            if (type == -1) // au cas ou mais pas tres fiable
            {
                Task<TheMovieDBResult> taskMovie = GetFilm(id, false);
                Task<TVMazeResult> taskSerie = GetSerie(id);

                Task.WaitAll(new Task[] { taskMovie, taskSerie });

                Result serie = taskSerie.Result;
                Result movie = taskMovie.Result;

                res = serie ?? movie;
            }
            else
            {
                res = type == 0 ? GetSerie(id).GetAwaiter().GetResult() : GetFilm(id, type == 2).GetAwaiter().GetResult();
            }

            return Search(res, r, databaseSites);
        }

        public static ModelMultiSearch[] MultiSearch(string search)
        {
            Task<List<TVMazeResult>> taskListSeries = APIController.GetSeries(search, false);
            Task<List<TheMovieDBResult>> taskListFilms = APIController.GetFilms(search, false);

            Task.WaitAll(new Task[] { taskListFilms, taskListSeries });

            List<Result> listResult = new(taskListSeries.Result);
            listResult.AddRange(taskListFilms.Result);

            List<Result> list = new();

            foreach (Result res in listResult)
                if (!list.Contains(res))
                    list.Add(res);

            List<dynamic> returnResult = new();

            return list.Select(r => new ModelMultiSearch()
            {
                Name = r.GetName(),
                Type = r.IsAnime() ? "Anime" : r.IsSerie() ? "Série" : r.IsFilm() ? "Film" : "Autre",
                Date = r.GetRealeaseDate(),
                Img = r.GetImage(),
                Lien = (r is TVMazeResult ? "tv" : r.IsFilm() ? "movie" : "tvmovie") + "/" + r.GetId()
            }).ToArray();
        }

        private static async Task AddAkasToTVMazeAsync(TVMazeResult serie)
        {
            HttpResponseMessage response = await client.GetAsync( Utilities.URL_BASE_ANIME_API + "shows/" + serie.GetId() + "/akas");

            var languages = JsonConvert.DeserializeAnonymousType<object>(await response.Content.ReadAsStringAsync(), new
            {
                Name = "",
                Country = new Dictionary<string, string>()
            });

            if (languages is Newtonsoft.Json.Linq.JArray array)
                foreach (Newtonsoft.Json.Linq.JToken t in array)
                    AddJsonTokenNameToSerie(t, serie);
            else
                AddJsonTokenNameToSerie((Newtonsoft.Json.Linq.JToken)languages, serie);

            serie.AddNameInLanguage();
        }

        private static void AddJsonTokenNameToSerie(Newtonsoft.Json.Linq.JToken t, TVMazeResult serie)
        {
            string name = t.Value<string>("name");

            Newtonsoft.Json.Linq.JObject counry = t.Value<Newtonsoft.Json.Linq.JObject>("country");

            CultureInfo culture;
            try
            {
                string code = counry?.Value<string>("code");

                if (code != null && code.ToLower().Equals("jp"))
                    code = "ja";

                culture = code != null ? new(code) : CultureInfo.InvariantCulture;
            }
            catch (Exception)
            {
                culture = CultureInfo.InvariantCulture;
            }

            serie.AddOtherName(culture, name);
        }

        private static bool IsTypeSiteForResult(Result result, string typeSite)
        {
            if (string.IsNullOrWhiteSpace(typeSite))
                return false;

            return typeSite.Contains("all") ||                     // condition 1, c'est un site qui contient de tous
                result.IsAnime() && typeSite.Contains("animes") || // condition 2, c'est un site d'anime pour un anime
                result.IsSerie() && typeSite.Contains("séries") || // condition 3, c'est un site de serie pour une serie
                result.IsFilm()  && typeSite.Contains("film")   || // condition 4, c'est un site de film pour un film
                (result is TheMovieDBResult result1 && result1.IsHentai())       && typeSite.Contains("hentai") || // condition 5 Hentai
                (result is TheMovieDBResult result2 && result2.IsFilmAnimation() && typeSite.Contains("FA"));      // condition 6 film d'animation 
        }
    }
}
