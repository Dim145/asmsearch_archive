using System.Text.RegularExpressions;
using AnimeSearch.Core.Models.Search;
using HtmlAgilityPack;

namespace AnimeSearch.Core.Models.Sites;

public class SiteDynamiqueGet : SearchGet
{
    private readonly string title;
    private readonly string urlIcon;
    private readonly string type;
    private readonly bool is_inter;

    private readonly string idBase;
    private readonly string cheminBaliseA;
    private readonly string cheminBaliseNbResult;

    /// <summary>
    ///     Permet d'exécuté une requête de type get sur n'importe quel site et de récupéré le nb de résultats.
    /// </summary>
    /// <param name="title">Titre du site affiché sur la page des résultats</param>
    /// <param name="url">URL de base du site (ex: "https://truck.com/" ) </param>
    /// <param name="urlsearch">Url de recherche avec les paramètres (ex: search.php?s=Search&amp;terme=) </param>
    /// <param name="urlIcon">Url complet de l'icon du site</param>
    /// <param name="cheminBaliseA">chemin des balises séparé par des espace ou des "/". Si l'attribut idbase est vide, alors depuis la balise body. sinon depuis la balise de l'id envoyer dans idbase</param>
    /// <param name="idbase">ID d'une balise de référence se trouvant dans la balise body</param>
    /// <param name="typeSite">Type du site (chaine libre) (la class <see cref="TypeEnum"/> est utilisable)</param>
    public SiteDynamiqueGet(string title, string url, string urlsearch, string urlIcon, string cheminBaliseA, string idbase = "", string typeSite = "", bool is_inter = false, HttpClient client = null, string cheminBaliseNbResult = null): 
        base(client, url.EndsWith("/") ? url : url + "/", urlsearch.StartsWith("/") ? urlsearch[1..] : urlsearch)
    {
        this.title = title;
        this.urlIcon = urlIcon;
        type = typeSite;
        this.is_inter = is_inter;

        idBase = idbase;

        var cheminTmp = string.IsNullOrWhiteSpace(idbase) ? "//" : "";
        cheminTmp = cheminBaliseA.Aggregate(cheminTmp, (current, c) => current + (CharIsAuthorized(c) ? c : '/'));

        this.cheminBaliseA = cheminTmp;

        if (!string.IsNullOrWhiteSpace(cheminBaliseNbResult))
        {
            cheminTmp = string.IsNullOrWhiteSpace(idbase) ? "//" : "";
            cheminTmp = cheminBaliseNbResult.Aggregate(cheminTmp, (current, c) => current + (CharIsAuthorized(c) ? c : '/'));

            this.cheminBaliseNbResult = cheminTmp;
        }

        if(!this.urlIcon.StartsWith("http"))
        {
            if (this.urlIcon.StartsWith("/"))
                this.urlIcon = this.urlIcon[1..];

            this.urlIcon = Base_URL + this.urlIcon;
        }
    }
    
    public override int GetNbResult()
    {
        if(NbResult <= 0 && SearchResult != null)
        {
            if (!string.IsNullOrWhiteSpace(cheminBaliseNbResult))
            {
                var node = (string.IsNullOrWhiteSpace(idBase)? SearchHTMLResult.DocumentNode : SearchHTMLResult.GetElementbyId(idBase))?.SelectSingleNode(cheminBaliseNbResult);

                if(!string.IsNullOrWhiteSpace(node?.InnerText))
                    NbResult = int.TryParse(Regex.Match(node.InnerText.Replace(" ", ""), @"-?\d+").Value, out var i) ? i : 0;
            }
            
            if(NbResult <= 0)
            {
                var results = (string.IsNullOrWhiteSpace(idBase)? SearchHTMLResult.DocumentNode : SearchHTMLResult.GetElementbyId(idBase))?.SelectNodes(cheminBaliseA);

                NbResult = results?.Count ?? 0;
            }
        }

        return NbResult;
    }

    public override string GetJavaScriptClickEvent()
    {
        if(GetNbResult() == 1)
        {
            HtmlNode results = (string.IsNullOrWhiteSpace(idBase) ? SearchHTMLResult.DocumentNode : SearchHTMLResult.GetElementbyId(idBase))?.SelectSingleNode(cheminBaliseA);

            if(results != null)
            {
                string href = results.Attributes["href"].Value;

                if(!href.StartsWith("http"))
                {
                    if (href.StartsWith("/"))
                        href = href[1..];

                    href = Base_URL + href;
                }

                return "window.open(\"" + href + "\");";
            }
        }

        return "window.open(\"" + Search_URL + SearchStr + "\");";
    }

    public override Task<HttpResponseMessage> SearchAsync(string search)
    {
        try
        {
            if (is_inter)
            {
                return Task.Run(() =>
                {
                    if (search == null) search = "";

                    KeyValuePair<string, Task<HttpResponseMessage>>[] tasks = CoreUtils.LaguageOrder.Select(lang => KeyValuePair.Create(search.EndsWith(lang) ? search : search + " " + lang, base.SearchAsync(search.EndsWith(lang) ? search : search + " " + lang))).ToArray();

                    Task.WaitAll(tasks.Select(kv => kv.Value).ToArray());

                    KeyValuePair<string, Task<HttpResponseMessage>> result = tasks.Where(t => t.Value?.Result != null).OrderByDescending(t => t.Value.Result.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult().Length).FirstOrDefault();

                    SearchStr = result.Key;
                    SearchHTMLResult.LoadHtml(result.Value.Result.Content.ReadAsStringAsync().GetAwaiter().GetResult());

                    return result.Value.Result; // lance 3 recherches et prend celle avec le plus de resulats
                });
            }

            return base.SearchAsync(search);
        }
        catch(Exception e)
        {
            CoreUtils.AddExceptionError(title, e);
            return Task.FromResult<HttpResponseMessage>(null); // null = error, faire cela evite d'avoir à géré cette exception
        }
    }

    public override string GetSiteTitle() => title;
    public override string GetUrlImageIcon() => urlIcon;

    public override string GetTypeSite() => type;

    private static bool CharIsAuthorized(char c)
    {
        return char.IsLetter(c) || char.IsNumber(c) || c is '@' or '[' or ']' or '\'' or '=' or '-' or ' ';
    }
}