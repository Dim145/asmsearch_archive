using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AnimeSearch.Models;
using AnimeSearch.Models.Results;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimeSearch.Core;

public sealed class ApiSearch
{
    /// <summary>
    ///     Récupère les données d'une série dans la base de donnée des séries/animes. Utilise l'algorithme de recherche optimisé de l'api 
    /// </summary>
    /// <param name="search">Le nom d'une série ou d'un anime</param>
    /// <param name="addOtherName">Ajoute les autres nom de la série (prend plus de temps car requête suplémentaire à l'api)</param>
    /// <returns>Un Objet de type <see cref="TvMazeResult"/></returns>
    public static async Task<TvMazeResult> GetSerie(string search, bool addOtherName = true) => await GetSerieFromUrl(Utilities.URL_ANIME_SINGLE_SEARCH + search, addOtherName);
    public static async Task<TvMazeResult> GetSerie(int id, bool addOtherName = true) => await GetSerieFromUrl(Utilities.URL_ANIME_SHOWS + id, addOtherName);

    private static async Task<TvMazeResult> GetSerieFromUrl(string url, bool addOtherName = true)
    {
        TvMazeResult serie = await Utilities.GetAndDeserialiseFromUrl<TvMazeResult>(url, (body) => body.Replace("Chinese", "zh-Hans"));

        if (serie != null && serie.GetImage() == null)
            serie.SetImage(serie.Image?.GetValueOrDefault("original"));

        if (serie != null && addOtherName)
            await AddAkasToTvMazeAsync(serie);

        return serie;
    }
    
