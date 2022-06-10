using Newtonsoft.Json;
using System;
using AnimeSearch.Models.Search;

namespace AnimeSearch.Models.Sites
{
    public class WikiPediaSearch : SearchGet
    {
        private Uri[] Urls;

        public WikiPediaSearch( string search ): this()
        {
           this.SearchAsync(search).Wait();
        }

        public WikiPediaSearch(): base("https://fr.wikipedia.org/", "w/api.php?action=opensearch&format=json&search=")
        {

        }

        public override int GetNbResult()
        {
            if( this.NbResult <= 0 && this.SearchResult != null )
            {
                var result = JsonConvert.DeserializeObject<object[]>(this.SearchResult);

                var obj = new
                {
                    search = (string) result[0],
                    names = ((Newtonsoft.Json.Linq.JArray)result[1]).ToObject<string[]>(),
                    truck = ((Newtonsoft.Json.Linq.JArray)result[2]).ToObject<string[]>(),
                    urls = ((Newtonsoft.Json.Linq.JArray)result[3]).ToObject<Uri[]>()
                };

                this.Urls = obj.urls;

                this.NbResult = this.Urls != null ? this.Urls.Length : 0;
            }

            return this.NbResult;
        }

        public override string GetSiteTitle() => "WikiPedia";
        public override string GetUrlImageIcon() => Base_URL + "static/images/project-logos/enwiki-2x.png";

        public override string GetJavaScriptClickEvent()
        {
            if( this.GetNbResult() >= 1 )
            {
                Uri url = null;

                foreach (Uri u in this.Urls)
                    if (u.ToString().Contains("série") || u.ToString().Contains("film"))
                        url = u;

                if (url == null)
                    url = Urls[0];

                return "window.open(\"" + url + "\");";
            }

            return "window.open(\""+Base_URL+"wiki/Erreur_HTTP_404\");";
        }
    }
}
