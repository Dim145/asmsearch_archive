using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace AnimeSearch.Models.Search
{
    public abstract class Search
    {
        protected readonly HttpClient client = new();

        protected int NbResult { get; set; }

        public string SearchResult { get; set; }
        protected HtmlDocument SearchHTMLResult { get; private set; }
        protected string SearchStr { get; set; }

        public Search()
        {
            this.NbResult     = -1;
            this.SearchResult = null;

            this.SearchHTMLResult = new();

            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.159 Safari/537.36");
            client.DefaultRequestHeaders.Add("sec-fetch-user", "?1");
            client.DefaultRequestHeaders.Add("sec-fetch-mode", "navigate");
            client.DefaultRequestHeaders.Add("sec-fetch-dest", "document");
            client.DefaultRequestHeaders.Add("sec-fetch-user", "?1");
            client.DefaultRequestHeaders.CacheControl = CacheControlHeaderValue.Parse("no-cache");
            client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
        }

        public abstract int GetNbResult();

        public abstract Task<HttpResponseMessage> SearchAsync(string search);

        public HtmlNode GetElementById(string id)
        {
            return this.SearchHTMLResult.GetElementbyId(id);
        }

        public void ResetHTMLElement()
        {
            this.SearchHTMLResult = new();
            this.SearchHTMLResult.OptionUseIdAttribute = true;
        }

        public virtual string GetTypeSite()
        {
            return "";
        }

        public abstract string GetUrlImageIcon();
        public abstract string GetBaseURL();
        public abstract string GetSiteTitle();

        public abstract string GetJavaScriptClickEvent();

        public override bool Equals(object obj)
        {
            if (!(obj is Search)) return false;

            if (obj == this)
                return true;

            Search s = (Search) obj;

            if (this.GetBaseURL() == null || s.GetBaseURL() == null)
                return false;

            return this.GetBaseURL() == s.GetBaseURL();
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }
}
