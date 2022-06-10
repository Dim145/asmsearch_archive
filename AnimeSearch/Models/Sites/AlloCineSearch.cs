using AnimeSearch.Models.Search;
using HtmlAgilityPack;

namespace AnimeSearch.Models.Sites
{
    public class AlloCineSearch : SearchGet
    {
        public AlloCineSearch(): base("https://www.allocine.fr/", "rechercher/?q=")
        {

        }

        public AlloCineSearch(string search): this()
        {
            this.SearchAsync(search).Wait();
        }

        public override int GetNbResult()
        {
            if( this.NbResult <= 0 && this.SearchResult != null)
            {
                HtmlNodeCollection nodes = this.SearchHTMLResult.GetElementbyId("content-layout").SelectNodes("div/div/section/ul/li/div/figure");

                this.NbResult = nodes != null ? nodes.Count : 0;
            }

            return this.NbResult;
        }

        public override string GetSiteTitle() => "Allo-Cine";
        public override string GetUrlImageIcon() => "https://assets.allocine.fr/skin/img/allocine/logo-main-12195cd9d9.svg";

        public override string GetTypeSite() => "Informatif";
    }
}
