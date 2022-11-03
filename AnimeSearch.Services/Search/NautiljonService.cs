using System.Net.Http.Headers;
using AnimeSearch.Core.Extensions;
using AnimeSearch.Core.Models;
using AnimeSearch.Core.Models.Api;
using HtmlAgilityPack;

namespace AnimeSearch.Services.Search;

public class NautiljonService
{
    private const string BASE_URL = "https://www.nautiljon.com";
    
    private HttpClient Client { get; }
    private DuckDuckGoSearch DuckDuckGoSearch { get; }

    public NautiljonService(HttpClient client, DuckDuckGoSearch duckDuckGoSearch)
    {
        Client = client;
        DuckDuckGoSearch = duckDuckGoSearch;
        
        Client.DefaultRequestHeaders.UserAgent.Clear();
        Client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.159 Safari/537.36");
        Client.DefaultRequestHeaders.Add("sec-fetch-user", "?1");
        Client.DefaultRequestHeaders.Add("sec-fetch-mode", "navigate");
        Client.DefaultRequestHeaders.Add("sec-fetch-dest", "document");
        Client.DefaultRequestHeaders.Add("sec-fetch-user", "?1");
        Client.DefaultRequestHeaders.CacheControl = CacheControlHeaderValue.Parse("no-cache");
        Client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
    }

    public async Task<NautiljonResult> SearchOnNautiljon(string search, NautiljonFilter filter = NautiljonFilter.None)
    {
        var url = new Uri($"{BASE_URL}/{filter.ToUrlParams()}{search}");
        var response = await Client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var res = new NautiljonResult
            {
                HtmlDoc = new(),
                UriDoc = url
            };

            res.HtmlDoc.Load(await response.Content.ReadAsStreamAsync());

            var results = res.HtmlDoc.GetElementbyId("content").SelectNodes("div/table/tbody/tr/td/a[@class='sim']")?.ToArray() ?? Array.Empty<HtmlNode>();

            res.Urls = results.Select(r => new Uri($"{(r.Attributes["href"].Value.StartsWith("http") ? "" : BASE_URL + "/")}{r.Attributes["href"].Value}")).ToArray();
            res.PosterLink.AddRange(results.Select(r => new Uri($"{BASE_URL}/{r.Attributes["im"].Value}")));

            return res;
        }

        return null;
    }

    public async Task<Uri> SearchOnDuckDuckGo(Result result)
    {
        string res = null;
        
        foreach(var name in result.GetAllOtherNamesList())
            if(!string.IsNullOrWhiteSpace(res = await DuckDuckGoSearch.GetNautiljonLink(new(){Name = name})))
                break;

        if (!string.IsNullOrWhiteSpace(res))
            return new(res);

        return null;
    }

    public async Task<NautiljonResult> GetInfos(Uri nautiljonLink)
    {
        var response = await Client.GetAsync(nautiljonLink);

        if (response.IsSuccessStatusCode)
        {
            NautiljonResult res = new()
            {
                Urls = new[] {nautiljonLink},
                HtmlDoc = new()
            };
            
            res.HtmlDoc.Load(await response.Content.ReadAsStreamAsync());

            var poster = res.HtmlDoc.GetElementbyId("image_couverture")?.ParentNode?.Attributes?["href"]?.Value;
            
            if(!string.IsNullOrWhiteSpace(poster))
                res.PosterLink.Add(new($"{(poster.StartsWith("http") ? "" : BASE_URL + "/")}{poster}"));

            var bAs = res.HtmlDoc.GetElementbyId("bloc_ba")?.SelectNodes("div/div/div/a")?.ToArray() ?? Array.Empty<HtmlNode>();
            
            res.BandeAnnonces.AddRange(bAs.Select(b => new Uri(b.Attributes["href"].Value)));

            return res;
        }

        return null;
    }
}