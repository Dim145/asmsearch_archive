using System.Net.Http.Headers;
using HtmlAgilityPack;

namespace AnimeSearch.Core.Models.Search;

public abstract class Search
{
    protected HttpClient Client { get; }

    protected int NbResult { get; set; }

    public string SearchResult => SearchHTMLResult?.Text;
    protected HtmlDocument SearchHTMLResult { get; private set; }
    protected string SearchStr { get; set; }

    protected Search(): this(new())
    {
        
    }

    protected Search(HttpClient client)
    {
        this.NbResult     = -1;

        this.SearchHTMLResult = new();
        
        Client = client ?? new();
        Client.DefaultRequestHeaders.UserAgent.Clear();
        Client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.159 Safari/537.36");
        Client.DefaultRequestHeaders.Add("sec-fetch-user", "?1");
        Client.DefaultRequestHeaders.Add("sec-fetch-mode", "navigate");
        Client.DefaultRequestHeaders.Add("sec-fetch-dest", "document");
        Client.DefaultRequestHeaders.Add("sec-fetch-user", "?1");
        Client.DefaultRequestHeaders.CacheControl = CacheControlHeaderValue.Parse("no-cache");
        Client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
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