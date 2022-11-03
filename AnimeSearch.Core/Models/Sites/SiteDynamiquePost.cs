using System.Text.RegularExpressions;
using AnimeSearch.Core.Models.Search;
using HtmlAgilityPack;

namespace AnimeSearch.Core.Models.Sites;

public class SiteDynamiquePost : SearchPost
{
    private readonly string title;
    private readonly string urlIcon;
    private readonly string type;
    private readonly bool is_inter;

    private readonly string idBase;
    private readonly string cheminBaliseA;
    private readonly string cheminBaliseNbResult;

    public SiteDynamiquePost(string title, string url, string urlsearch, string urlIcon, string cheminBaliseA, Dictionary<string, string> postValues, string idbase = "", string typeSite = "", bool is_inter = false, HttpClient client = null, string cheminBaliseNbResult = null) :
        base(client, (url.EndsWith("/") ? url : url + "/") + ((urlsearch.StartsWith("/") ? urlsearch[1..] : urlsearch)), postValues)
    {
        this.title = title;
        this.urlIcon = urlIcon;
        type = typeSite;
        this.is_inter = is_inter;

        idBase = idbase;

        var cheminTmp = string.IsNullOrWhiteSpace(idbase) ? "//" : "";

        this.cheminBaliseA = cheminBaliseA.Aggregate(cheminTmp, (current, c) => current + (CharIsAuthorized(c) ? c : '/'));;
        
        if (!string.IsNullOrWhiteSpace(cheminBaliseNbResult))
        {
            cheminTmp = string.IsNullOrWhiteSpace(idbase) ? "//" : "";

            this.cheminBaliseNbResult = cheminBaliseNbResult.Aggregate(cheminTmp, (current, c) => current + (CharIsAuthorized(c) ? c : '/'));;
        }

        

        if (!this.urlIcon.StartsWith("http"))
        {
            if (this.urlIcon.StartsWith("/"))
                this.urlIcon = this.urlIcon[1..];

            this.urlIcon = Base_URL + this.urlIcon;
        }
    }

    /*public SiteDynamiquePost(Database.Sites s) : this(s.Title, s.Url, s.UrlSearch ?? "", s.UrlIcon, s.CheminBaliseA, s.PostValues, s.IdBase, s.TypeSite, s.Is_inter)
    {

    }*/

    public override int GetNbResult()
    {
        if (NbResult <= 0 && SearchResult != null)
        {
            if (!string.IsNullOrWhiteSpace(cheminBaliseNbResult))
            {
                var node = (string.IsNullOrWhiteSpace(idBase)? SearchHTMLResult.DocumentNode : SearchHTMLResult.GetElementbyId(idBase))?.SelectSingleNode(cheminBaliseNbResult);

                if(!string.IsNullOrWhiteSpace(node?.InnerText))
                    NbResult = int.TryParse(Regex.Match(node.InnerText.Replace(" ", ""), @"-?\d+").Value, out var i) ? i : 0;
            }

            if (NbResult <= 0)
            {
                var results = (string.IsNullOrWhiteSpace(idBase) ? SearchHTMLResult.DocumentNode : SearchHTMLResult.GetElementbyId(idBase))?.SelectNodes(cheminBaliseA);

                NbResult = results?.Count ?? 0;
            }
        }

        return NbResult;
    }

    public override string GetJavaScriptClickEvent()
    {
        if (GetNbResult() == 1)
        {
            HtmlNode results = (string.IsNullOrWhiteSpace(idBase) ? SearchHTMLResult.DocumentNode : SearchHTMLResult.GetElementbyId(idBase))?.SelectSingleNode(cheminBaliseA);

            if (results != null)
            {
                string href = results.Attributes["href"].Value;

                if (!href.StartsWith("http"))
                {
                    if (href.StartsWith("/"))
                        href = href[1..];

                    href = Base_URL + href;
                }

                return "window.open(\"" + href + "\");";
            }
        }

        string str = "var form = document.createElement(\"form\");" +
                     "form.setAttribute(\"method\", \"post\");" +
                     "form.setAttribute(\"action\", \"" + Base_URL + "\");" +
                     "form.setAttribute(\"target\", \"_blank\");" +
                     "var hiddenField = undefined;";

        foreach (string key in ListValueToPost.Keys)
        {
            string val = ListValueToPost.GetValueOrDefault(key);

            if (val == null)
                val = SearchStr;

            str += "hiddenField = document.createElement(\"input\");" +
                   "hiddenField.setAttribute(\"type\", \"hidden\");" +
                   "hiddenField.setAttribute(\"name\", '" + key + "');" +
                   "hiddenField.setAttribute(\"value\", \"" + val + "\");" +
                   "form.appendChild(hiddenField);";
        }

        return str + "document.body.appendChild(form);" +
               "form.submit(); " +
               "document.body.removeChild(form);";
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

                    KeyValuePair<string, Task<HttpResponseMessage>> result = tasks
                        .Where(t => t.Value?.Result != null)
                        .OrderByDescending(t => t.Value.Result.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult().Length)
                        .FirstOrDefault();

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
            return Task.FromResult<HttpResponseMessage>(null);
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