using AnimeSearch.Core;
using AnimeSearch.Data;
using AnimeSearch.Data.Models;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace AnimeSearch.Services.HangFire;

public class CitationService: IHangFireService
{
    private AsmsearchContext Database { get; }

    public CitationService(AsmsearchContext database)
    {
        Database = database;
    }

    public async Task Execute()
    {
        try
        {
            var current   = await Database.Citations.FirstOrDefaultAsync(c => c.IsCurrent);
            var citations = await Database.Citations.Where(c => c.IsValidated && !c.IsCurrent).ToArrayAsync();

            if (current != null)
                current.IsCurrent = false;

            if(citations.Any())
                citations[ServiceUtils.RANDOM.Next(citations.Length)].IsCurrent = true;

            await Database.SaveChangesAsync();
        }
        catch (Exception e)
        {
            CoreUtils.AddExceptionError("CitationsService:", e);
            throw;
        }
    }

    public string GetCron() => Cron.Daily();

    public string GetDescription() => "Sélectionne tous les jours une nouvelle citation aléatoirement.";
}