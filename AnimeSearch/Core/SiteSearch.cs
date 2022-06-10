using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AnimeSearch.Database;
using AnimeSearch.Models;
using AnimeSearch.Models.Results;
using AnimeSearch.Models.Search;
using AnimeSearch.Models.Sites;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using static AnimeSearch.Core.ApiSearch;

namespace AnimeSearch.Core;

public sealed class SiteSearch
{
    /// <summary>
    ///     Récupères les informations de la recherche depuis:<br/>
    ///     - nautiljon (+ bande annonce dans ce cas)<br/>
    ///     - wikipédia <br/>
    ///     - Allo ciné
    /// </summary>
    /// <param name="searchResult"></param>
    /// <param name="rCookies"></param>
    /// <returns>un tableau de taille 2 qui contient: l'url d'infos, le lien de bande annonce</returns>
    public static Task<string[]> GetInfosAndNba(Result searchResult, IRequestCookieCollection rCookies) => Task.Run(async () =>
    {
        NautiljonSearch nautiljon = new(searchResult is TheMovieDbResult r && r.IsHentai() ? NautiljonSearch.FILTER_NONE : searchResult.IsAnime() ? NautiljonSearch.FILTER_ANIME : searchResult.IsFilm() ? NautiljonSearch.FILTER_FILM : NautiljonSearch.FILTER_NONE);
        string javascript = "";
        string[] tab = new string[2];

        if (searchResult.IsAnime())
        {
            await nautiljon.SearchAsync(searchResult.GetName());

            string cookies = "";
            bool isPresent = rCookies != null && rCookies.TryGetValue("languageOrder", out cookies);

            List<CultureInfo> languageOrder = new();

            if (isPresent)
            {
                string[] languages = cookies.Split("|");

                foreach (string s in languages)
                    if (s != "")
                        languageOrder.Add(new(s));
            }

            if (nautiljon.GetNbResult() == 0)
            {
                foreach (string otherName in searchResult.GetAllOtherNamesList(languageOrder))
                {
                    if (!isPresent && otherName == searchResult.GetName()) continue;

                    await nautiljon.SearchAsync(otherName);

                    if (nautiljon.GetNbResult() > 0)
                        break;
                }
            }

            if (nautiljon.GetNbResult() != 0 && searchResult.GetImage() == null)
            {
                bool succes = Uri.TryCreate(await nautiljon.GetImageResultAsync(), UriKind.RelativeOrAbsolute, out Uri u);

                if (succes) searchResult.SetImage(u);
            }
        }

        if (nautiljon.GetNbResult() <= 0)
        {
            AlloCineSearch allo = new();

            if (searchResult is TheMovieDbResult)
                await allo.SearchAsync(searchResult.GetName());

            if (allo.GetNbResult() <= 0)
            {
                WikiPediaSearch wiki = new();
                await wiki.SearchAsync(searchResult.GetName());
                wiki.GetNbResult();

                javascript = wiki.GetJavaScriptClickEvent();
            }
            else
            {
                javascript = allo.GetJavaScriptClickEvent();
            }
        }
        else
        {
            javascript = nautiljon.GetJavaScriptClickEvent();
            tab[1] = await nautiljon.GetBandeAnnonceVideoURL();
        }

        int start = javascript.IndexOf("\"") + 1;
        tab[0] = javascript[start..javascript.LastIndexOf("\"")];

        return tab;
    });

