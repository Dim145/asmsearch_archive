using AnimeSearch.Core;
using AnimeSearch.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace AnimeSearch.Services.HangFire;

public class DonsTimeoutService: IHangFireService
{
    private AsmsearchContext Database { get; }

    public DonsTimeoutService(AsmsearchContext database)
    {
        Database = database;
    }
    
    public async Task Execute()
    {
        try
        {
            var dons = await Database.Dons.Where(d => !d.Done).ToArrayAsync();
            
            Database.Dons.RemoveRange(dons.Where(d => DateTime.Now.Subtract(d.Date) >= TimeSpan.FromMinutes(30) * 2));
            
            await Database.SaveChangesAsync();
        }
        catch(Exception e)
        {
            CoreUtils.AddExceptionError("Don Services", e);
        }
    }

    public string GetCron() => $"*/{30} * * * *";

    public string GetDescription() => "Vérirife que les dons sont bien validés au bout de 30 minutes, les supprime le cas échéant.";
}