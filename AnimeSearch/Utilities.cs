using AnimeSearch.Database;
using AnimeSearch.Models;
using AnimeSearch.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Sentry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Reflection;
using System.Threading.Tasks;
using AnimeSearch.Models.Results;
using AnimeSearch.Models.Search;

namespace AnimeSearch
{
    internal sealed class Utilities
    {
        /// <summary>
        ///     Liste des genres displonible sur TheMovieDB (contient nom en string & id en int)
        /// </summary>
        public static List<TheMovieDbGenres> TMBD_GENRES { get; } = new();

        /// <summary>
        ///     Liste des erreures surevenie au cours des recherches (erreurs http & erreurs url)
        /// </summary>
        public static List<string> Errors { get; } = new();

        public static string GUEST => "Guest";

        public static string BALISE_SCRIPT_DEBUT => "<script";
        public static string BALISE_SCRIPT_FIN => "</script>";
        public static string BALISE_LINK_DEBUT => "<link";
        public static string BALISE_LINK_FIN => ">";

        public static string URL_BASE_ANIME_API => "https://api.tvmaze.com/";
        public static string URL_ANIME_SINGLE_SEARCH { get; } = URL_BASE_ANIME_API + "singlesearch/shows?q=";
        public static string URL_ANIME_SEARCH { get; } = URL_BASE_ANIME_API + "search/shows?q=";
        public static string URL_ANIME_SHOWS { get; } = URL_BASE_ANIME_API + "shows/";

        public static string URL_BASE_FILMS => "https://api.themoviedb.org/3/";
        public static string URL_FILMS_SEARCH { get; } = URL_BASE_FILMS + "search/multi?include_adult=true&language=fr-FR&query=";
        public static string URL_FILMS_SHOWS { get; } = URL_BASE_FILMS + "movie/";
        public static string URL_MOVIEDB_TV_SHOWS { get; } = URL_BASE_FILMS + "tv/";
        public static string URL_DISCOVER_TMDB { get; } = URL_BASE_FILMS + "discover/";

        public static string URL_DUCKDUCKGO_SEARCH => "https://html.duckduckgo.com/html";

        public static string[] LAGUAGE_ORDER { get; } = { "vostfr", "vf", "multi" };

        public static Random RANDOM { get; } = new(new Random().Next(int.MaxValue));

        public static Citations CITATION_DU_JOUR { get; set; }

        /// <summary>
        ///     Liste de toutes les classes qui sont des sites à intérrogés. 
        ///     (hérite de Search, dans le namespace "AnimeSearch.Models.Sites" & contient champs statique "TYPE" )
        /// </summary>
        public static List<Type> AllSearchSite { get; } = AppDomain.CurrentDomain.GetAssemblies().SelectMany(t => t.GetTypes())
            .Where(t => t.IsClass && !t.Name.Contains("<>") && t.Namespace == "AnimeSearch.Models.Sites" && t.GetField("TYPE") != null).ToList();
        public static string BASE_URL { get; internal set; }

        public static string MOVIEDB_API_KEY { get; private set; }
        public static DonsTimeoutService LAST_DONS_SERVICE { get; internal set; }
        public static DateTime LAST_DATE_CHECKSITE_EXECUTION { get; internal set; }
        public static List<BaseService> SERVICES { get; } = new();
        public static bool IN_DEV { get; internal set; }

        public static string[] ImgExt { get; } = { "png", "jpg", "svg", "ico", "webp", "jpeg" };
        public static int[] SUPPORTED_ERROR_CODE { get; } = { 400, 401, 403, 404, 405, 500 };
        public static HttpClient CLIENT { get; } = new();
        public static Roles ADMIN_ROLE { get; } = new("Admin", 5) { Color = Color.DarkMagenta };
        public static Roles SUPER_ADMIN_ROLE { get; } = new("Super-Admin", 6) { Color = Color.DodgerBlue };
        internal static string SQL_SERVER_CONNECTIONS_STRING { get; private set; }

