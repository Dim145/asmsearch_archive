using AnimeSearch.Core.Models.Search;
using HtmlAgilityPack;

namespace AnimeSearch.Core.Models.Sites;

public class AnimeUltimeSearch : SearchPost
{
    public static readonly string TYPE = TypeEnum.ANIMES_DL_STREAM;

    public AnimeUltimeSearch(): base("http://www.anime-ultime.net/search-0-1", new())
    {
        this.ListValueToPost.Add("search", null);
    }

    public override string GetJavaScriptClickEvent()
    {
        if (this.GetNbResult() == 1)
        {
            HtmlNodeCollection nodes = this.SearchHTMLResult.GetElementbyId("main").SelectNodes("div/table/tbody/tr/td/a");

            HtmlNode node = nodes?.First();

            if (node != null)
                return "window.open(\"" + this.GetBaseURL() + "/" + node.Attributes["href"].Value + "\");";
        }

        return "var form = document.createElement(\"form\");" +
               "form.setAttribute(\"method\", \"post\");" +
               "form.setAttribute(\"action\", \"" + Base_URL + "\");" +
               "form.setAttribute(\"target\", \"_blank\");" +

               "var hiddenField = document.createElement(\"input\");" +
               "hiddenField.setAttribute(\"type\", \"text\");" +
               "hiddenField.setAttribute(\"name\", 'search');" +
               "hiddenField.setAttribute(\"value\", \"" + this.SearchStr + "\");" +
               "form.appendChild(hiddenField);" +
               "document.body.appendChild(form);" +
               "form.submit(); " +
               "document.body.removeChild(form);";
    }

    public override int GetNbResult()
    {
        if (this.NbResult <= 0 && this.SearchResult != null)
        {
            IEnumerable<HtmlNode> infoNode = this.SearchHTMLResult.GetElementbyId("main").SelectNodes("h1");

            if( infoNode != null && infoNode.Any() )
            {
                this.NbResult = 1;
                return this.NbResult;
            }

            infoNode = this.SearchHTMLResult.GetElementbyId("main").SelectNodes("div/div");

            string strTuUse = null;

            if (infoNode != null && infoNode.Any()) // plus rapide Si tous va bien car string BEAUCOUP moins longue
            {
                HtmlNode div = infoNode.First();

                strTuUse = div.InnerHtml;
            }
            else
            {
                strTuUse = this.SearchResult;
            }

            int index = strTuUse.LastIndexOf("anime (") + 7;

            if (index > 6)
            {
                string str = strTuUse[index..strTuUse.LastIndexOf(" trouv")];

                bool isParsed = int.TryParse(str, out int result);

                this.NbResult = isParsed ? result : -1;
            }
            else
            {
                this.NbResult = 0;
            }
        }

        return this.NbResult;
    }

    public override string GetSiteTitle()
    {
        return "Anime Ultime";
    }

    public override string GetUrlImageIcon()
    {
        return "webfonts/anime-utime-logo.png";
    }

    public override string GetBaseURL()
    {
        return base.GetBaseURL()[0..base.GetBaseURL().LastIndexOf("/")];
    }

    public override string GetTypeSite() => TYPE;
}