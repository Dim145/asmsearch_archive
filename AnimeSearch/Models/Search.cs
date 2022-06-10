using HtmlAgilityPack;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace AnimeSearch.Models
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

            /*
                :authority: www.adkami.com
                :method: GET
                :path: /
                :scheme: https
                accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng
                            *; q = 0.8,application / signed - exchange; v = b3; q = 0.9
                accept - encoding: gzip, deflate, br
                accept - language: fr - FR,fr; q = 0.9,en - US; q = 0.8,en; q = 0.7
                cache - control: no - cache
                cookie: cf_chl_prog = a9; cf_clearance = N5TKBYJCAN5.yVbXXdBQH411TJbqdns6KZSWOdbdTyc - 1630445225 - 0 - 150; PHPSESSID = 4c2bfd49d1c8272b330522b7404ebdca; __cf_bm = 65a87cc5b6b3395054d85f2210ff5ff07cbbd3f3 - 1630445228 - 1800 - AUhDlUscpEWfbBSeiTENQcOUQjmLV / jHFXwNj / AULGE1Ib8IkZd7Hjp29XTvCj27g40mP9zt / V1GXo3xOVqaBp0 + 00zve / E + LJdbBuxLfYrMUPPWZ0SMaP1URDGvHDgedQ ==
                                            pragma: no - cache
                referer: https://www.adkami.com/?__cf_chl_jschl_tk__=pmd_XmHa2SOv0lLSw.9rrhMz8ULU5KWP0nZj5oVNFGH9A9Q-1630445222-0-gqNtZGzNAhCjcnBszQZR
                            sec - fetch - dest: document
                sec - fetch - mode: navigate
                sec - fetch - site: same - origin
                sec - fetch - user: ?1
                sec - gpc: 1
                upgrade - insecure - requests: 1
                user - agent: Mozilla / 5.0(Windows NT 10.0; Win64; x64) AppleWebKit / 537.36(KHTML, like Gecko) Chrome / 92.0.4515.159 Safari / 537.36

                   */
        }

        public abstract int GetNbResult();

        public abstract Task<string> SearchAsync(string search);

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
