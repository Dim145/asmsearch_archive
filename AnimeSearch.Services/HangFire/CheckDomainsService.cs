using AnimeSearch.Core;
using AnimeSearch.Data;
using AnimeSearch.Data.Models;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace AnimeSearch.Services.HangFire;

public class CheckDomainsService: IHangFireService
{
    private HttpClient Client { get; }
    private AsmsearchContext Database { get; }

    public CheckDomainsService(HttpClient client, AsmsearchContext database)
    {
        Client = client;
        Database = database;
    }
    
    public async Task Execute()
    {
        foreach (var domain in await Database.Domains.ToListAsync())
        {
            try
            {
                var response = await Client.GetAsync(domain.Url);
                
                if(response.IsSuccessStatusCode)
                    domain.LastSeen = DateTime.Now;
            }
            catch (Exception e)
            {
                CoreUtils.AddExceptionError("Domain Hangfire service", e);
            }
        }

        if (Client.BaseAddress != null || !string.IsNullOrWhiteSpace(CoreUtils.BaseUrl))
        {
            var current = new Domains
            {
                Url = Client.BaseAddress ?? new Uri(CoreUtils.BaseUrl),
                Description = "Current server",
                LastSeen = DateTime.Now
            };
            
            if(!await Database.Domains.AnyAsync(d => d.Url == current.Url))
                await Database.AddAsync(current);
        }

        await Database.SaveChangesAsync();
    }

    public string GetCron() => Cron.Hourly();

    public string GetDescription() => "Vérifie régulièrement si les domaines sont accessibles.";
}