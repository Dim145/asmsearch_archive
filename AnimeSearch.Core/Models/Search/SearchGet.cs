using System.Net;
using HtmlAgilityPack;

namespace AnimeSearch.Core.Models.Search;

public abstract class SearchGet : Search
{
    protected string Base_URL { get; }
    protected string Search_URL { get; }

    public SearchGet(string url, string searchLink): base()
    {
        this.Base_URL = url;
        this.Search_URL = (searchLink.StartsWith("http") ? "" : url) + searchLink;
    }
    
    public SearchGet(HttpClient client, string url, string searchLink): base(client)
    {
        this.Base_URL = url;
        this.Search_URL = (searchLink.StartsWith("http") ? "" : url) + searchLink;
    }

    public override string GetBaseURL()
    {
        return this.Base_URL;
    }

    public override string GetJavaScriptClickEvent()
    {
        return "window.open(\"" + this.Search_URL + this.SearchStr + "\");";
    }

    public override async Task<HttpResponseMessage> SearchAsync(string search)
    {
        try
        {
            HttpResponseMessage response = await Client.GetAsync(this.Search_URL + search);

            if (response.IsSuccessStatusCode)
            {
                this.SearchHTMLResult.Load(await response.Content.ReadAsStreamAsync());

                this.SearchStr = search;

                return response;
            }
            else
            {
                try
                {
                    throw new WebException($"{response.ReasonPhrase} ({(int) response.StatusCode})");
                }
                catch (WebException e) // to have stack trace
                {
                    CoreUtils.AddExceptionError(new()
                    {
                        Date = DateTime.Now,
                        Zone = GetSiteTitle(),
                        Exception = e,
                        HtmlResponse = await response.Content.ReadAsStringAsync()
                    });
                }
            }
        }
        catch(Exception e)
        {
            CoreUtils.AddExceptionError(GetSiteTitle(), e);
        }

        return null;
    }
}