    public static async Task<TheMovieDbResult> GetFilm(int id, bool type, bool addOtherName = true)
    {
        string url = (type ? Utilities.URL_MOVIEDB_TV_SHOWS : Utilities.URL_FILMS_SHOWS) + id + "?api_key=" + Utilities.MOVIEDB_API_KEY;

        TheMovieDbResult movie = await Utilities.GetAndDeserialiseFromUrl<TheMovieDbResult>(url, (str) => str.Replace("Chinese", "zh-Hans"));

        if (movie != null && addOtherName)
        {
            movie.SetTypeResult(type ? "tv" : "movie");

            movie.SetUrl(new("https://themoviedb.org/" + movie.GetTypeResult() + "/" + movie.GetId()));

            var languages = await Utilities.GetAndDeserialiseAnonymousFromUrl("https://api.themoviedb.org/3/" + movie.GetTypeResult() + "/" + movie.GetId() + "/alternative_titles?api_key=" + Utilities.MOVIEDB_API_KEY, new 
            {
                id = 0,
                results = new List<Dictionary<string, string>>(),
                titles = new List<Dictionary<string, string>>()
            });

            if(languages != null)
            {
                List<Dictionary<string, string>> dicToUse = languages.results ?? languages.titles;

                foreach (Dictionary<string, string> vals in dicToUse)
                {
                    try
                    {
                        CultureInfo lang = CultureInfo.CreateSpecificCulture(vals.GetValueOrDefault("iso_3166_1"));
                        movie.AddOtherName(lang, vals.GetValueOrDefault("title"));
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }

        return movie;
    }

    public static async Task<List<TvMazeResult>> GetSeries(string search, bool addOtherNames = true)
    {
        HttpResponseMessage response = await Utilities.CLIENT.GetAsync(Utilities.URL_ANIME_SEARCH + search);

        List<TvMazeResult> listSeries = null;

        if (response.IsSuccessStatusCode)
        {
            string json = (await response.Content.ReadAsStringAsync()).Replace("Chinese", "zh-Hans");

            var jsonArray = JsonConvert.DeserializeObject<JArray>(json);

            listSeries = new();

            foreach (var jToken in jsonArray)
            {
                var obj = (JObject) jToken;

                TvMazeResult res = JsonConvert.DeserializeObject<TvMazeResult>(obj.GetValue("show")?.ToString() ?? string.Empty);

                if (res.GetImage() == null)
                    res.SetImage(res.Image?.GetValueOrDefault("original"));

                listSeries.Add(res);
            }

            if (addOtherNames && listSeries.Count > 0)
            {
                Task[] tasks = new Task[listSeries.Count];

                for (int cpt = 0; cpt < tasks.Length; cpt++)
                    tasks[cpt] = AddAkasToTvMazeAsync(listSeries[cpt]);

                Task.WaitAll(tasks);
            }
        }

        return listSeries;
    }

    public static async Task<TheMovieDbResult> GetFilm(string search)
    {
        List<TheMovieDbResult> list = (await GetFilms(search));

        if (list != null && list.Count > 0)
        {
            TheMovieDbResult selectedResult = list.Where(item => item.GetAllOtherNamesList().Contains(search))?.FirstOrDefault();

            return selectedResult ?? list.FirstOrDefault();
        }

        return null;
    }

    public static async Task<List<TheMovieDbResult>> GetFilms(string search, bool addOtherNames = true, int page = 1)
    {
       var tmp = await Utilities.GetAndDeserialiseAnonymousFromUrl($"{Utilities.URL_FILMS_SEARCH}{search}&api_key={Utilities.MOVIEDB_API_KEY}&page={Math.Max(page, 1)}", new
        {
            page = 0,
            results = new List<TheMovieDbResult>()
        });

        if (tmp != null)
        {
            List<Task> listTask = new();
            foreach(TheMovieDbResult movie in tmp.results)
            {
                movie.SetUrl(new("https://themoviedb.org/" + movie.GetTypeResult() + "/" + movie.GetId()));

                if(addOtherNames) listTask.Add(Task.Run(async () =>
                {
                    var languages = await Utilities.GetAndDeserialiseAnonymousFromUrl("https://" + $"api.themoviedb.org/3/{movie.GetTypeResult()}/{movie.GetId()}/alternative_titles?api_key={Utilities.MOVIEDB_API_KEY}", new
                    {
                        id = 0,
                        results = new List<Dictionary<string, string>>(),
                        titles = new List<Dictionary<string, string>>()
                    });

                    if (languages != null)
                    {
                        List<Dictionary<string, string>> dicToUse = languages.results ?? languages.titles;

                        foreach (Dictionary<string, string> vals in dicToUse)
                        {
                            CultureInfo lang = null;

                            try
                            {
                                lang = CultureInfo.CreateSpecificCulture(vals.GetValueOrDefault("iso_3166_1"));
                                movie.AddOtherName(lang, vals.GetValueOrDefault("title"));
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                }));
            }

            Task.WaitAll(listTask.ToArray());

           return tmp.results;
        }

        return new List<TheMovieDbResult>();
    }

    public static Task<string> GetBandAnnonce(Result searchResult) => Task.Run(async () =>
    {
        Dictionary<string, string> listValue = new();
        listValue.Add("q", searchResult.GetName() + " bande annonce");

        HttpResponseMessage response = await Utilities.CLIENT.PostAsync(Utilities.URL_DUCKDUCKGO_SEARCH, new FormUrlEncodedContent(listValue));

        if(response.IsSuccessStatusCode)
        {
            HtmlDocument doc = new();

            string html = await response.Content.ReadAsStringAsync();
            doc.LoadHtml(html);

            HtmlNode mainDiv = doc.GetElementbyId("links");

            HtmlNodeCollection otherResults = mainDiv?.SelectNodes("div/div/h2/a");

            if (otherResults != null)
            {
                string url = otherResults.Select(node => node.Attributes["href"].Value).Where(url => url.Contains("youtube", StringComparison.InvariantCultureIgnoreCase) || url.Contains("allocine", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

                if (url != null && url.Contains("youtube"))
                    url = url.Replace("watch", "embed").Replace("?v=", "/");

                return url;
            }
        }

        return null;
    });

    public static async Task<List<Result>> RechercheByGenresAsync(AdvanceSearch advSearch)
    {
        IEnumerable<Result> results = new List<Result>();

        string genreSeparator = advSearch.AndGenre ? "," : "|";

        string strwithgenres = advSearch.With_genres == null ? "" : string.Join(genreSeparator, advSearch.With_genres.Where(s => Utilities.TMBD_GENRES.Any(genre => genre.Name == s)).Select(s => Utilities.TMBD_GENRES.Find(genre => genre.Name == s).Id.ToString()));
        string strwithoutgenres = advSearch.Without_genres == null ? "" : string.Join(genreSeparator, advSearch.Without_genres.Where(s => Utilities.TMBD_GENRES.Any(genre => genre.Name == s)).Select(s => Utilities.TMBD_GENRES.Find(genre => genre.Name == s).Id.ToString()));

        if (string.IsNullOrWhiteSpace(advSearch.Q))
        {
            // Utilisation de tmdb seulement pour une recherche par genres car TVMaze n'as pas d'api pour la recherche par genres

            string filter = $"?api_key={Utilities.MOVIEDB_API_KEY}&page={advSearch.Page}&language=fr-FR&include_adult=true" +

            $"{(string.IsNullOrWhiteSpace(strwithgenres) ? "" : $"&with_genres={strwithgenres}")}{(string.IsNullOrWhiteSpace(strwithoutgenres) ? "" : $"&without_genres={strwithoutgenres}")}" +
                $"{(advSearch.Before == null ? "" : $"&primary_release_date.lte={advSearch.Before.Value:yyyy-MM-dd}&first_air_date.lte={advSearch.Before.Value:yyyy-MM-dd}")}{(advSearch.After == null ? "" : $"&primary_release_date.gte={advSearch.After.Value:yyyy-MM-dd}&first_air_date.gte={advSearch.After.Value:yyyy-MM-dd}")}" +
            $"{(advSearch.SortBy > -1 && advSearch.SortBy < Utilities.SORT_BY_DISCOVERY.Length ? $"&sort_by={Utilities.SORT_BY_DISCOVERY[advSearch.SortBy].Key}" : "")}";

            var type = new { page = 0, results = new List<TheMovieDbResult>(), total_pages = -1 };

            if(advSearch.SearchIn == AdvanceSearch.SearchType.ALL || advSearch.SearchIn == AdvanceSearch.SearchType.MOVIES)
            {
                var discoverMovies = await Utilities.GetAndDeserialiseAnonymousFromUrl($"{Utilities.URL_DISCOVER_TMDB}movie{filter}", type);

                discoverMovies.results.ForEach(m => m.Media_type = "movie");

                if (discoverMovies.results != null)
                    results = results.Concat(discoverMovies.results);
            }

            if(advSearch.SearchIn == AdvanceSearch.SearchType.ALL || advSearch.SearchIn == AdvanceSearch.SearchType.SERIES)
            {
                var discoverTV = await Utilities.GetAndDeserialiseAnonymousFromUrl($"{Utilities.URL_DISCOVER_TMDB}tv{filter}", type);

                if (discoverTV.results != null)
                    results = results.Concat(discoverTV.results);
            }
        }
        else // Recherche + filtre plus efficace que filtre sur les nom après les genres
        {
            var indicePage  = advSearch.Page * 2;
            var movieSearch = advSearch.SearchIn == AdvanceSearch.SearchType.ALL || advSearch.SearchIn == AdvanceSearch.SearchType.MOVIES;
            var serieSearch = advSearch.SearchIn == AdvanceSearch.SearchType.ALL || advSearch.SearchIn == AdvanceSearch.SearchType.SERIES;

            var taskSeries = serieSearch ? GetSeries(advSearch.Q, false) : null;

            // TMDB part
            var tmdbResults = await GetFilms(advSearch.Q, false, indicePage - 1);
            tmdbResults.AddRange(await GetFilms(advSearch.Q, false, indicePage));

            var withGenreIds = strwithgenres.Split(genreSeparator);
            var withoutGenreIds = strwithoutgenres.Split(genreSeparator);

            bool filterWithGenre(TheMovieDbResult t)
            {
                if (advSearch.With_genres == null || advSearch.With_genres.Length == 0)
                    return true;

                return t.Genres != null && (advSearch.AndGenre ? withGenreIds.All(id => t.Genres.Any(g => g.Id.ToString() == id)) : withGenreIds.Any(id => t.Genres.Any(g => g.Id.ToString() == id)));
            }

            bool filterWithoutGenres(TheMovieDbResult t)
            {
                if (advSearch.Without_genres == null || advSearch.Without_genres.Length == 0 || t.Genres == null)
                    return true;

                return advSearch.AndGenre ? withoutGenreIds.All(id => !t.Genres.Any(g => g.Id.ToString() == id)) : withoutGenreIds.Any(id => !t.Genres.Any(g => g.Id.ToString() == id));
            }

            var filteredTmdb = tmdbResults
                .Where(t => advSearch.SearchIn == AdvanceSearch.SearchType.ALL || movieSearch && (t.IsFilm() || t.IsFilmAnimation()) || serieSearch && (t.IsSerie() || t.IsAnime() || t.IsHentai()))
                .Where(filterWithGenre)
                .Where(filterWithoutGenres);


            // TVMaze part
            var filteredTvMaze = (serieSearch ? await taskSeries : new())
                .Where(r => FilterWithGenreTVMaze(r, advSearch))
                .Where(r => FilterWithoutGenreTVMaze(r, advSearch));


            results = results.Concat(filteredTvMaze).Concat(filteredTmdb)
                .Where(r => (advSearch.After == null || r.GetRealeaseDate() > advSearch.After) && (advSearch.Before == null || r.GetRealeaseDate() < advSearch.Before));
        }

        if (advSearch.SortBy > -1 && advSearch.SortBy < Utilities.SORT_BY_DISCOVERY.Length)
        {
            var values = Utilities.SORT_BY_DISCOVERY[advSearch.SortBy].Key.Split(".");
            var funcSort = Utilities.SORT_BY_SEARCH.GetValueOrDefault(values[0]);

            if (funcSort != null)
            {
                if (values[1] == "asc") results = results.OrderBy(funcSort);
                else if (values[1] == "desc") results = results.OrderByDescending(funcSort);
            }
        }

        return results.ToList();
    }

    private static bool FilterWithGenreTVMaze(TvMazeResult r, AdvanceSearch advSearch)
    {
        if (advSearch.With_genres == null || advSearch.With_genres.Length == 0)
            return true;

        return r.GetGenres() != null && (advSearch.AndGenre ? advSearch.With_genres.All(g => r.GetGenres().Any(genre => g.ContainsGenre(genre))) : advSearch.With_genres.Any(g => r.GetGenres().Any(genre => g.ContainsGenre(genre))));
    }

    private static bool FilterWithoutGenreTVMaze(TvMazeResult r, AdvanceSearch advSearch)
    {
        if (advSearch.Without_genres == null || advSearch.Without_genres.Length == 0 || r.GetGenres() == null)
            return true;

        return advSearch.AndGenre ? advSearch.Without_genres.All(g => !r.GetGenres().Any(genre => g.ContainsGenre(genre))) : advSearch.Without_genres.Any(g => !r.GetGenres().Any(genre => g.ContainsGenre(genre)));
    }
    
    private static async Task AddAkasToTvMazeAsync(TvMazeResult serie)
    {
        object languages = await Utilities.GetAndDeserialiseAnonymousFromUrl<object>(Utilities.URL_BASE_ANIME_API + "shows/" + serie.GetId() + "/akas", new
        {
            Name = "",
            Country = new Dictionary<string, string>()
        });

        if (languages is JArray array)
            foreach (JToken t in array)
                AddJsonTokenNameToSerie(t, serie);
        else
            AddJsonTokenNameToSerie((JToken)languages, serie);

        serie.AddNameInLanguage();
    }
    
    private static void AddJsonTokenNameToSerie(JToken t, TvMazeResult serie)
    {
        string name = t.Value<string>("name");

        JObject counry = t.Value<JObject>("country");

        CultureInfo culture;
        try
        {
            string code = counry?.Value<string>("code");

            if (code != null && code.ToLower().Equals("jp"))
                code = "ja";

            culture = code != null ? new(code) : CultureInfo.InvariantCulture;
        }
        catch (Exception)
        {
            culture = CultureInfo.InvariantCulture;
        }

        serie.AddOtherName(culture, name);
    }
}