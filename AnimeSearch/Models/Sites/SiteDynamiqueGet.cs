using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AnimeSearch.Models.Search;

namespace AnimeSearch.Models.Sites
{
    public class SiteDynamiqueGet : SearchGet
    {
        private readonly string title;
        private readonly string urlIcon;
        private readonly string type;
        private readonly bool is_inter;

        private readonly string idBase;
        private readonly string cheminBaliseA;

        /// <summary>
        ///     Permet d'exécuté une requête de type get sur n'importe quel site et de récupéré le nb de résultats.
        /// </summary>
        /// <param name="title">Titre du site affiché sur la page des résultats</param>
        /// <param name="url">URL de base du site (ex: "https://truck.com/" ) </param>
        /// <param name="urlsearch">Url de recherche avec les paramètres (ex: search.php?s=Search&amp;terme=) </param>
        /// <param name="urlIcon">Url complet de l'icon du site</param>
        /// <param name="cheminBaliseA">chemin des balises séparé par des espace ou des "/". Si l'attribut idbase est vide, alors depuis la balise body. sinon depuis la balise de l'id envoyer dans idbase</param>
        /// <param name="idbase">ID d'une balise de référence se trouvant dans la balise body</param>
        /// <param name="typeSite">Type du site (chaine libre) (la class <see cref="TypeEnum"/> est utilisable)</param>
        public SiteDynamiqueGet(string title, string url, string urlsearch, string urlIcon, string cheminBaliseA, string idbase = "", string typeSite = "", bool is_inter = false): 
            base(url.EndsWith("/") ? url : url + "/", urlsearch.StartsWith("/") ? urlsearch[1..] : urlsearch)
        {
            this.title = title;
            this.urlIcon = urlIcon;
            this.type = typeSite;
            this.is_inter = is_inter;

            this.idBase = idbase;

            string cheminTmp = string.IsNullOrWhiteSpace(idbase) ? "//" : "";
            foreach (char c in cheminBaliseA)
                cheminTmp += CharIsAuthorized(c) ? c : '/';

            this.cheminBaliseA = cheminTmp;

            if(!this.urlIcon.StartsWith("http"))
            {
                if (this.urlIcon.StartsWith("/"))
                    this.urlIcon = this.urlIcon[1..];

                this.urlIcon = this.Base_URL + this.urlIcon;
            }
        }

        public SiteDynamiqueGet(Database.Sites s): this(s.Title, s.Url, s.UrlSearch, s.UrlIcon, s.CheminBaliseA, s.IdBase, s.TypeSite, s.Is_inter)
        { 

        }

        public override int GetNbResult()
        {
            if(this.NbResult <= 0 && this.SearchResult != null)
            {
                HtmlNodeCollection results = (string.IsNullOrWhiteSpace(this.idBase)? this.SearchHTMLResult.DocumentNode : this.SearchHTMLResult.GetElementbyId(idBase))?.SelectNodes(cheminBaliseA);

                this.NbResult = results != null ? results.Count : 0;
            }

            return this.NbResult;
        }

        public override string GetJavaScriptClickEvent()
        {
            if(this.GetNbResult() == 1)
            {
                HtmlNode results = (string.IsNullOrWhiteSpace(this.idBase) ? this.SearchHTMLResult.DocumentNode : this.SearchHTMLResult.GetElementbyId(idBase))?.SelectSingleNode(cheminBaliseA);

                if(results != null)
                {
                    string href = results.Attributes["href"].Value;

                    if(!href.StartsWith("http"))
                    {
                        if (href.StartsWith("/"))
                            href = href[1..];

                        href = this.Base_URL + href;
                    }

                    return "window.open(\"" + href + "\");";
                }
            }

            return "window.open(\"" + this.Search_URL + this.SearchStr + "\");";
        }

        public override Task<HttpResponseMessage> SearchAsync(string search)
        {
            try
            {
                if (this.is_inter)
                {
                    return Task.Run(() =>
                    {
                        if (search == null) search = "";

                        KeyValuePair<string, Task<HttpResponseMessage>>[] tasks = Utilities.LAGUAGE_ORDER.Select(lang => KeyValuePair.Create(search.EndsWith(lang) ? search : search + " " + lang, base.SearchAsync(search.EndsWith(lang) ? search : search + " " + lang))).ToArray();

                        Task.WaitAll(tasks.Select(kv => kv.Value).ToArray());

                        KeyValuePair<string, Task<HttpResponseMessage>> result = tasks.Where(t => t.Value?.Result != null).OrderByDescending(t => t.Value.Result.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult().Length).FirstOrDefault();

                        SearchResult = result.Value.Result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        SearchStr = result.Key;
                        SearchHTMLResult.LoadHtml(SearchResult);

                        return result.Value.Result; // lance 3 recherches et prend celle avec le plus de resulats
                    });
                }

                return base.SearchAsync(search);
            }
            catch(Exception e)
            {
                Utilities.AddExceptionError(title, e);
                return Task.FromResult<HttpResponseMessage>(null); // null = error, faire cela evite d'avoir à géré cette exception
            }
        }

        public override string GetSiteTitle() => this.title;
        public override string GetUrlImageIcon() => this.urlIcon;

        public override string GetTypeSite() => this.type;

        private static bool CharIsAuthorized(char c)
        {
            return char.IsLetter(c) || char.IsNumber(c) || c == '@' || c == '[' || c == ']' || c == '\'' || c == '=' || c == '-' || c == ' ';
        }
    }
}
