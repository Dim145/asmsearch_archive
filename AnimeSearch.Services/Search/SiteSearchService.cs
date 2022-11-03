using System.Globalization;
using AnimeSearch.Core;
using AnimeSearch.Core.Models;
using AnimeSearch.Core.Models.Api;
using AnimeSearch.Core.Models.Search;
using AnimeSearch.Core.Models.Sites;
using AnimeSearch.Core.ViewsModel;
using AnimeSearch.Data;
using AnimeSearch.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AnimeSearch.Services.Search;

public class SiteSearchService
{
    private ApiService ApiService { get; }
    private AsmsearchContext Database { get; }
    private DuckDuckGoSearch DuckDuckGo { get; }
    private NautiljonService NautiljonService { get; }

    public SiteSearchService(ApiService apiService, AsmsearchContext database, DuckDuckGoSearch duckGo, NautiljonService nautiljonService)
    {
        ApiService = apiService;
        Database = database;
        DuckDuckGo = duckGo;
        NautiljonService = nautiljonService;
    }
    
    public async Task<string[]> GetInfosAndNba(Result searchResult, IRequestCookieCollection rCookies)
    {
        var tab = new string[2];

        if (searchResult.IsAnime || searchResult.IsFilmAnimation())
        {
            var taskMulti = NautiljonService.SearchOnNautiljon(searchResult.Name, searchResult.IsFilmAnimation() ? NautiljonFilter.FilmAnimation : searchResult.IsAnime ? NautiljonFilter.Anime : NautiljonFilter.None);
            var taskSingle = NautiljonService.SearchOnDuckDuckGo(searchResult);

            await Task.WhenAll(taskMulti, taskSingle);

            Uri uri = null;
            if ((taskMulti.Result?.Urls?.Length ?? 0) > 0)
            {
                if (taskMulti.Result.Urls.Length > 1)
                {
                    tab[0] = taskMulti.Result.UriDoc.ToString();
                }
                else
                {
                    uri = taskMulti.Result.Urls.FirstOrDefault();
                }
            }
            else
            {
                uri = taskSingle.Result;
            }
            
            if(uri != null)
            {
                var datas = await NautiljonService.GetInfos(uri);

                if (searchResult.Image == null && datas.PosterLink.Any())
                    searchResult.Image = datas.PosterLink.FirstOrDefault();

                tab[0] = datas.Urls.FirstOrDefault()?.ToString();
                tab[1] = datas.BandeAnnonces.FirstOrDefault()?.ToString();
            }
        }

        if (string.IsNullOrWhiteSpace(tab[0]))
        {
            AlloCineSearch allo = new();

            if (searchResult.IsFilm)
                await allo.SearchAsync(searchResult.Name);

            string javascript;
            if (allo.GetNbResult() <= 0)
            {
                WikiPediaSearch wiki = new();
                await wiki.SearchAsync(searchResult.Name);
                wiki.GetNbResult();

                javascript = wiki.GetJavaScriptClickEvent();
            }
            else
            {
                javascript = allo.GetJavaScriptClickEvent();
            }
            
            int start = javascript.IndexOf("\"", StringComparison.InvariantCulture) + 1;
            tab[0] = javascript[start..javascript.LastIndexOf("\"", StringComparison.InvariantCulture)];
        }

        return tab;
    }
    