    public static ModelAPI Search(Result searchResult, IRequestCookieCollection cookiesCollection, List<Sites> databaseSites = null, bool searchInfosAndBA = true, Action<Search> callBack = null)
    {
        var result = new ModelAPI()
        {
            Search = searchResult?.GetName()
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

        var bandeAnnonce = searchInfosAndBA && string.IsNullOrWhiteSpace(result.Bande_Annone) ? GetBandAnnonce(searchResult) : null;

        List<Search> listSiteToSearch = new();

        if (databaseSites != null)
        {
            listSiteToSearch.AddRange(databaseSites
                .Where(s => IsTypeSiteForResult(searchResult, s.TypeSite)) // filtre les types
                .Select(s => (Search)(s.PostValues == null || s.PostValues.Count == 0 ? new SiteDynamiqueGet(s) : new SiteDynamiquePost(s)))); // creer les sites
        }

        listSiteToSearch.AddRange(Utilities.AllSearchSite
            .Where(t => IsTypeSiteForResult(searchResult, t.GetField("TYPE")?.GetValue(null)?.ToString())) // filtre les type pour garder les bons
            .Select(t => (Search)t.GetConstructor(Array.Empty<Type>()).Invoke(null)) // apelle les constructeurs et l'ajoute a la liste
            .Where(s => !listSiteToSearch.Contains(s))); // verifie qu'il n'y as pas de doublons

        if (!isPresent)
            Task.WaitAll(listSiteToSearch.Select(item =>
            {
                var task = item.SearchAsync(searchResult.GetName());

                if (task != null && callBack != null)
                    task.ContinueWith(t => callBack(item));

                return task;
            }).ToArray());

        List<Task> tabTask = new();
        foreach (Search s in listSiteToSearch)
        {
            if (s.GetNbResult() == 0) tabTask.Add(Task.Run(async () =>
            {
                foreach (string otherName in searchResult.GetAllOtherNamesList(languageOrder))
                {
                    if (!isPresent && otherName == searchResult.GetName()) continue;

                    Task t = s.SearchAsync(otherName);

                    if (t != null)
                        await t.ContinueWith(task => { callBack?.Invoke(s); });

                    if (s.GetNbResult() > 0)
                        break;
                }
            }));
        }

        Task.WaitAll(tabTask.ToArray());

        foreach (Search s in listSiteToSearch)
            if (s.GetNbResult() > -1)
            {
                string url = null;

                if (s is SearchGet)
                {
                    string javascript = s.GetJavaScriptClickEvent();
                    int start = javascript.IndexOf("\"") + 1;

                    url = javascript[start..javascript.LastIndexOf("\"")];
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
            string[] infosRes = infos.GetAwaiter().GetResult();

            if (infosRes != null && infosRes.Length == 2)
            {
                result.InfoLink = infosRes[0];
                result.Bande_Annone = infosRes[1];
            }

            if (bandeAnnonce != null && string.IsNullOrWhiteSpace(result.Bande_Annone))
                result.Bande_Annone = bandeAnnonce.GetAwaiter().GetResult();
        }

        result.Result = searchResult;

        return result;
    }

    public static ModelAPI Search(string search, IRequestCookieCollection r, List<Sites> databaseSites = null, bool searchInfosAndBA = true, Action<Search> callBack = null)
    {
        if (string.IsNullOrEmpty(search))
            return null;

        return Search(SearchResult(search), r, databaseSites, searchInfosAndBA, callBack);
    }

    public static ModelAPI OptiSearch(string search, AsmsearchContext database, IRequestCookieCollection r, List<Sites> databaseSites = null, bool searchInfosAndBA = true, Action<Search> callBack = null)
    {
        Result result = SearchResult(search);

        var taskSearch      = Task.Run(() => Search(result, r, databaseSites, searchInfosAndBA, callBack));
        var taskSavedSearch = Task.Run(async () =>
        {
            SavedSearch savedSearch = await database.SavedSearch
            .OrderByDescending(ss => ss.DateSauvegarde)
            .FirstOrDefaultAsync(ss => ss.Search == result.GetName());

            TimeSpan tempsExpiration = (await database.Settings.FirstOrDefaultAsync(s => s.Name == Utilities.SETTING_TIME_BEFORE_SS_NAME))?.GetValueObject();

            if(savedSearch != null && DateTime.Now - savedSearch.DateSauvegarde <= tempsExpiration)
                return savedSearch.Results;

            return null;
        });

        ModelAPI model = null;

        taskSearch.ContinueWith(t => model = t.Result);
        taskSavedSearch.ContinueWith(t => model = t.Result);

        while (model == null)
            Task.Delay(100);

        return model;
    }

    public static Result SearchResult(string search)
    {
        bool serieFirst = true;

        if (search.Contains("=>"))
        {
            int index = search.IndexOf("=>");

            string before = search[..index];
            search = search[(index + 2)..];

            serieFirst = before != "movie";
        }

        Task<TheMovieDbResult> taskMovie = GetFilm(search);
        Task<TvMazeResult> taskSerie = GetSerie(search);

        Task.WaitAll(taskMovie, taskSerie);

        Result serie = taskSerie.Result;
        Result movie = taskMovie.Result;

        Result searchResult = serieFirst ? serie ?? movie : movie ?? serie;

        if (searchResult == null) // ne devrais jamais arriver
        {
            searchResult = new();
            searchResult.SetName("introuvable");
        }
        else if (searchResult.GetImage() == null && serie != null)
        {
            searchResult.SetImage(((TvMazeResult)serie).Image?.GetValueOrDefault("original"));
        }

        return searchResult;
    }

    public static Result SearchResult(int id, int type = -1)
    {
        Result res;

        if (type == -1) // au cas ou mais pas tres fiable
        {
            Task<TheMovieDbResult> taskMovie = GetFilm(id, false);
            Task<TvMazeResult> taskSerie = GetSerie(id);

            Task.WaitAll(taskMovie, taskSerie);

            Result serie = taskSerie.Result;
            Result movie = taskMovie.Result;

            res = serie ?? movie;
        }
        else
        {
            res = type == 0 ? GetSerie(id).GetAwaiter().GetResult() : GetFilm(id, type == 2).GetAwaiter().GetResult();
        }

        return res;
    }

    public static ModelAPI Search(int id, IRequestCookieCollection r, int type = -1, List<Sites> databaseSites = null, bool executeSiteSearch = true, Action<Search> callBack = null)
    {
        if (id < 0 || type < -1)
            return null;

        return Search(SearchResult(id, type), r, databaseSites, executeSiteSearch, callBack);
    }

    public static ModelMultiSearch[] MultiSearch(string search)
    {
        Task<List<TvMazeResult>>     taskListSeries = GetSeries(search, false);
        Task<List<TheMovieDbResult>> taskListFilms  = GetFilms (search, false);

        Task.WaitAll( taskListFilms, taskListSeries );

        List<Result> listResult = new(taskListSeries.Result);
        listResult.AddRange(taskListFilms.Result);

        List<Result> list = new();

        foreach (Result res in listResult)
            if (!list.Contains(res))
                list.Add(res);

        return list.Select(r => new ModelMultiSearch()
        {
            Name = r.GetName(),
            Type = r.IsAnime() ? "Anime" : r.IsSerie() ? "Série" : r.IsFilm() ? "Film" : "Autre",
            Date = r.GetRealeaseDate(),
            Img = r.GetImage(),
            Lien = (r is TvMazeResult ? "tv" : r.IsFilm() ? "movie" : "tvmovie") + "/" + r.GetId()
        }).ToArray();
    }

    private static bool IsTypeSiteForResult(Result result, string typeSite)
    {
        if (string.IsNullOrWhiteSpace(typeSite))
            return false;

        return typeSite.Contains("all") ||                     // condition 1, c'est un site qui contient de tous
            result.IsAnime() && typeSite.Contains("animes") || // condition 2, c'est un site d'anime pour un anime
            result.IsSerie() && typeSite.Contains("séries") || // condition 3, c'est un site de serie pour une serie
            result.IsFilm()  && typeSite.Contains("film")   || // condition 4, c'est un site de film pour un film
            (result is TheMovieDbResult result1 && result1.IsHentai())       && typeSite.Contains("hentai") || // condition 5 Hentai
            (result is TheMovieDbResult result2 && result2.IsFilmAnimation() && typeSite.Contains("FA"));      // condition 6 film d'animation 
    }

}