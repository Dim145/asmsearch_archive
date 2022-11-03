using AnimeSearch.Core.Models.Search;
using HtmlAgilityPack;

namespace AnimeSearch.Core.Models.Sites;

public class VostFreeSearch : SearchPost
{
    public static readonly string TYPE = TypeEnum.ANIMES_DL_STREAM;

    public VostFreeSearch(string search) : this()
    {
        this.SearchAsync(search).Wait();
    }

    public VostFreeSearch() : base("https://vostfree.tv/", new())
    {
        this.ListValueToPost.Add("do", "search");
        this.ListValueToPost.Add("subaction", "search");
        this.ListValueToPost.Add("story", null);
    }

    // TODO: Touver une meilleure facon de faire quand même
    public override string GetJavaScriptClickEvent()
    {
        if (this.GetNbResult() == 1)
        {
            HtmlNodeCollection nodes = this.SearchHTMLResult.DocumentNode.SelectNodes("//div/div/div/div/div/div/div/div[@class='title']/a");

            HtmlNode node = nodes?.First();

            if (node != null)
                return "window.open(\"" + node.Attributes["href"].Value + "\");";
        }

        return "var form = document.createElement(\"form\");" +
               "form.setAttribute(\"method\", \"post\");" +
               "form.setAttribute(\"action\", \"" + Base_URL + "\");" +
               "form.setAttribute(\"target\", \"_blank\");" +

               "var hiddenField = document.createElement(\"input\");" +
               "hiddenField.setAttribute(\"type\", \"hidden\");" +
               "hiddenField.setAttribute(\"name\", 'do');" +
               "hiddenField.setAttribute(\"value\", \"search\");" +
               "form.appendChild(hiddenField);" +

               "hiddenField = document.createElement(\"input\");" +
               "hiddenField.setAttribute(\"type\", \"hidden\");" +
               "hiddenField.setAttribute(\"name\", 'subaction');" +
               "hiddenField.setAttribute(\"value\", \"search\");" +
               "form.appendChild(hiddenField);" +

               "hiddenField = document.createElement(\"input\");" +
               "hiddenField.setAttribute(\"type\", \"hidden\");" +
               "hiddenField.setAttribute(\"name\", 'story');" +
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
            IEnumerable<HtmlNode> infoNode = this.SearchHTMLResult.DocumentNode.Descendants().Where(n => n.GetAttributeValue("class", "").Equals("berrors"));

            if (infoNode.Any())
            {
                HtmlNode div = infoNode.First();

                string html = div.InnerText;

                if (!html.Contains("Malheureusement"))
                {
                    int index = html.IndexOf("Trouv&eacute; ") + 14;
                    int length = html.IndexOf(" r&eacute") - index;

                    bool isParsed = int.TryParse(html.Substring(index, length < 0 ? 0 : length), out int res);

                    this.NbResult = isParsed ? res : -1;
                }
                else
                {
                    this.NbResult = 0;
                }
            }
        }

        return this.NbResult;
    }

    public override string GetUrlImageIcon()
    {
        return "https://vostfree.com/templates/Animix/images/logo.png";
    }

    public override string GetSiteTitle()
    {
        return "VostFree";
    }

    public override string GetTypeSite()
    {
        return TYPE;
    }
}