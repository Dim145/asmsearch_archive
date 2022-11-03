using System.Drawing;
using AnimeSearch.Core;
using AnimeSearch.Core.Models.Api;
using AnimeSearch.Core.ViewsModel;
using AnimeSearch.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;

namespace AnimeSearch.Data;

public static class DataUtils
{
    public static string Guest => "Guest";
    public static string SettingDonName => "Objectif Dons";
    public static string SettingPaypalMailName => "Mail Paypal";
    public static string SettingTokenTelegramName => "Token Télégram";
    public static string SettingTimeBeforeSsName => "Temps avant expiration des sauvegardes";
    public static string SettingAdsIdName => "Identifiant publicitaire";
    public static string SettingGoogleSearchIdName => "Identifiant 'Google search'";
    public static string SettingDiscordBotName => "Token Discord";
    public static string SettingOpennodeApikey => "API key OpenNode";
    public static string SettingHotJarId => "HotJar id";
    public static string SettingRecaptchaSecretKey => "Recaptcha secret key";
    public static string SettingRecaptchaSiteKey => "Recaptcha site key";
    
    public static string OpennodeApiUrl => "https://api.opennode.com/v1/charges";
    
    public static Roles AdminRole { get; } = new("Admin", 5) { Color = Color.DarkMagenta };
    public static Roles SuperAdminRole { get; } = new("Super-Admin", 6) { Color = Color.DodgerBlue };
    
    public static int DroitVueBas => 1;
    public static int DroitVueHaut => 2;
    public static int DroitModifBas => 3;
    public static int DroitModifHaut => 4;
    public static int DroitAdd => 5;
    public static int DroitDelete => 6;

    public static List<ApiObject> Apis { get; private set; } = new();
    public static List<Genre> AllGenres { get; } = new();
    
    public static void Initialize(IServiceProvider services)
    {
        var database = services.GetService<AsmsearchContext>();
        
        if(database is null)
            return;

        Apis = database.Apis
            .Include(a => a.Filters)
            .ThenInclude(f => f.ApiFilter)
            .Include(a => a.Sorts)
            .ThenInclude(s => s.ApiSort)
            .Include(a => a.Genres)
            .AsNoTracking()
            .ToArray()
            .Where(a => a.IsValid)
            .ToList();

        CoreUtils.TMDB_TVMAZE_GENRES_EQ.Add("animation", "anime");
        CoreUtils.TMDB_TVMAZE_GENRES_EQ.Add("science fiction", "science-fiction");

        Task.Run(async () =>
        {
            foreach (var api in Apis)
                api.Genres = await database.Genres.Where(g => g.ApiId == api.Id).ToListAsync();
        });
    }
    
    public static dynamic GetValueOrDefault(this DbSet<Setting> settings, string name)
    {
        return settings?.FirstOrDefault(s => s.Name == name)?.GetValueObject();
    }
    
    public static T GetValueOrDefault<T>(this DbSet<Setting> settings, string name)
    {
        var setting = settings?.FirstOrDefault(s => s.Name == name);

        return setting is null ? default : setting.GetValueObject<T>();
    }

    public static IQueryable<Genre> GetByApi(this DbSet<Genre> genres, int idApi)
    {
        return genres.Where(g => g.ApiId == idApi);
    }
    
    public static IQueryable<Genre> GetByApi(this DbSet<Genre> genres, ApiObject api)
    {
        return api is null ? genres : genres.Where(g => g.ApiId == api.Id);
    }
    
    public static IQueryable<EpisodesUrls> ForResult(this IQueryable<EpisodesUrls> urls, int idApi, int searchId)
    {
        return urls.Where(u => u.ApiId == idApi && u.SearchId == searchId);
    }
    
    public static IQueryable<EpisodesUrls> ForResult(this IQueryable<EpisodesUrls> urls, Result result)
    {
        return urls.ForResult(result.IdApiFrom, result.Id);
    }
    
    public static IQueryable<EpisodesUrls> BySeason(this IQueryable<EpisodesUrls> urls, int season)
    {
        return urls.Where(u => u.SeasonNumber == season);
    }
    
    public static IQueryable<EpisodesUrls> ByEp(this IQueryable<EpisodesUrls> urls, int episode)
    {
        return urls.Where(u => u.EpisodeNumber == episode);
    }
    
    public static IQueryable<EpisodesUrls> By(this IQueryable<EpisodesUrls> urls, int apiId, int searchId, int seasonId = -1, int episodeId = -1)
    {
        var tmp = urls;

        if (apiId > -1 && searchId > -1)
            tmp = tmp.ForResult(apiId, searchId);

        if (seasonId > -1)
            tmp = tmp.BySeason(seasonId);

        if (episodeId > -1)
            tmp = tmp.ByEp(episodeId);
        
        return tmp;
    }
}