        public static int DROIT_VUE_BAS => 1;
        public static int DROIT_VUE_HAUT => 2;
        public static int DROIT_MODIF_BAS => 3;
        public static int DROIT_MODIF_HAUT => 4;
        public static int DROIT_ADD => 5;
        public static int DROIT_DELETE => 6;

        public static ColorConverter ColorConverter { get; } = new();

        public static string TELEGRAM_INVITE_LINK { get; private set; } = string.Empty;
        public static string DISCORD_INVITE_LINK { get; private set; } = string.Empty;

        public static string BASE_URL_IMAGE_TMDB => "https://www.themoviedb.org/t/p/w600_and_h900_bestv2";

        public static string SETTING_DON_NAME => "Objectif Dons";
        public static string SETTING_PAYPAL_MAIL_NAME => "Mail Paypal";
        public static string SETTING_TOKEN_TELEGRAM_NAME => "Token Télégram";
        public static string SETTING_TIME_BEFORE_SS_NAME => "Temps avant expiration des sauvegardes";
        public static string SETTING_ADS_ID_NAME => "Identifiant publicitaire";
        public static string SETTING_GOOGLE_SEARCH_ID_NAME => "Identifiant 'Google search'";
        public static string SETTING_DISCORD_BOT_NAME => "Token Discord";
        public static string SETTING_OPENNODE_APIKEY => "API key OpenNode";

        public static Dictionary<string, string> TMDB_TVMAZE_GENRES_EQ { get; } = new();

        public static string OPENNODE_API_URL => "https://api.opennode.com/v1/charges";

        public static KeyValuePair<string, string>[] SORT_BY_DISCOVERY { get; } = { KeyValuePair.Create("original_title.asc"       , "Titre (Alphabétique a-z)"   ),
                                                                                    KeyValuePair.Create("original_title.desc"      , "Titre (Alphabétique z-a)"   ),
                                                                                    KeyValuePair.Create("primary_release_date.desc", "Date (+ récent en haut)"    ),
                                                                                    KeyValuePair.Create("primary_release_date.asc" , "Date (+ vieux en haut)"     ),
                                                                                    KeyValuePair.Create("popularity.desc"          , "Popularité (+ pop. en haut)"),
                                                                                    KeyValuePair.Create("popularity.asc"           , "Popularité (- pop. en haut)") };
        public static Dictionary<string, Func<Result, object>> SORT_BY_SEARCH { get; } = new(new[] {KeyValuePair.Create<string, Func<Result, object>>("original_title"       , r => r.GetName()   ),
                                                                                               KeyValuePair.Create<string, Func<Result, object>>("primary_release_date", r => r.GetRealeaseDate()    ),
                                                                                               KeyValuePair.Create<string, Func<Result, object>>("popularity"           , r => r.GetPopularity() )});

        private static Utilities Instance { get; set; }
        
        private Utilities()
        {
            
        }

        public static Utilities GetInstance(IConfiguration config = null)
        {
            if (Instance == null)
            {
                Instance = new Utilities();

                if (config != null)
                {
                    if (string.IsNullOrWhiteSpace(MOVIEDB_API_KEY))
                        MOVIEDB_API_KEY = config["themoviedb_api"];

                    if (string.IsNullOrWhiteSpace(SQL_SERVER_CONNECTIONS_STRING))
                        SQL_SERVER_CONNECTIONS_STRING = config["ConnectionStrings:SQLServer"];

                    // On fait exprès de ne pas attendre le retour de TMDB afin de ne pas ralentir le demmarage.
                    // Le délais entre la réponse de l'api et le besoins des données est suffisament élevé. (sauf pb/ralentissement ?)
                    InitialiseTMDB_Genres();

                    TMDB_TVMAZE_GENRES_EQ.Add("animation", "anime");
                    TMDB_TVMAZE_GENRES_EQ.Add("science fiction", "science-fiction");
                }
            }

            return Instance;
        }

