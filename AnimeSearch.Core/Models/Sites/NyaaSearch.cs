using AnimeSearch.Core.Models.Search;
using HtmlAgilityPack;

namespace AnimeSearch.Core.Models.Sites;

public class NyaaSearch: SearchGet
{
    public static readonly string TYPE = TypeEnum.ANIMES_DL_TORRENTS;

    public NyaaSearch(string search): this()
    {
        SearchAsync(search).Wait();
    }

    public NyaaSearch(): base("https://nyaa.si/", "?q=")
    {

    }

    public sealed override Task<HttpResponseMessage> SearchAsync(string search)
    {
        return Task.Run(() =>
        {
            search ??= string.Empty;

            var tasks = CoreUtils.LaguageOrder.Select(lang => KeyValuePair.Create(search.EndsWith(lang) ? search : search + "+" + lang, base.SearchAsync(search.EndsWith(lang) ? search : search + "+" + lang))).ToArray();

            Task.WhenAll(tasks.Select(kv => kv.Value)).Wait();

            var result = tasks.Where(t => t.Value?.Result != null).OrderByDescending(t => (t.Value.Result.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()).Length).FirstOrDefault();

            SearchStr = result.Key;
            SearchHTMLResult.LoadHtml(result.Value.Result.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            return result.Value.Result; // lance 3 recherches et prend celle avec le plus de resulats
        });
    }

    public override int GetNbResult()
    {
        if( NbResult <= 0 && SearchResult != null )
        {
            var infoNode = SearchHTMLResult.DocumentNode
                .Descendants()
                .Where(n => n.GetAttributeValue("class", "").Equals("pagination-page-info"))
                .ToArray();

            string strTuUse;

            if( infoNode.Any() ) // plus rapide Si tous va bien car string BEAUCOUP moins longue
            {
                var div = infoNode.First();

                strTuUse = div.InnerHtml;
            }
            else
            {
                strTuUse = SearchResult;
            }

            var index = strTuUse.LastIndexOf(" out of ", StringComparison.InvariantCulture) + 8;

            if( index > 7 )
            {
                var str = strTuUse[index..strTuUse.LastIndexOf(" results.<br>", StringComparison.InvariantCulture)];

                var isParsed = int.TryParse(str, out var result);

                NbResult = isParsed ? result : -1;
            }
            else
            {
                NbResult = 0;
            }
        }

        return NbResult;
    }

    public override string GetUrlImageIcon()
    {
        return Base_URL + "static/favicon.png";
    }

    public override string GetJavaScriptClickEvent()
    {
        if( GetNbResult() == 1 )
        {
            var nodes = SearchHTMLResult.DocumentNode.SelectNodes("//div/table/tbody/tr/td[@colspan='2']/a");

            var node = nodes?.LastOrDefault();

            if (node != null)
                return "window.open(\"" + Base_URL + node.Attributes["href"].Value + "\");";
        }

        return "window.open(\""+ Search_URL + SearchStr + "\");";
    }

    public override string GetSiteTitle()
    {
        return "Nyaa.si";
    }

    public override string GetTypeSite() => TYPE;
}