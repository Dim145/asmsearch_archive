using AnimeSearch.Database;
using AnimeSearch.Models;
using AnimeSearch.Services;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using React;
using React.AspNet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AnimeSearch
{
    public sealed class Utilities
    {
        /// <summary>
        ///     Liste des genres displonible sur TheMovieDB (contient nom en string & id en int)
        /// </summary>
        public static List<TheMovieDBGenres> TMBD_GENRES { get; private set; } = new();

        /// <summary>
        ///     Liste des erreures surevenie au cours des recherches (erreurs http & erreurs url)
        /// </summary>
        public static List<string> Errors { get; private set; } = new();

        public static string GUEST { get; private set; } = "Guest";

        public static string BALISE_SCRIPT_DEBUT { get; private set; } = "<script";
        public static string BALISE_SCRIPT_FIN { get; private set; } = "</script>";
        public static string BALISE_LINK_DEBUT { get; private set; } = "<link";
        public static string BALISE_LINK_FIN { get; private set; } = ">";

        public static string URL_BASE_ANIME_API { get; private set; } = "https://api.tvmaze.com/";
        public static string URL_ANIME_SINGLE_SEARCH { get; private set; } = URL_BASE_ANIME_API + "singlesearch/shows?q=";
        public static string URL_ANIME_SEARCH { get; private set; } = URL_BASE_ANIME_API + "search/shows?q=";
        public static string URL_ANIME_SHOWS { get; private set; } = URL_BASE_ANIME_API + "shows/";

        public static string URL_BASE_FILMS { get; private set; } = "https://api.themoviedb.org/3/";
        public static string URL_FILMS_SEARCH { get; private set; } = URL_BASE_FILMS + "search/multi?include_adult=true&query=";
        public static string URL_FILMS_SHOWS { get; private set; } = URL_BASE_FILMS + "movie/";
        public static string URL_MOVIEDB_TV_SHOWS { get; private set; } = URL_BASE_FILMS + "tv/";

        public static string URL_DUCKDUCKGO_SEARCH { get; private set; } = "https://html.duckduckgo.com/html";

        public static string[] LAGUAGE_ORDER { get; private set; } = new string[] { "vostfr", "vf", "multi" };

        public static Random RANDOM { get; private set; } = new Random(new Random().Next(int.MaxValue));

        public static Citations CITATION_DU_JOUR { get; set; }

        /// <summary>
        ///     Liste de toutes les classes qui sont des sites à intérrogés. 
        ///     (hérite de Search, dans le namespace "AnimeSearch.Models.Sites" & contient champs statique "TYPE" )
        /// </summary>
        public static List<Type> AllSearchSite { get; private set; } = AppDomain.CurrentDomain.GetAssemblies().SelectMany(t => t.GetTypes())
            .Where(t => t.IsClass && !t.Name.Contains("<>") && t.Namespace == "AnimeSearch.Models.Sites" && t.GetField("TYPE") != null).ToList();
        public static string BASE_URL { get; internal set; }

        public static string MOVIEDB_API_KEY { get; private set; }
        public static string PAYPAL_MAIL { get; private set; }
        public static DonsTimeoutService LAST_DONS_SERVICE { get; internal set; }
        public static string GOOGLE_ADS_ID { get; private set; }
        public static DateTime LAST_DATE_CHECKSITE_EXECUTION { get; internal set; }
        public static List<BaseService> SERVICES { get; } = new();
        public static double DONS_OBJECTIF_MONTH { get; private set; }
        public static bool IN_DEV { get; internal set; } = false;
        internal static IReactSiteConfiguration ConfigReact { private get; set; }

        public Utilities(IConfiguration config)
        {
            MOVIEDB_API_KEY = config["themoviedb_api"];
            PAYPAL_MAIL = config["Paypal_mail"];
            GOOGLE_ADS_ID = config["GoogleADS_id"];
            DONS_OBJECTIF_MONTH = GetDouble(config["objectif-dons-month"]);

            // On fait exprès de ne pas attendre le retour de TMDB afin de ne pas ralentir le demmarage.
            // Le délais entre la réponse de l'api et le besoins des données est suffisament élevé. (sauf pb/ralentissement ?)
            Task _ = InitialiseTMDB_Genres();
        }

        private static async Task InitialiseTMDB_Genres()
        {
            if (TMBD_GENRES.Count == 0)
            {
                HttpClient client = new();

                HttpResponseMessage response = await client.GetAsync("https://api.themoviedb.org/3/genre/movie/list?api_key=" + MOVIEDB_API_KEY);

                if (response.IsSuccessStatusCode)
                {
                    string strResponse = await response.Content.ReadAsStringAsync();
                    TMBD_GENRES.AddRange(JsonConvert.DeserializeObject<Dictionary<string, List<TheMovieDBGenres>>>(strResponse).GetValueOrDefault("genres"));
                }
            }
        }

        public static async Task Update_IP_Localisation(IP ip)
        {
            if (ip == null || string.IsNullOrWhiteSpace(ip.Adresse_IP))
                return;

            try
            {
                HttpResponseMessage response = await new HttpClient().GetAsync("http://ip-api.com/json/" + ip.Adresse_IP);


                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();

                    var localisation = JsonConvert.DeserializeAnonymousType(json, new
                    {
                        country = string.Empty,
                        regionName = string.Empty,
                        city = string.Empty,
                        status = string.Empty
                    });

                    if (localisation != null && localisation.status == "success")
                    {
                        ip.Localisation = localisation.country + "/" + localisation.regionName + "/" + localisation.city;
                    }
                }
            }
            catch (Exception e)
            {
                AddExceptionError("IP-Localisation", e);
            }
        }

        public static void AddExceptionError(string zone, Exception e)
        {
            Errors.Add($"Erreur dans {zone} à {DateTime.Now:dd/MM/yyyy HH:mm:ss}:\n{e.Message}");
        }

        public static double GetDouble(string value, double defaultValue = 0.0)
        {
            // Try parsing in the current culture
            if (!double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out double result) &&
                // Then try in US english
                !double.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out result) &&
                // Then in neutral language
                !double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                result = defaultValue;
            }

            return result;
        }

        public static TimeSpan GetTimeBeforeNextCheckSiteService()
        {
            return CheckSites.FREQUENCE - DateTime.Now.Subtract(LAST_DATE_CHECKSITE_EXECUTION);
        }

        /// <summary>
        ///     Permet d'automatique ajouter le html des code react ainsi que la balise script associer sur une vue.
        /// </summary>
        /// <typeparam name="a">Le type de l'objet représentant les propriétés. Peut être une class annonyme (new{} dans les params)</typeparam>
        /// <param name="html">Le <see cref="IHtmlHelper"/> associer à la vue (variable Html)</param>
        /// <param name="url">l'url complête du fichier JS ex: "~/js/react/Test.jsx". L'url doit commencer par ~/ pour être remplacer par l'url de bse et le nom du fichier doit matcher exactement le nom de la class React.</param>
        /// <param name="props">Objet représentant les propriétés. ex: "new { message = 'test'}"</param>
        /// <returns>un objet de type <see cref="IHtmlContent"/> qui peut être utilisé directement dans la vue. ex: "@Utilities.ReactComponent('~/js/react/Test.jsx', new {message = 'test'})"</returns>
        public static IHtmlContent ReactComponent<a>(IHtmlHelper html, string url, a props)
        {
            if (!ConfigReact.Scripts.Contains(url))
                ConfigReact.AddScript(url);

            var builder = new HtmlContentBuilder();

            url = url.Replace("~/", BASE_URL);

            builder.AppendHtml(html.React(url[(url.LastIndexOf("/")+1)..url.LastIndexOf(".")], props));
            builder.AppendHtml($"<script src='{url}'></script>");

            return builder;
        }

        /// <summary>
        ///     Permet de creer un tableau de n'importe quel type de façon plus succinte qu'habituellement.
        ///     new string[]{ "t1", "t2" } devient
        ///     Tab( "t1", "t2" )
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tab"></param>
        /// <returns></returns>
        public static T[] Tab<T>(params T[] tab)
        {
            return tab;
        }

        public static object[] Tab(params object[] tab)
        {
            return tab;
        }
    }
}
