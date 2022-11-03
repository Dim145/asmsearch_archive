using AnimeSearch.Core.Models.Search;
using HtmlAgilityPack;

namespace AnimeSearch.Core.Models.Sites;

public class ADKamiSearch : SearchGet
{
    public static readonly string TYPE = TypeEnum.ANIME_SERIES_STREAM;

    public ADKamiSearch(string search) : this()
    {
        SearchAsync(search).Wait();
    }

    public ADKamiSearch() : base("https://www.adkami.com/", "video?search=")
    {

    }

    public override string GetJavaScriptClickEvent()
    {
        if (GetNbResult() == 1)
        {
            var nodes = SearchHTMLResult.DocumentNode.SelectNodes("//div/section/div/div/div/div[@class='video-item-list']/a");

            var node = nodes?.LastOrDefault();

            if (node != null)
                return "window.open(\"" + node.Attributes["href"].Value + "\");";
        }

        return "window.open(\"" + Search_URL + SearchStr + "\");";
    }

    public override int GetNbResult()
    {
        if (NbResult <= 0 && SearchResult != null)
        {
            var infoNode = SearchHTMLResult.DocumentNode
                .Descendants()
                .Where(n => n.GetAttributeValue("class", "").Equals("title"))
                .ToArray();

            string strTuUse;
            string strEnd;

            if (infoNode.Any()) // plus rapide Si tous va bien car string BEAUCOUP moins longue
            {
                var div = infoNode.First();

                strTuUse = div.InnerHtml;

                strEnd = ")";
            }
            else
            {
                strTuUse = SearchResult;
                strEnd = ")</h1>";
            }

            int index = strTuUse.LastIndexOf("Rechercher (", StringComparison.InvariantCulture) + 12;

            if (index > 11)
            {
                var str = strTuUse[index..strTuUse.LastIndexOf(strEnd, StringComparison.InvariantCulture)];

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

    public override string GetUrlImageIcon() => "https://adkami.com/image/logo.png";

    public override string GetSiteTitle() => "ADKami";

    public override string GetTypeSite() => TYPE;
}