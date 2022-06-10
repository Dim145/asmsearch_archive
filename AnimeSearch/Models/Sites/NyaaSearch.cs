using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AnimeSearch.Models.Search;

namespace AnimeSearch.Models.Sites
{
    public class NyaaSearch: SearchGet
    {
        public static readonly string   TYPE = TypeEnum.ANIMES_DL_TORRENTS;

        public NyaaSearch(string search): this()
        {
            this.SearchAsync(search).Wait();
        }

        public NyaaSearch(): base("https://nyaa.si/", "?q=")
        {

        }

        public override Task<HttpResponseMessage> SearchAsync(string search)
        {
            return Task.Run(() =>
            {
                if (search == null) search = "";

                KeyValuePair<string, Task<HttpResponseMessage>>[] tasks = Utilities.LAGUAGE_ORDER.Select(lang => KeyValuePair.Create(search.EndsWith(lang) ? search : search + "+" + lang, base.SearchAsync(search.EndsWith(lang) ? search : search + "+" + lang))).ToArray();

                Task.WaitAll(tasks.Select(kv => kv.Value).ToArray());

                KeyValuePair<string, Task<HttpResponseMessage>> result = tasks.Where(t => t.Value?.Result != null).OrderByDescending(t => (t.Value.Result.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()).Length).FirstOrDefault();

                SearchResult = result.Value.Result?.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                SearchStr = result.Key;
                SearchHTMLResult.LoadHtml(SearchResult);

                return result.Value.Result; // lance 3 recherches et prend celle avec le plus de resulats
            });
        }

        public override int GetNbResult()
        {
            if( this.NbResult <= 0 && this.SearchResult != null )
            {
                IEnumerable<HtmlNode> infoNode = this.SearchHTMLResult.DocumentNode.Descendants().Where(n => n.GetAttributeValue("class", "").Equals("pagination-page-info"));

                string strTuUse = null;

                if( infoNode.Any() ) // plus rapide Si tous va bien car string BEAUCOUP moins longue
                {
                    HtmlNode div = infoNode.First();

                    strTuUse = div.InnerHtml;
                }
                else
                {
                    strTuUse = this.SearchResult;
                }

                int index = strTuUse.LastIndexOf(" out of ") + 8;

                if( index > 7 )
                {
                    string str = strTuUse[index..strTuUse.LastIndexOf(" results.<br>")];

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
            return Base_URL + "static/favicon.png";
        }

        public override string GetJavaScriptClickEvent()
        {
            if( this.GetNbResult() == 1 )
            {
                HtmlNodeCollection nodes = this.SearchHTMLResult.DocumentNode.SelectNodes("//div/table/tbody/tr/td[@colspan='2']/a");

                HtmlNode node = nodes?.LastOrDefault();

                if (node != null)
                    return "window.open(\"" + Base_URL + node.Attributes["href"].Value + "\");";
            }

            return "window.open(\""+ Search_URL + this.SearchStr + "\");";
        }

        public override string GetSiteTitle()
        {
            return "Nyaa.si";
        }

        public override string GetTypeSite() => TYPE;
    }
}
