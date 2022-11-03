using AnimeSearch.Core.ViewsModel;
using AnimeSearch.Data;
using AnimeSearch.Data.Models;
using FluentEmail.Core;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace AnimeSearch.Services.HangFire;

public class GenreMajService: IHangFireService
{
    private AsmsearchContext Database { get; }

    public GenreMajService(AsmsearchContext database)
    {
        Database = database;
    }
    
    public async Task Execute()
    {
        var apis = await Database.Apis.ToListAsync();
        
        foreach (var api in apis.Where(api => api.IsValid))
        {
            var tmp = await api.WaitGenres();

            var genres = tmp.DistinctBy(g => new {g.Id, IdApi = g.ApiId}).ToList();
            
            tmp.Where(g => !genres.Contains(g)).ForEach(g =>
            {
                var genreTmp = genres.FirstOrDefault(genre => genre.ApiId == g.ApiId && genre.Id == g.Id);

                if (genreTmp != null)
                    genreTmp.Type = SearchType.All;
            });
            
            var index = DataUtils.Apis.FindIndex(a => a.Id == api.Id);
            
            if(index > -1 && index < DataUtils.Apis.Count)
                DataUtils.Apis[index].Genres = genres;

            foreach (var genre in genres)
            {
                var g = await Database.Genres.FirstOrDefaultAsync(g => g.ApiId == api.Id && g.Id == genre.Id);

                if (g != null)
                {
                    g.Name = genre.Name;
                    g.Type = genre.Type;
                    
                    Database.Genres.Update(g);
                }
                else
                    await Database.Genres.AddAsync(genre);
            }
        }

        await Database.SaveChangesAsync();
    }

    public string GetCron() => $"{0} {0} * * *";

    public string GetDescription() => "Récupère les genres sur les différentes api et les sauvegarde en bdd.";
}