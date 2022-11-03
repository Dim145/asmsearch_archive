using AnimeSearch.Core;
using AnimeSearch.Core.Models.Api;
using HtmlAgilityPack;

namespace AnimeSearch.Services.Search;

public class DuckDuckGoSearch
{
    private HttpClient Client { get; }

    public DuckDuckGoSearch(HttpClient client)
    {
        Client = client;
    }
    
    public async Task<List<string>> SearchText(string search)
    {
        var listRes = new List<string>();
        Dictionary<string, string> listValue = new() {{"q", search}};

        var response = await Client.PostAsync("https://html.duckduckgo.com/html", new FormUrlEncodedContent(listValue));

        if(response.IsSuccessStatusCode)
        {
            HtmlDocument doc = new();

            var html = await response.Content.ReadAsStringAsync();
            doc.LoadHtml(html);

            var mainDiv = doc.GetElementbyId("links");

            var otherResults = mainDiv?.SelectNodes("div/div/h2/a");

            if (otherResults != null)
                listRes.AddRange(otherResults.Select(node => node.Attributes["href"].Value));
        }

        return listRes;
    }

    public async Task<string> GetBandeAnnonce(Result result)
    {
        var list = await SearchText($"{result.Name} bande annonce");
        
        var url = list.FirstOrDefault(url => url.Contains("youtube", StringComparison.InvariantCultureIgnoreCase) || url.Contains("allocine", StringComparison.InvariantCultureIgnoreCase));
        
        if (url != null && url.Contains("youtube"))
            url = url.Replace("watch", "embed").Replace("?v=", "/");

        return url;
    }

    public async Task<string> GetNautiljonLink(Result result)
    {
        var list = await SearchText($"{result.Name} nautiljon");

        return list.FirstOrDefault(url => url.Contains("nautiljon"));
    }
}