        private static void InitialiseTMDB_Genres()
        {
            if (TMBD_GENRES.Count != 0) return;
            
            var type = new { genres = new List<TheMovieDbGenres>() };

            _ = GetAndDeserialiseAnonymousFromUrl($"{URL_BASE_FILMS}genre/movie/list?api_key=" + MOVIEDB_API_KEY, type).ContinueWith(res =>
            {
                if(res.Result?.genres != null)
                    TMBD_GENRES.AddRange(res.Result.genres);
            });

            _ = GetAndDeserialiseAnonymousFromUrl($"{URL_BASE_FILMS}genre/tv/list?api_key=" + MOVIEDB_API_KEY, type).ContinueWith(res =>
            {
                if (res.Result?.genres != null)
                    TMBD_GENRES.AddRange(res.Result.genres.Where(g => !TMBD_GENRES.Any(genre => genre.Id == g.Id)));
            });
        }

        public static void AddExceptionError(string zone, Exception e)
        {
            Errors.Add($"Erreur dans {zone} à {DateTime.Now:dd/MM/yyyy HH:mm:ss}:\n{e.Message}");

            SentrySdk.CaptureException(e);
        }

        public static TimeSpan GetTimeBeforeNextCheckSiteService()
        {
            return CheckSites.FREQUENCE - DateTime.Now.Subtract(LAST_DATE_CHECKSITE_EXECUTION);
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

        /// <summary>
        ///     Permet d'initialiser un tableau de différents types d'objets.
        /// </summary>
        /// <param name="tab"></param>
        /// <returns></returns>
        public static object[] Tab(params object[] tab)
        {
            return tab;
        }

        /// <summary>
        ///     Permet d'intérroger une API distante (ou pas) et désérialise le résultat.
        /// </summary>
        /// <typeparam name="T">Le type de désérialisation</typeparam>
        /// <param name="url">URL de la requête. Elle peut pointer n'importe où tant que le résultat est un JSON.</param>
        /// <param name="opOnBody">Fonction qui prend un string en paramètre et renvoie cette string modifier. S'éxécute juste avant la désérialisation.</param>
        /// <param name="cookies">Ensemble de Cookies envoyer avec la requête.</param>
        /// <param name="TimeOut">Changer le TimeOut par défault.</param>
        /// <returns>Un objet du type donnée si la requête fonctionne. la valeur par default sinon (null le plus souvent)</returns>
        public static async Task<T> GetAndDeserialiseFromUrl<T>(string url, Func<string, string> opOnBody = null, IEnumerable<KeyValuePair<string, string>> cookies = null, TimeSpan? TimeOut = null)
        {
            var client = CLIENT;

            if(cookies != null)
            {
                var cookiesContainer = new CookieContainer();
                client = new(new HttpClientHandler() { CookieContainer = cookiesContainer });

                if (cookies != null && cookies.Any())
                {
                    foreach (var kv in cookies)
                        cookiesContainer.Add(
                            new Cookie(kv.Key, 
                                kv.Value, 
                                "/", 
                                BASE_URL[(BASE_URL.IndexOf("//", StringComparison.InvariantCulture) + 2)..BASE_URL.LastIndexOf(":", StringComparison.InvariantCulture)]));
                }
            }

            if (TimeOut != null)
            {
                client = new();

                client.Timeout = TimeOut.GetValueOrDefault();
            }

            if (!url.StartsWith("http"))
                url = BASE_URL + url[(url.StartsWith("/")?1:0)..];

            var m = await client.GetAsync(url).ConfigureAwait(false);

            if (client != CLIENT)
                client.Dispose();

            if (m.IsSuccessStatusCode)
            {
                var body = await m.Content.ReadAsStringAsync();
                var res  = body;

                if (opOnBody != null)
                    res = opOnBody(body);

                return JsonConvert.DeserializeObject<T>(res ?? body);
            }

            return default;
        }

        /// <summary>
        ///     Permet d'intérroger une API distante (ou pas) et désérialise le résultat selon le type donnée en second param. <br/>
        ///     S'apelle comme ceci: GetAndDeserialiseAnonymousFromUrl("https://url.test", new { test = 0 });
        /// </summary>
        /// <typeparam name="a">Un type anonyme ou object</typeparam>
        /// <param name="url">URL de la requête. Elle peut pointer n'importe où tant que le résultat est un JSON.</param>
        /// <param name="type">Un type anonyme déclarer comme suit: new { }</param>
        /// <param name="cookies">Ensemble de Cookies envoyer avec la requête.</param>
        /// <param name="TimeOut">Changer le TimeOut par défault.</param>
        /// <returns>Un objet du type donnée si la requête fonctionne. la valeur par default sinon (null le plus souvent)</returns>
        public static async Task<a> GetAndDeserialiseAnonymousFromUrl<a>(string url, a type, IEnumerable<KeyValuePair<string, string>> cookies = null, TimeSpan? TimeOut = null)
        {
            var client = CLIENT;

            if (cookies != null)
            {
                var cookiesContainer = new CookieContainer();

                client = new(new HttpClientHandler() { CookieContainer = cookiesContainer });

                if (cookies.Any())
                {
                    foreach (var kv in cookies)
                        cookiesContainer.Add(
                            new Cookie(
                                kv.Key, 
                                kv.Value, 
                                "/", 
                                BASE_URL[(BASE_URL.IndexOf("//", StringComparison.InvariantCulture) + 2)..BASE_URL.LastIndexOf(":", StringComparison.InvariantCulture)]));
                }
            }

            if (TimeOut != null)
            {
                client = new();

                client.Timeout = TimeOut.GetValueOrDefault();
            }

            if (!url.StartsWith("http"))
                url = BASE_URL + url[(url.StartsWith("/") ? 1 : 0)..];

            var m = await client.GetAsync(url).ConfigureAwait(false);

            if (client != CLIENT)
                client.Dispose();

            if (m.IsSuccessStatusCode) // le fait de préciser <a> n'est pas inutile. cela permet de passer deux types différents comme <object> et un type anonyme en param
                return JsonConvert.DeserializeAnonymousType(await m.Content.ReadAsStringAsync(), type);

            return default;
        }

        public static bool SetTelegramLink(string username)
        {
            if (!string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(TELEGRAM_INVITE_LINK))
                return (TELEGRAM_INVITE_LINK = "https://t.me/" + username) != null;

            return false;
        }

        public static bool SetDiscordLink(ulong botId)
        {
            if (botId > 0 && string.IsNullOrWhiteSpace(DISCORD_INVITE_LINK))
                return (DISCORD_INVITE_LINK = $"https://discord.com/api/oauth2/authorize?client_id={botId}&permissions=274878008384&scope=bot") != null;

            return false;
        }

        public static MemberInfo GetPropertyMemberInfo<T>(Expression<Func<T, object>> expression) => Extensions.GetPropertyMemberInfo(expression);

        public static async Task CreateOrUpdateSearch(string username, string search, AsmsearchContext _database, SearchSource source = SearchSource.API)
        {
            var user = await _database.Users.FirstOrDefaultAsync(u => u.UserName == (username ?? GUEST));

            var r = await _database.Recherches.FirstOrDefaultAsync(r => r.User_ID == user.Id && r.recherche == search);

            if (r == null)
            {
                r = new()
                {
                    User_ID = user.Id,
                    recherche = search,
                    Nb_recherches = 1,
                    User = user,
                    Derniere_Recherche = DateTime.Now,
                    Source = source
                };

                await _database.Recherches.AddAsync(r);
            }
            else
            {
                r.Source = source;
                r.Derniere_Recherche = DateTime.Now;
                r.Nb_recherches++;

                _database.Recherches.Update(r);
            }

            await _database.SaveChangesAsync();
        }

        public static string GetHtmlType(object type) => type switch
        {
            double or long => "number",
            TimeSpan => "time",
            string s when MailAddress.TryCreate(s, out _) => "email",
            _ => "text"
        };
    }

