using AnimeSearch.Data;
using Hangfire;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;

namespace AnimeSearch.Services.HangFire;

public class CheckEpUrlsService: IHangFireService
{
    private AsmsearchContext Database { get; }
    private HttpClient Client { get; }
    
    public CheckEpUrlsService(AsmsearchContext databse, HttpClient client)
    {
        Database = databse;
        Client = client;
    }
    
    public async Task Execute()
    {
        foreach (var episodesUrl in await Database.EpisodesUrls.ToListAsync())
        {
            var response = await Client.GetAsync(episodesUrl.Url);

            if (!response.IsSuccessStatusCode)
            {
                episodesUrl.Valid = false;
                continue;
            }

            var content = new HtmlDocument();
            content.Load(await response.Content.ReadAsStreamAsync());

            episodesUrl.Valid = new[]
            { 
                "//*[contains(@class, 'loading')] | //*[contains(@id, 'loading')]", 
                "//*[contains(@class, 'unlocker')] | //*[contains(@id, 'unlocker')]",
                "//iframe[not(contains(@src, 'googletagmanager.com'))]",
                "//video"
            }.Any(s => content.DocumentNode.SelectNodes(s)?.Any() ?? false);
        }

        await Database.SaveChangesAsync();
    }

    public string GetCron() => $"0 0 */{1} * *";

    public string GetDescription() => "Verifie si les liens donne bien une vidéos.";
}