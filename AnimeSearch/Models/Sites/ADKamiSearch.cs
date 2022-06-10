using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;

namespace AnimeSearch.Models.Sites
{
    public class ADKamiSearch : SearchGet
    {
        public static readonly string TYPE = TypeEnum.ANIME_SERIES_STREAM;

        public ADKamiSearch(string search) : this()
        {
            this.SearchResult = this.SearchAsync(search).Result;
        }

        public ADKamiSearch() : base("https://www.adkami.com/", "video?search=")
        {

        }

        public override string GetJavaScriptClickEvent()
        {
            if (this.GetNbResult() == 1)
            {
                HtmlNodeCollection nodes = this.SearchHTMLResult.DocumentNode.SelectNodes("//div/section/div/div/div/div[@class='video-item-list']/a");

                HtmlNode node = nodes?.LastOrDefault();

                if (node != null)
                    return "window.open(\"" + node.Attributes["href"].Value + "\");";
            }

            return "window.open(\"" + Search_URL + this.SearchStr + "\");";
        }

        public override int GetNbResult()
        {
            if (this.NbResult <= 0 && this.SearchResult != null)
            {
                IEnumerable<HtmlNode> infoNode = this.SearchHTMLResult.DocumentNode.Descendants().Where(n => n.GetAttributeValue("class", "").Equals("title"));

                string strTuUse = null;
                string strEnd   = null;

                if (infoNode.Any()) // plus rapide Si tous va bien car string BEAUCOUP moins longue
                {
                    HtmlNode div = infoNode.First();

                    strTuUse = div.InnerHtml;

                    strEnd = ")";
                }
                else
                {
                    strTuUse = this.SearchResult;
                    strEnd = ")</h1>";
                }

                int index = strTuUse.LastIndexOf("Rechercher (") + 12;

                if (index > 11)
                {
                    string str = strTuUse[index..strTuUse.LastIndexOf(strEnd)];

                    bool isParsed = int.TryParse(str, out int result);

                    this.NbResult = isParsed ? result : -1;
                }
                else
                {
                    this.NbResult = 0;
                }
            }

            return this.NbResult;
        }

        public override string GetUrlImageIcon()
        {
            return "https://adkami.com/image/logo.png";
        }

        public override string GetSiteTitle()
        {
            return "ADKami";
        }

        public override string GetTypeSite() => TYPE;
    }
}
