using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace AnimeSearch.Models
{
    public abstract class SearchPost: Search
    {
        protected string Base_URL { get; }

        protected Dictionary<string, string> ListValueToPost { get; }

        public SearchPost(string url, Dictionary<string, string> requiredFields) : base()
        {
            this.Base_URL = url;

            this.ListValueToPost = requiredFields;
        }

        public override async Task<string> SearchAsync(string search)
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

                HttpResponseMessage response = await client.PostAsync(Base_URL, postContent);

                if (response.IsSuccessStatusCode)
                {
                    string html = await response.Content.ReadAsStringAsync();

                    this.SearchHTMLResult.LoadHtml(html);
                    this.SearchResult = html;

                    this.SearchStr = search;

                    return html;
                }
                else
                {
                    Utilities.Errors.Add(this.GetSiteTitle() + "(" + Base_URL + ")" + ": " + DateTime.Now + "\n" + await response.Content.ReadAsStringAsync());
                }
            }
            catch(Exception e)
            {
                Utilities.AddExceptionError(this.GetSiteTitle(), e);
            }

            return null;
        }

        public override string GetBaseURL()
        {
            return this.Base_URL;
        }
    }
}