    internal static class Extensions
    {
        public static MemberInfo GetPropertyMemberInfo<T>(this Expression<Func<T, object>> expression)
        {
            if (expression == null)
                return null;

            if (expression.Body is not MemberExpression memberExpression)
                memberExpression = ((UnaryExpression)expression.Body).Operand as MemberExpression;

            return memberExpression?.Member;
        }

        public static Type GetMemberUnderlyingType(this MemberInfo member)
        {
            return member.MemberType switch
            {
                MemberTypes.Field => ((FieldInfo)member).FieldType,
                MemberTypes.Property => ((PropertyInfo)member).PropertyType,
                MemberTypes.Event => ((EventInfo)member).EventHandlerType,
                _ => throw new ArgumentException("MemberInfo must be if type FieldInfo, PropertyInfo or EventInfo", nameof(member)),
            };
        }

        public static BinaryExpression CreateNullChecks(this Expression expression, bool skipFinalMember = false)
        {
            var parents = new Stack<BinaryExpression>();

            BinaryExpression newExpression = null;

            if (expression is UnaryExpression unary)
            {
                expression = unary.Operand;
            }

            var temp = expression as MemberExpression;

            while (temp is MemberExpression member)
            {
                try
                {
                    var nullCheck = Expression.NotEqual(temp, Expression.Constant(null));
                    parents.Push(nullCheck);
                }
                catch (InvalidOperationException) { }

                temp = member.Expression as MemberExpression;
            }

            while (parents.Count > 0)
            {
                if (skipFinalMember && parents.Count == 1 && newExpression != null)
                    break;

                newExpression = newExpression == null ? parents.Pop() : Expression.AndAlso(newExpression, parents.Pop());
            }

            return newExpression ?? Expression.Equal(Expression.Constant(true), Expression.Constant(true));
        }

