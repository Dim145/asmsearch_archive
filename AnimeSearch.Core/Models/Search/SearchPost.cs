using System.Net;

namespace AnimeSearch.Core.Models.Search;

public abstract class SearchPost: Search
{
    protected string Base_URL { get; }

    protected Dictionary<string, string> ListValueToPost { get; }

    public SearchPost(string url, Dictionary<string, string> requiredFields) : base()
    {
        this.Base_URL = url;

        this.ListValueToPost = requiredFields;
    }
    
    public SearchPost(HttpClient client, string url, Dictionary<string, string> requiredFields) : base(client)
    {
        this.Base_URL = url;

        this.ListValueToPost = requiredFields;
    }

    public override async Task<HttpResponseMessage> SearchAsync(string search)
    {
        try
        {
            string keySearch = null;

            foreach (string key in this.ListValueToPost.Keys)
                if (this.ListValueToPost.GetValueOrDefault(key) == null)
                {
                    keySearch = key;
                    break;
                }

            List<KeyValuePair<string, string>> list = new();

            foreach (string key in this.ListValueToPost.Keys)
                if (this.ListValueToPost.GetValueOrDefault(key) != null)
                    list.Add(new KeyValuePair<string, string>(key, this.ListValueToPost.GetValueOrDefault(key)));

            list.Add(new KeyValuePair<string, string>(keySearch, search));

            HttpContent postContent = new FormUrlEncodedContent(list);

            HttpResponseMessage response = await Client.PostAsync(Base_URL, postContent);

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
                catch (WebException e) // to have stackTrace
                {
                    CoreUtils.AddExceptionError(new()
                    {
                        Date = DateTime.Now,
                        Zone = $"{GetSiteTitle()} ({Base_URL})",
                        Exception = e,
                        HtmlResponse = await response.Content.ReadAsStringAsync()
                    });
                }
            }
        }
        catch(Exception e)
        {
            CoreUtils.AddExceptionError(this.GetSiteTitle(), e);
        }

        return null;
    }

    public override string GetBaseURL()
    {
        return this.Base_URL;
    }
}