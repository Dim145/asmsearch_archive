using AnimeSearch.Core;
using AnimeSearch.Core.Extensions;
using AnimeSearch.Core.Models.Api;
using AnimeSearch.Core.ViewsModel;
using AnimeSearch.Data;
using HtmlAgilityPack;

namespace AnimeSearch.Services.Search;

public class ApiService
{
    private HttpClient Client { get; }

    public ApiService(HttpClient client)
    {
        Client = client;
    }

    public async Task<List<Result>> RechercheByGenresAsync(AdvanceSearch advSearch)
    {
        IEnumerable<Result> results = new List<Result>();

        if (string.IsNullOrWhiteSpace(advSearch.Q))
        {
            var tasks = DataUtils.Apis.Select(a => a.Discover(advSearch)).ToArray();

            await Task.WhenAll(tasks);

            results = results.Concat(tasks.SelectMany(t => t.Result.Results));
        }
        else // Recherche + filtre plus efficace que filtre sur les nom après les genres
        {
            var tmpResults = new List<Result>();
            
            var indicePage  = advSearch.Page * 2;

            switch (advSearch.SearchIn)
            {
                case SearchType.All: tmpResults.AddRange(DataUtils.Apis
                    .Select(a => a.SearchMany(advSearch.Q, indicePage - 1))
                    .Select(t => t.GetAwaiter().GetResult()).SelectMany(m => m.Results));
                    break;
                case SearchType.Movies: tmpResults.AddRange(DataUtils.Apis
                    .Select(a => a.SearchMany(advSearch.Q, indicePage - 1, ResultType.Movies))
                    .Select(t => t.GetAwaiter().GetResult()).SelectMany(m => m.Results));
                    break;
                case SearchType.Series: 
                    tmpResults.AddRange(DataUtils.Apis
                        .Select(a => a.SearchMany(advSearch.Q, indicePage - 1, ResultType.Series))
                        .Select(t => t.GetAwaiter().GetResult()).SelectMany(m => m.Results));
                    
                    tmpResults.AddRange(DataUtils.Apis
                        .Select(a => a.SearchMany(advSearch.Q, indicePage, ResultType.Anime))
                        .Select(t => t.GetAwaiter().GetResult()).SelectMany(m => m.Results));
                    break;
            }

            bool FilterByGenres(Result r)
            {
                var withGenres = advSearch.WithGenres is null || (advSearch.AndGenre
                    ? advSearch.WithGenres.All(g => r.GetGenres()?.Any(genre => genre.ContainsGenre(g)) ?? false)
                    : advSearch.WithGenres.Any(g => r.GetGenres()?.Any(genre => genre.ContainsGenre(g)) ?? false));

                var withoutGenres = advSearch.WithoutGenres is null || (advSearch.AndGenre
                    ? advSearch.WithoutGenres.All(g => !(r.GetGenres()?.Any(genre => genre.ContainsGenre(g)) ?? true))
                    : advSearch.WithoutGenres.Any(g => !(r.GetGenres()?.Any(genre => genre.ContainsGenre(g)) ?? true)));

                return withGenres && withoutGenres;
            }
            
            results = tmpResults.Where(FilterByGenres)
                .Where(r => (advSearch.After == null || r.ReleaseDate > advSearch.After) && (advSearch.Before == null || r.ReleaseDate < advSearch.Before));
        }
        
        if (advSearch.SortBy != null)
        {
            results = advSearch.SortBy switch
            {
                Sort.DateA => results.OrderByDescending(r => r.ReleaseDate),
                Sort.DateD => results.OrderBy(r => r.ReleaseDate),
                Sort.TitleA => results.OrderBy(r => r.Name),
                Sort.TitleD => results.OrderByDescending(r => r.Name),
                Sort.PopularityA => results.OrderBy(r => r.Popularity ?? -1),
                Sort.PopularityD => results.OrderByDescending(r => r.Popularity ?? -1),
                _ => results
            };
        }

        var tmp = new[] {ResultType.Anime, ResultType.Series, ResultType.Movies};
        
        return results.Where(r => tmp.Any(r.Is)).ToList();
    }
    
    public async Task<Result> SearchResult(string search)
    {
        var serieFirst = true;

        if (search.Contains("=>"))
        {
            int index = search.IndexOf("=>", StringComparison.InvariantCultureIgnoreCase);

            var before = search[..index];
            search = search[(index + 2)..];

            serieFirst = before != "movie";
        }

        var tasks = DataUtils.Apis.Select(a => a.SearchOne(search)).ToArray();

        await Task.WhenAll(tasks);

        var result = tasks
            .Where(t => t.Result != null)
            .OrderByDescending(t => serieFirst ? t.Result.IsSerie || t.Result.IsAnime ? 1 : 0 : t.Result.IsFilm ? 1 : 0)
            .ThenByDescending(t => t.Result?.Popularity ?? -1)
            .FirstOrDefault()?.Result;

        if (result is null or {Name: null}) // ne devrais jamais arriver
        {
            result = new Result
            {
                Name = "introuvable"
            };
        }

        return result;
    }
    
    public async Task<Result> SearchResult(int id, ResultType type = ResultType.All, int idApi = -1)
    {
        var tasks = new List<Task<Result>>();

        if (idApi <= -1)
        {
            tasks.AddRange(DataUtils.Apis.Select(a => a.GetById(id, type)));
        }
        else
        {
            var task = DataUtils.Apis.FirstOrDefault(a => a.Id == idApi)?.GetById(id, type);
            
            if(task != null)
                tasks.Add(task);
        }

        await Task.WhenAll(tasks.ToArray());

        var results = tasks.Select(t => t.Result).DistinctBy(r => r.Name);

        return results.FirstOrDefault();
    }
}