    public async Task<ModelAPI> Search(Result searchResult, IRequestCookieCollection cookiesCollection, List<Sites> databaseSites = null, bool searchInfosAndBA = true, Action<Core.Models.Search.Search> callBack = null)
    {
        var result = new ModelAPI
        {
            Search = searchResult?.Name
        };

        if (searchResult == null)
            return result;

        string cookies = "";
        bool isPresent = cookiesCollection != null && cookiesCollection.TryGetValue("languageOrder", out cookies);

        List<CultureInfo> languageOrder = new();

        if (isPresent)
        {
            string[] languages = cookies.Split("|");

            foreach (string s in languages)
                if (s != "")
                    languageOrder.Add(new(s));
        }

        var infos = searchInfosAndBA ? GetInfosAndNba(searchResult, cookiesCollection) : null;

        var bandeAnnonce = searchInfosAndBA && string.IsNullOrWhiteSpace(result.Bande_Annone) ? DuckDuckGo.GetBandeAnnonce(searchResult) : null;

        List<Core.Models.Search.Search> listSiteToSearch = new();

        if (databaseSites != null)
        {
            listSiteToSearch.AddRange(databaseSites
                .Where(s => IsTypeSiteForResult(searchResult, s.TypeSite)) // filtre les types
                .Select(s => s.ToDynamicSite((s.PostValues?.Count ?? 0) > 0))); // creer les sites
        }

        listSiteToSearch.AddRange(CoreUtils.AllSearchSite
            .Where(t => IsTypeSiteForResult(searchResult, t.GetField("TYPE")?.GetValue(null)?.ToString())) // filtre les type pour garder les bons
            .Select(t => t.GetConstructor(Array.Empty<Type>())?.Invoke(null) as Core.Models.Search.Search) // apelle les constructeurs et l'ajoute a la liste
            .Where(s => s != null && !listSiteToSearch.Contains(s))); // verifie qu'il n'y as pas de doublons

        if (!isPresent)
            await Task.WhenAll(listSiteToSearch.Select(item =>
            {
                var task = item.SearchAsync(searchResult.Name);

                if (task != null && callBack != null)
                    task.ContinueWith(t => callBack(item));

                return task;
            }).ToArray());

        List<Task> tabTask = new();
        foreach (var s in listSiteToSearch)
        {
            if (s.GetNbResult() == 0) tabTask.Add(Task.Run(async () =>
            {
                foreach (string otherName in searchResult.GetAllOtherNamesList(languageOrder).Where(n => !string.IsNullOrWhiteSpace(n)))
                {
                    if (!isPresent && otherName == searchResult.Name) continue;

                    Task t = s.SearchAsync(otherName);

                    if (t != null)
                        await t.ContinueWith(_ => { callBack?.Invoke(s); });

                    if (s.GetNbResult() > 0)
                        break;
                }
            }));
        }

        await Task.WhenAll(tabTask.ToArray());

        foreach (var s in listSiteToSearch)
            if (s.GetNbResult() > -1)
            {
                string url = null;

                if (s is SearchGet)
                {
                    var javascript = s.GetJavaScriptClickEvent();
                    int start = javascript.IndexOf("\"", StringComparison.InvariantCulture) + 1;

                    url = javascript[start..javascript.LastIndexOf("\"", StringComparison.InvariantCulture)];
                }

                ModelSearchResult model = new()
                {
                    NbResults = s.GetNbResult(),
                    SiteUrl = s.GetBaseURL(),
                    IconUrl = s.GetUrlImageIcon(),
                    Url = url,
                    OpenJavaScript = s.GetJavaScriptClickEvent(),
                    Type = s.GetTypeSite()
                };

                result.SearchResults.Add(s.GetSiteTitle(), model);
            }

        if(searchInfosAndBA)
        {
            var infosRes = await infos;

            if (infosRes is {Length: 2})
            {
                result.InfoLink = infosRes[0];
                result.Bande_Annone = infosRes[1];
            }

            if (bandeAnnonce != null && string.IsNullOrWhiteSpace(result.Bande_Annone))
                result.Bande_Annone = await bandeAnnonce;
        }

        result.Result = searchResult;

        return result;
    }
    
    public async Task<ModelAPI> Search(string search, IRequestCookieCollection r, List<Sites> databaseSites = null, bool searchInfosAndBa = true, Action<Core.Models.Search.Search> callBack = null)
    {
        if (string.IsNullOrEmpty(search))
            return null;

        return await Search(await ApiService.SearchResult(search), r, databaseSites, searchInfosAndBa, callBack);
    }

    public async Task<ModelAPI> OptiSearch(string search, IRequestCookieCollection r, List<Sites> databaseSites = null, bool searchInfosAndBa = true, Action<Core.Models.Search.Search> callBack = null)
    {
        var result = await ApiService.SearchResult(search);

        return await OptiSearch(result, r, databaseSites, searchInfosAndBa, callBack);
    }
    
    public async Task<ModelAPI> OptiSearch(Result result, IRequestCookieCollection r, List<Sites> databaseSites = null, bool searchInfosAndBa = true, Action<Core.Models.Search.Search> callBack = null)
    {
        var taskSearch      = Task.Run(() => Search(result, r, databaseSites, searchInfosAndBa, callBack));
        var taskSavedSearch = Task.Run(async () =>
        {
            SavedSearch savedSearch = await Database.SavedSearch
                .OrderByDescending(ss => ss.DateSauvegarde)
                .FirstOrDefaultAsync(ss => ss.Search == result.Name);

            TimeSpan tempsExpiration = (await Database.Settings.FirstOrDefaultAsync(s => s.Name == DataUtils.SettingTimeBeforeSsName))?.GetValueObject();

            if(savedSearch != null && DateTime.Now - savedSearch.DateSauvegarde <= tempsExpiration)
                return savedSearch.Results;

            return null;
        });


        Task<ModelAPI>[] tasks = {taskSearch, taskSavedSearch};

        var index = Task.WaitAny(tasks);

        var model = tasks[index].Result ?? await tasks[1 - index];

        return model;
    }
    
    

    public async Task<ModelAPI> Search(int id, int idApi, IRequestCookieCollection r, ResultType type = ResultType.All, List<Sites> databaseSites = null, bool executeSiteSearch = true, Action<Core.Models.Search.Search> callBack = null)
    {
        return id < 0 ? null : await Search(await ApiService.SearchResult(id, type, idApi), r, databaseSites, executeSiteSearch, callBack);
    }
    
    private static bool IsTypeSiteForResult(Result result, string typeSite)
    {
        if (string.IsNullOrWhiteSpace(typeSite))
            return false;

        return typeSite.Contains("all") ||                     // condition 1, c'est un site qui contient de tous
               result.IsAnime && typeSite.Contains("animes") || // condition 2, c'est un site d'anime pour un anime
               result.IsSerie && typeSite.Contains("séries") || // condition 3, c'est un site de serie pour une serie
               result.IsFilm  && typeSite.Contains("film")   || // condition 4, c'est un site de film pour un film
               result.IsHentai        && typeSite.Contains("hentai") || // condition 5 Hentai
               result.IsFilmAnimation() && typeSite.Contains("FA");      // condition 6 film d'animation 
    }
}