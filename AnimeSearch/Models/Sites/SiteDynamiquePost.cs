using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnimeSearch.Models.Sites
{
    public class SiteDynamiquePost : SearchPost
    {
        private readonly string title;
        private readonly string urlIcon;
        private readonly string type;
        private readonly bool is_inter;

        private readonly string idBase;
        private readonly string cheminBaliseA;

        public SiteDynamiquePost(string title, string url, string urlsearch, string urlIcon, string cheminBaliseA, Dictionary<string, string> postValues, string idbase = "", string typeSite = "", bool is_inter = false) :
            base((url.EndsWith("/") ? url : url + "/") + ((urlsearch.StartsWith("/") ? urlsearch[1..] : urlsearch)), postValues)
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

            if (!this.urlIcon.StartsWith("http"))
            {
                if (this.urlIcon.StartsWith("/"))
                    this.urlIcon = this.urlIcon[1..];

                this.urlIcon = this.Base_URL + this.urlIcon;
            }
        }

        public SiteDynamiquePost(Database.Sites s) : this(s.Title, s.Url, s.UrlSearch ?? "", s.UrlIcon, s.CheminBaliseA, s.PostValues, s.IdBase, s.TypeSite, s.Is_inter)
        {

        }

        public override int GetNbResult()
        {
            if (this.NbResult <= 0 && this.SearchResult != null)
            {
                HtmlNodeCollection results = (string.IsNullOrWhiteSpace(this.idBase) ? this.SearchHTMLResult.DocumentNode : this.SearchHTMLResult.GetElementbyId(idBase))?.SelectNodes(cheminBaliseA);

                this.NbResult = results != null ? results.Count : 0;
            }

            return this.NbResult;
        }

        public override string GetJavaScriptClickEvent()
        {
            if (this.GetNbResult() == 1)
            {
                HtmlNode results = (string.IsNullOrWhiteSpace(this.idBase) ? this.SearchHTMLResult.DocumentNode : this.SearchHTMLResult.GetElementbyId(idBase))?.SelectSingleNode(cheminBaliseA);

                if (results != null)
                {
                    string href = results.Attributes["href"].Value;

                    if (!href.StartsWith("http"))
                    {
                        if (href.StartsWith("/"))
                            href = href[1..];

                        href = this.Base_URL + href;
                    }

                    return "window.open(\"" + href + "\");";
                }
            }

            string str = "var form = document.createElement(\"form\");" +
                    "form.setAttribute(\"method\", \"post\");" +
            "form.setAttribute(\"action\", \"" + Base_URL + "\");" +
            "form.setAttribute(\"target\", \"_blank\");" +
            "var hiddenField = undefined;";

            foreach (string key in this.ListValueToPost.Keys)
            {
                string val = this.ListValueToPost.GetValueOrDefault(key);

                if (val == null)
                    val = this.SearchStr;

                str += "hiddenField = document.createElement(\"input\");" +
                       "hiddenField.setAttribute(\"type\", \"hidden\");" +
                       "hiddenField.setAttribute(\"name\", '" + key + "');" +
                       "hiddenField.setAttribute(\"value\", \"" + val + "\");" +
                       "form.appendChild(hiddenField);";
            }

            return str + "document.body.appendChild(form);" +
                         "form.submit(); " +
                         "document.body.removeChild(form);";
        }

        public override Task<string> SearchAsync(string search)
        {
            try
            {
                if (this.is_inter)
                {
                    return Task.Run(() =>
                    {
                        if (search == null) search = "";

                        KeyValuePair<string, Task<string>>[] tasks = Utilities.LAGUAGE_ORDER.Select(lang => KeyValuePair.Create(search.EndsWith(lang) ? search : search + " " + lang, base.SearchAsync(search.EndsWith(lang) ? search : search + " " + lang))).ToArray();

                        Task.WaitAll(tasks.Select(kv => kv.Value).ToArray());

                        KeyValuePair<string, Task<string>> result = tasks.Where(t => t.Value != null && t.Value.Result != null).OrderByDescending(t => t.Value.Result.Length).FirstOrDefault();

                        SearchResult = result.Value.Result;
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
                return Task.Run(() => { string s = null; return s; });
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