        /// <summary>
        ///     Transforme un mot en upper camel case. <br/>
        ///     search => Search <br/>
        ///     SEARCH => Search <br/>
        ///     autre_string => Autre_string
        /// </summary>
        /// <param name="str">Un mot à convertir</param>
        /// <returns>le résultat ou string.Empty si param null ou vide</returns>
        public static string ToUpperCamelCase(this string str)
        {
            if (!string.IsNullOrWhiteSpace(str))
                return char.ToUpper(str[0]) + str[1..].ToLower();

            return string.Empty;
        }

        /// <summary>
        ///     Transforme une string en double. <br/>
        ///     Si cela échoue, c'est la valeur par défault qui est renvoyée.
        /// </summary>
        /// <param name="value">Une string qui représente un nombre.</param>
        /// <param name="defaultValue">Valeur renvoyer en cas d'echec. 0 par défault.</param>
        /// <returns></returns>
        public static double ToDouble(this string value, double defaultValue = 0.0)
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

        /// <summary>
        /// d => Days<br/>
        /// h => Hours<br/>
        /// m => Minutes<br/>
        /// s => Seconds<br/>
        /// f => Millisconds<br/>
        /// z => Ticks<br/>
        /// default => Days<br/>
        /// <br/>
        /// Exemple: 1d = 1 days, 1m = 1 minute
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public static TimeSpan ToTimeSpan(this string timeSpan)
        {
            var l = timeSpan.Length - 1;
            var value = timeSpan[..l];
            var type = timeSpan[l..];

            return type switch
            {
                "d" => TimeSpan.FromDays(value.ToDouble()),
                "h" => TimeSpan.FromHours(value.ToDouble()),
                "m" => TimeSpan.FromMinutes(value.ToDouble()),
                "s" => TimeSpan.FromSeconds(value.ToDouble()),
                "f" => TimeSpan.FromMilliseconds(value.ToDouble()),
                "z" => TimeSpan.FromTicks(long.Parse(value)),
                _   => TimeSpan.FromDays(value.ToDouble()),
            };
        }

        public static bool ContainsGenre(this string str, string genre)
        {
            var tmdbEq = Utilities.TMDB_TVMAZE_GENRES_EQ.GetValueOrDefault(str.ToLowerInvariant());

            return (tmdbEq ?? str).Contains(genre, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
