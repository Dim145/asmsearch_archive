using AnimeSearch.Models.Search;
using HtmlAgilityPack;

namespace AnimeSearch.Models.Sites
{
    public class YggTorrent : SearchGet
    {
        public static readonly string TYPE = TypeEnum.FILM_ANIME_SERIES_DL;

        public YggTorrent(): base("https://www3.yggtorrent.re/", "engine/search?do=search&category=2145&name=")
        {
        }

        public override int GetNbResult()
        {
            if(this.NbResult <= 0 && this.SearchResult != null)
            {
                HtmlNode node = this.SearchHTMLResult.GetElementbyId("#torrents")?.SelectSingleNode("h2/font");

                if( node != null )
                {
                    string nb = node.InnerText.Substring(0, node.InnerText.IndexOf(" r"))?.Replace(" ", "");

                    if(!string.IsNullOrWhiteSpace(nb))
                        this.NbResult = int.Parse(nb);
                }
                else
                {
                    HtmlNodeCollection nodes = this.SearchHTMLResult.DocumentNode.SelectNodes("//div/main/div/div/section/div/table/tbody/tr/td/a[@id='torrent_name']");

                    this.NbResult = nodes != null ? nodes.Count : 0; // à améliorer
                }
            }

            return this.NbResult;
        }

        public override string GetJavaScriptClickEvent()
        {
            if(this.GetNbResult() == 1)
            {
                HtmlNode node = this.SearchHTMLResult.DocumentNode.SelectSingleNode("//div/main/div/div/section/div/table/tbody/tr/td/a[@id='torrent_name']");

                if (node != null)
                    return "window.open(\"" + node.Attributes["href"].Value + "\");";
            }

            return base.GetJavaScriptClickEvent();
        }

        public override string GetSiteTitle() => "YggTorrent";
        public override string GetUrlImageIcon() => Base_URL + "assets/img/logo.svg";
        public override string GetTypeSite() => TYPE;
    }
}
