using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AnimeSearch.Models.Search;

namespace AnimeSearch.Models.Sites
{
    public class NautiljonSearch : SearchGet
    {
        public static readonly int FILTER_NONE  = -1;
        public static readonly int FILTER_ANIME = 0;
        public static readonly int FILTER_FILM  = 1;

        private string javascript;
        private string bande_annonce;

        public NautiljonSearch(string search, int type): this(type)
        {
            this.SearchAsync(search).Wait();
        }

        public NautiljonSearch(): this(FILTER_NONE)
        {

        }

        public NautiljonSearch(int type): base("https://www.nautiljon.com/", 
            type == FILTER_ANIME ? "animes/?formats_include%5B%5D=1&encourss_exclude%5B%5D=6&encourss_exclude%5B%5D=7&q=" : // filtre animes
            type == FILTER_FILM ? "animes/?formats_include%5B%5D=3&formats_include%5B%5D=8&q=" : // filtre films
            "animes/?q=") // pas de filtres
        {
            this.javascript = null;
        }

        public override int GetNbResult()
        {
            if( this.NbResult <= 0 && this.SearchResult != null )
            {
                IEnumerable<HtmlNode> infoNode = this.SearchHTMLResult.GetElementbyId("content")?.SelectNodes("h2");

                if (infoNode != null && infoNode.Any())
                {
                    HtmlNode h2 = infoNode.First();

                    string html = h2.InnerText;

                    int index = html.LastIndexOf("\" (") + 3;
                    int length = html.LastIndexOf(" r") - index;

                    bool isParsed = int.TryParse(html.Substring(index, length < 0 ? 0 : length), out int res);

                    this.NbResult = isParsed ? res : -1;
                }
            }

            return this.NbResult;
        }

        public override string GetSiteTitle() => "Nautijon";
        public override string GetUrlImageIcon() => "https://www.nautiljon.com/static/images/logo.png";

        public override string GetTypeSite() => "Informatif";

        public override string GetJavaScriptClickEvent()
        {
            if (this.GetNbResult() == 1 && string.IsNullOrWhiteSpace(this.javascript))
            {
                HtmlNodeCollection nodes = this.SearchHTMLResult.GetElementbyId("content").SelectNodes("div/table/tbody/tr/td/a");

                HtmlNode node = nodes?.LastOrDefault();

                if (node != null)
                    this.javascript = "window.open(\"" + Base_URL + node.Attributes["href"].Value + "\");";
                else
                    this.javascript = "window.open(\"" + Search_URL + this.SearchStr + "\");";
            }

            return this.javascript == null ? "window.open(\"" + Search_URL + this.SearchStr + "\");" : javascript;
        }

        public override Task<HttpResponseMessage> SearchAsync(string search)
        {
            if(search.Contains(':'))
            {
                int index = search.IndexOf(':');

                if (search[index - 1] != ' ')
                    search = search.Insert(index - 1, " ");
            }

            return base.SearchAsync(search);
        }

        public async Task<string> GetBandeAnnonceVideoURL()
        {
            if(this.GetNbResult() == 1 && this.bande_annonce == null)
            {
                HtmlDocument pageDoc = new();
                pageDoc.OptionUseIdAttribute = true;

                HttpResponseMessage response = await client.GetAsync( this.GetJavaScriptClickEvent()[(this.GetJavaScriptClickEvent().IndexOf("\"")+1)..this.GetJavaScriptClickEvent().LastIndexOf("\"")] );

                if(response.IsSuccessStatusCode)
                {
                    pageDoc.LoadHtml(await response.Content.ReadAsStringAsync());

                    HtmlNode aNode = pageDoc.GetElementbyId("bloc_ba")?.SelectSingleNode("div/div/div/a");

                    if (aNode != null)
                        bande_annonce = aNode.Attributes["href"].Value?.Replace("autoplay=1", "autoplay=0");
                }
            }

            return bande_annonce;
        }

        public async Task<string> GetImageResultAsync()
        {
            if( SearchResult != null && this.GetNbResult() == 1 )
            {
                string link = this.GetJavaScriptClickEvent();

                int index = link.IndexOf("\"") + 1;
                link = link[index..link.LastIndexOf("\"")];

                HttpResponseMessage response = await client.GetAsync(link);

                if (response.IsSuccessStatusCode)
                {
                    string htmlString = await response.Content.ReadAsStringAsync();

                    this.ResetHTMLElement();
                    this.SearchHTMLResult.LoadHtml(htmlString);
                    this.SearchResult = htmlString;

                    HtmlNode node = this.SearchHTMLResult.GetElementbyId("image_couverture")?.ParentNode;

                    if( node != null )
                        return Base_URL + node.Attributes["href"].Value;
                }
            }
            
            return "";
        }
    }
}
