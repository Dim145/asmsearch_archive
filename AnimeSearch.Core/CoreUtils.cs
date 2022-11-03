using System.Drawing;
using System.Net;
using AnimeSearch.Core.Models;
using AnimeSearch.Core.Models.Api;
using Newtonsoft.Json;

namespace AnimeSearch.Core;

public static class CoreUtils
{
    public static List<Type> AllSearchSite { get; } = AppDomain.CurrentDomain.GetAssemblies().SelectMany(t => t.GetTypes())
        .Where(t => t.IsClass && !t.Name.Contains("<>") && t.Namespace == "AnimeSearch.Core.Models.Sites" && t.GetField("TYPE") != null).ToList();
    
    public static List<Errors> Errors { get; } = new();
    private static HttpClient Client { get; } = new();
    public static ColorConverter ColorConverter { get; } = new();
    public static Dictionary<string, string> TMDB_TVMAZE_GENRES_EQ { get; } = new();
    
    public static string[] LaguageOrder { get; } = { "vostfr", "vf", "multi" };

    public static string BaseUrl { get; set; }

    public static void AddExceptionError(string zone, Exception e, string userName = "")
    {
        AddExceptionError(new()
        {
            Zone = zone,
            Exception = e,
            Date = DateTime.Now,
            UserName = userName
        });
    }

    public static void AddExceptionError(Errors error)
    {
        Errors.Add(error);
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
    public static async Task<T> GetAndDeserialiseFromUrl<T>(string url, Func<string, string> opOnBody = null, IEnumerable<KeyValuePair<string, string>> cookies = null, TimeSpan? TimeOut = null, HttpClient client = null)
    {
        client ??= Client;

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
                            BaseUrl[(BaseUrl.IndexOf("//", StringComparison.InvariantCulture) + 2)..BaseUrl.LastIndexOf(":", StringComparison.InvariantCulture)]));
            }
        }

        if (TimeOut != null)
        {
            client = new();

            client.Timeout = TimeOut.GetValueOrDefault();
        }

        if (!url.StartsWith("http"))
            url = BaseUrl + url[(url.StartsWith("/")?1:0)..];

        var m = await client.GetAsync(url).ConfigureAwait(false);

        if (client != Client)
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
    public static async Task<a> GetAndDeserialiseAnonymousFromUrl<a>(string url, a type, IEnumerable<KeyValuePair<string, string>> cookies = null, TimeSpan? TimeOut = null, HttpClient client = null)
    {
        client ??= Client;

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
                            BaseUrl[(BaseUrl.IndexOf("//", StringComparison.InvariantCulture) + 2)..BaseUrl.LastIndexOf(":", StringComparison.InvariantCulture)]));
            }
        }

        if (TimeOut != null)
        {
            client = new();

            client.Timeout = TimeOut.GetValueOrDefault();
        }

        if (!url.StartsWith("http"))
            url = BaseUrl + url[(url.StartsWith("/") ? 1 : 0)..];

        var m = await client.GetAsync(url).ConfigureAwait(false);

        if (client != Client)
            client.Dispose();

        if (m.IsSuccessStatusCode) // le fait de préciser <a> n'est pas inutile. cela permet de passer deux types différents comme <object> et un type anonyme en param
            return JsonConvert.DeserializeAnonymousType(await m.Content.ReadAsStringAsync(), type);

        return default;
    }
}