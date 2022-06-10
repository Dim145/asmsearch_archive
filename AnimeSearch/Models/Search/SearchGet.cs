using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AnimeSearch.Models.Search
{
    public abstract class SearchGet : Search
    {
        protected string Base_URL { get; }
        protected string Search_URL { get; }

        public SearchGet(string url, string searchLink): base()
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
                HttpResponseMessage response = await client.GetAsync(this.Search_URL + search);

                if (response.IsSuccessStatusCode)
                {
                    string htmlString = await response.Content.ReadAsStringAsync();

                    this.SearchHTMLResult.LoadHtml(htmlString);
                    this.SearchResult = htmlString;

                    this.SearchStr = search;

                    return response;
                }
                else
                {
                    Utilities.Errors.Add(this.GetSiteTitle() + ": " + DateTime.Now + "\n" + await response.Content.ReadAsStringAsync());
                }
            }
            catch(Exception e)
            {
                Utilities.AddExceptionError(this.GetSiteTitle(), e);
            }

            return null;
        }
    }
}
