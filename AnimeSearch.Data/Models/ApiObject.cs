using System.Globalization;
using AnimeSearch.Core;
using AnimeSearch.Core.Extensions;
using AnimeSearch.Core.Models.Api;
using AnimeSearch.Core.ViewsModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimeSearch.Data.Models;

public class ApiObject
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string SiteUrl { get; set; }
    public string LogoUrl { get; set; }
    public string Description { get; set; }

    /// <summary>
    /// Base Url
    /// </summary>
    public string ApiUrl { get; set; }

    /// <summary>
    /// Type recherche
    /// </summary>
    public string SearchUrl { get; set; }

    /// <summary>
    /// Type Recherche
    /// </summary>
    public string SingleSearchUrl { get; set; }

    public string GlobalSearchUrl { get; set; }
    public string MoviesSearchUrl { get; set; }
    public string TvSearchUrl { get; set; }
    public string AnimeSearchUrl { get; set; }
    public string DiscoverUrl { get; set; }

    public string MoviesIdUrl { get; set; }
    public string TvIdUrl { get; set; }
    public string AnimeIdUrl { get; set; }

    public string Token { get; set; } = string.Empty;
    public string TokenName { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;

    public Dictionary<string, string> TableFields { get; set; } = new();
    
    public string PathToResults { get; set; }
    
    public string GenresMoviesUrl { get; set; }
    public string GenresTvUrl { get; set; }
    
    public string GenresPath { get; set; }
    
    public string OtherNamesUrl { get; set; }
    
    public string PathToOnResults { get; set; }
    
    public string PathInOnResObject { get; set; }
    
    public string ImageBasePath { get; set; }

    public bool IsValid => !string.IsNullOrWhiteSpace(ApiUrl) &&
                           string.IsNullOrWhiteSpace(TokenName) == string.IsNullOrWhiteSpace(Token) &&
                           !string.IsNullOrWhiteSpace(Name);
    
    public ICollection<ApiObjectFilter> Filters { get; set; } = new List<ApiObjectFilter>();
    public ICollection<ApiObjectSort> Sorts { get; set; } = new List<ApiObjectSort>();

    public List<Genre> Genres { get; set; }

    public async Task<List<Genre>> WaitGenres()
    {
        Task[] tab = {null, null};
        var resGenres = new List<Genre>();
        
        if (!string.IsNullOrWhiteSpace(GenresMoviesUrl))
        {
            tab[0] = CoreUtils.GetAndDeserialiseFromUrl<JObject>(GetGenresUrl(ResultType.Movies).ToString(), client: new())
                .ContinueWith(res =>
                {
                    if(!res.IsCompletedSuccessfully)
                        return;
                    
                    var genres = res.Result?.SelectToken(GenresPath)?.ToObject<Dictionary<string, string>[]>() ?? Array.Empty<Dictionary<string, string>>();

                    foreach (var genre in genres)
                    {
                        var id = genre.GetValueOrDefault("id");
                        var name = genre.GetValueOrDefault("name") ?? genre.GetValueOrDefault("label") ?? string.Empty;
                        
                        resGenres.Add(new(){ Name = name, Id = id, ApiId = Id, Type = SearchType.Movies});
                    }
                });
        }

        if (!string.IsNullOrWhiteSpace(GenresTvUrl))
        {
            tab[1] = CoreUtils.GetAndDeserialiseFromUrl<JObject>(GetGenresUrl(ResultType.Series).ToString(), client: new())
                .ContinueWith(res =>
                {
                    if(!res.IsCompletedSuccessfully)
                        return;
                    
                    tab[0]?.Wait(1000);

                    var genres = res.Result?.SelectToken(GenresPath)?.ToObject<Dictionary<string, string>[]>() ?? Array.Empty<Dictionary<string, string>>();

                    foreach (var genre in genres)
                    {
                        var id = genre.GetValueOrDefault("id");
                        var name = genre.GetValueOrDefault("name") ?? genre.GetValueOrDefault("label") ?? string.Empty;
                        
                        resGenres.Add(new(){ Name = name, Id = id, ApiId = Id, Type = SearchType.Series});
                    }
                });
        }

        if(tab[1] is not null || tab[0] is not null)
            await (tab[1] ?? tab[0]);

        return resGenres;
    }

    public Uri GetGenresUrl(ResultType type)
    {
        var genreStr = type switch
        {
            ResultType.Movies => GenresMoviesUrl,
            ResultType.Series or ResultType.Anime => GenresTvUrl,
            _ => GenresMoviesUrl ?? GenresTvUrl
        };

        return new Uri(ApiUrl + genreStr + GetApiUrlPart(genreStr));
    }

    public Uri GetSearchURL(string search, ResultType type, bool single = false, int page = -1)
    {
        var sm = single ? SingleSearchUrl ?? SearchUrl : SearchUrl;

        var typeStr = type switch
        {
            ResultType.Anime when !string.IsNullOrWhiteSpace(AnimeSearchUrl) => AnimeSearchUrl,
            ResultType.Series when !string.IsNullOrWhiteSpace(TvSearchUrl) => TvSearchUrl,
            ResultType.Movies when !string.IsNullOrWhiteSpace(MoviesSearchUrl) => MoviesSearchUrl,
            ResultType.All when !string.IsNullOrWhiteSpace(GlobalSearchUrl) => GlobalSearchUrl,
            _ => string.Empty
        };

        var str = ApiUrl + sm + typeStr + search;
        str += GetApiUrlPart(str);

        return new Uri(str + GetPageAttribute(page, str));
    }

    public Uri GetDiscoverUrl(ResultType type, AdvanceSearch adv)
    {
        var genreSeparator = adv.AndGenre ? ',' : '|';
        
        var typeStr = TypeIdBySearchType(type);
        
        var str = ApiUrl + DiscoverUrl + typeStr;

        if (str.EndsWith("/"))
            str = str[..^1];

        //------ filters -------
        
        var filters = "?";
        
        foreach (var filter in Filters)
        {
            var value = filter.ApiFilter.Label switch
            {
                "AfterDate" => adv.After?.ToString("yyyy-MM-dd"),
                "BeforeDate" => adv.Before?.ToString("yyyy-MM-dd"),
                "WithGenre"    => adv.WithGenres    == null ? string.Empty : string.Join(genreSeparator, adv.WithGenres   .Where(s => Genres.Any(g => g.Name == s)).Select(s => Genres.First(kv => kv.Name == s).Id)),
                "WithoutGenre" => adv.WithoutGenres == null ? string.Empty : string.Join(genreSeparator, adv.WithoutGenres.Where(s => Genres.Any(g => g.Name == s)).Select(s => Genres.First(kv => kv.Name == s).Id)),
                "Page" => adv.Page <= 0 ? string.Empty : adv.Page.ToString(),
                _ => string.Empty
            };
            
            if(string.IsNullOrWhiteSpace(value))
                continue;

            var tab = filter.FieldValue.Split('|');

            filters += (filters == "?" ? string.Empty : "&") + string.Join("&", tab.Select(t => $"{t}={value}"));
        }
        
        //----- Order ------

        var sortValue = adv.SortBy != null && Sorts.Any(s => adv.SortBy.Value.ToString() == s.ApiSort.Label) ? Sorts.FirstOrDefault(s => adv.SortBy.Value.ToString() == s.ApiSort.Label )?.FieldValue : string.Empty;
            
        if(!string.IsNullOrWhiteSpace(sortValue))
            filters += $"&sort_by={sortValue}";
        
        filters = filters == "?" ? string.Empty: filters;
        
        return string.IsNullOrWhiteSpace(DiscoverUrl) ? null : new Uri(str + filters + GetApiUrlPart(filters) + "&language=fr-FR&include_adult=true");
    }

    public Uri GetUrlWithId(object id, ResultType type = ResultType.All)
    {
        var typeStr = TypeIdBySearchType(type);

        var str = ApiUrl + typeStr + id;
        
        return new Uri(str + GetApiUrlPart(str));
    }

    public async Task<Result> SearchOne(string search, ResultType type = ResultType.All)
    {
        var res = await CoreUtils.GetAndDeserialiseFromUrl<Dictionary<string, dynamic>>(GetSearchURL(Uri.EscapeDataString(search), type, true).ToString(), body => body.Replace("Chinese", "zh-Hans"));

        if (res == null)
            return null;
        
        if (!string.IsNullOrWhiteSpace(PathToResults))
        {
            var tmp = res.GetValueOrDefault(PathToResults);

            if (tmp is JArray array)
                tmp = array.Count == 0 ? new Dictionary<string, dynamic>() : array.OrderByDescending(jo => jo.SelectToken(TableFields.FirstOrDefault(kv => kv.Value == "popularity").Key ?? "popularity")?.Value<int>()).First();
            
            if (tmp is JObject jObject)
                res = jObject.ToObject<Dictionary<string, dynamic>>();
        }

        var result = ConvertDictionaryToResult(res);
        
        if(string.IsNullOrWhiteSpace(result.Type) && type != ResultType.All)
            result.Type = type.ToTypeString() ?? string.Empty;
        
        return result.Is(type) ? await GetOtherNames(result, type) : null;
    }

    public async Task<ManyResult> SearchMany(string search, int page = 1, ResultType type = ResultType.All)
    {
        var res = await CoreUtils.GetAndDeserialiseFromUrl<dynamic>(GetSearchURL(search, type, false, page).ToString(), body => body.Replace("Chinese", "zh-Hans"));
        int totalPage = 1;
        
        List<Result> list = new();

        var tmp = res;

        if (!string.IsNullOrWhiteSpace(PathToResults) && res is JToken jToken)
            tmp = jToken.SelectToken(PathToResults) ?? res;

        if (tmp is JArray array)
        {
            foreach (var token in array)
            {
                var resToken = !string.IsNullOrWhiteSpace(PathToResults) ? token.SelectToken(PathToResults) ?? token : token;
                var result = ConvertDictionaryToResult(resToken.ToObject<Dictionary<string, dynamic>>(), type);
                
                if(string.IsNullOrWhiteSpace(result.Type) && type != ResultType.All)
                    result.Type = type.ToTypeString() ?? string.Empty;
                
                if(result.Is(type))
                    list.Add(result);
            }
        }

        if (tmp is JObject obj && !string.IsNullOrWhiteSpace(PathToResults))
        {
            tmp = obj.GetValue(PathToResults);
            
            if(tmp is JArray results)
                list.AddRange(results.Values<Dictionary<string, dynamic>>().Where(d => d != null).Select(d => ConvertDictionaryToResult(d)));
        }

        if (res is JObject resObj)
        {
            JToken totalToken = null;
            
            foreach (var path in new[]{"total", "totalPages", "total_pages", "all", "allPages", "all_pages"})
                if (totalToken == null)
                    totalToken = resObj.SelectToken(path);

            if (totalToken != null)
                totalPage = totalToken.Value<int>();
        }

        Task.WaitAll(list.Select(r => GetOtherNames(r, type)).ToArray());
        
        return new()
        {
            Results = list.ToArray(),
            Page = page,
            TotalPage = totalPage
        };
    }

    public async Task<Result> GetOtherNames(Result res, ResultType type = ResultType.All)
    {
        var resType = res.ToSearchType();
        var response = await CoreUtils.GetAndDeserialiseFromUrl<dynamic>(GetOtherNamesUrl(res.Id, resType is ResultType.All ? type : resType).ToString(), body => body.Replace("Chinese", "zh-Hans"));

        if (response is JObject jObject && !string.IsNullOrWhiteSpace(PathToOnResults))
        {
            response = jObject.SelectToken(PathToOnResults) ?? jObject.Last?.First; // si le chemin ne correspond pas, on prend la premiere valeur de la dernière propriété pour test.
        }

        if (response is JArray jArray && !string.IsNullOrWhiteSpace(PathInOnResObject) && PathInOnResObject.Contains('|'))
        {
            var inObjectRes = PathInOnResObject.Split('|');

            foreach (var jToken in jArray)
            {
                var cult = jToken.SelectToken(inObjectRes[1]) is { } token ? token.Value<string>() : string.Empty;
                var culture = CultureInfo.InvariantCulture;

                if (!string.IsNullOrWhiteSpace(cult))
                {
                    try
                    {
                        culture = JsonConvert.DeserializeObject<CultureInfo>($"\"{cult}\"");
                    }
                    catch (Exception)
                    {
                        culture = CultureInfo
                            .GetCultures(CultureTypes.AllCultures)
                            .Where(c => string.Equals(c.Name, cult, StringComparison.InvariantCultureIgnoreCase) || string.Equals(c.EnglishName, cult, StringComparison.InvariantCultureIgnoreCase) || string.Equals(c.NativeName, cult, StringComparison.InvariantCultureIgnoreCase))
                            .FirstOrDefault(CultureInfo.InvariantCulture);
                    }
                }

                res.AddOtherName(culture, jToken.SelectToken(inObjectRes[0]).Value<string>());
            }
        }

        return res;
    }
    
    public async Task<Result> GetById(object id, ResultType type = ResultType.All)
    {
        var res = await CoreUtils.GetAndDeserialiseFromUrl<Dictionary<string, dynamic>>(GetUrlWithId(id, type).ToString(),
            body => body.Replace("Chinese", "zh-Hans"));

        var result = ConvertDictionaryToResult(res);

        result = await GetOtherNames(result, type);

        if (type is not ResultType.All)
        {
            if(type is ResultType.Anime && result.GetGenres().Contains(ResultType.Anime.ToTypeString()))
                result.Type = ResultType.Anime.ToTypeString();
            else if(result.Type is null)
                result.Type = type.ToTypeString();
        }

        return result;
    }

    public async Task<ManyResult> Discover(AdvanceSearch adv)
    {
        if (string.IsNullOrWhiteSpace(DiscoverUrl))
            return new(){ Results = Array.Empty<Result>()};

        var type = adv.SearchIn.ToResultType();
        
        var responses2 = new Dictionary<ResultType, dynamic>();

        foreach (var searchType in Enum.GetValues<ResultType>())
        {
            if (searchType != ResultType.All && type is ResultType.All || type == ResultType.Movies && searchType == ResultType.Movies || adv.SearchIn == SearchType.Series && searchType is ResultType.Anime or ResultType.Series)
            {
                var url = GetDiscoverUrl(searchType, adv)?.ToString();
                responses2.Add(searchType, await CoreUtils.GetAndDeserialiseFromUrl<dynamic>(url, body => body.Replace("Chinese", "zh-Hans")));
            }
        }
        var list = new List<Result>();
        var totalPage = 0;
        
        foreach (var key in responses2.Keys)
        {
            var res = responses2[key];

            if (res is JObject jObject)
            {
                JToken totalToken = null;
            
                foreach (var path in new[]{"total", "totalPages", "total_pages", "all", "allPages", "all_pages"})
                    if (totalToken == null)
                        totalToken = jObject.SelectToken(path);

                if (totalToken != null)
                {
                    var total = totalToken.Value<int>();

                    if(totalPage < total)
                        totalPage = total;
                }
                
                res = jObject.SelectToken(PathToResults);
            }

            if (res is JArray jArray)
            {
                foreach (var token in jArray)
                {
                    var resToken = !string.IsNullOrWhiteSpace(PathToResults) ? token.SelectToken(PathToResults) ?? token : token;
                    var result = ConvertDictionaryToResult(resToken.ToObject<Dictionary<string, dynamic>>());
                
                    if(string.IsNullOrWhiteSpace(result.Type))
                        result.Type = key.ToTypeString() ?? string.Empty;
                
                    if(result.Is(key) && !list.Exists(r => r.Id == result.Id))
                        list.Add(result);
                }
            }
        }

        return new()
        {
            Results = list.ToArray(),
            TotalPage = totalPage,
            Page = 1
        };
    }
    
    private string GetApiUrlPart(string endUrl = "")
    {
        if(string.IsNullOrWhiteSpace(Token))
            return string.Empty;
        
        var separator = endUrl.Contains('?') ? '&' : '?';

        return $"{separator}{TokenName}={Token}";
    }

    private string GetPageAttribute(int page, string endUrl = "")
    {
        if (string.IsNullOrWhiteSpace(PageName) || page < 0)
            return string.Empty;

        var separator = endUrl.Contains('?') ? '&' : '?';

        return $"{separator}{PageName}={page}";
    }

    private string[] ResolveGenresWithIds(object[] ids)
    {
        return ids.Select(i => Genres.FirstOrDefault(g => g.Id == i.ToString())?.Name ?? string.Empty).ToArray();
    }

    private Result ConvertDictionaryToResult(Dictionary<string, dynamic> d, ResultType resultType = ResultType.All)
    {
        var res = new Result
        {
            IdApiFrom = Id,
            AddtionnalFields = new()
        };

        foreach (var key in d.Keys)
        {
            var data = d.GetValueOrDefault(key);
            var type = TableFields.GetValueOrDefault(key);

            if (data == null || type == null)
            {
                res.AddtionnalFields.Add(key, data);
                continue;
            }

            if (data is long l)
                data = (int) l;

            if (data is JObject jObject)
            {
                var tmp = jObject.First;
                
                while (tmp.HasValues)
                    tmp = tmp.First;

                data = tmp.Value<string>();
            }

            if (data is string s && s.Contains('/') && Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out var uri))
                data = uri;

            if (data is JArray {HasValues: true} array)
            {
                var typeFirst = array.First?.Type switch
                {
                    JTokenType.String => typeof(string),
                    JTokenType.Boolean => typeof(bool),
                    JTokenType.Date => typeof(DateTime),
                    JTokenType.TimeSpan => typeof(TimeSpan),
                    JTokenType.Integer => typeof(int),
                    JTokenType.Float => typeof(float),
                    JTokenType.Guid => typeof(Guid),
                    JTokenType.Uri => typeof(Uri),
                    _ => null
                };

                if (typeFirst != null)
                    data = array.Descendants().Select(t => t.ToObject(typeFirst)).ToArray();
            }

            if (type == "genres")
            {
                if(data is object[] datas)
                    data = datas.Select(d => d.ToString()).ToArray();
                else if (data is JArray arrayDatas)
                {
                    data = arrayDatas.AsEnumerable()
                        .Select(d => d.Value<string>("name") ?? d.Value<string>("label") ?? d.Value<string>())
                        .ToArray();
                }
            }

            if(data == null)
                continue;

            try
            {
                switch(type)
                {
                    case "id": res.Id = data;break;
                    case "name": res.Name = data.ToString();break;
                    case "language": res.Language = JsonConvert.DeserializeObject<CultureInfo>($"\"{data}\"");break;
                    case "other_name": res.AddOtherName(data.ToString());break;
                    case "type": res.Type = data;break;
                    case "release_date": res.ReleaseDate = string.IsNullOrWhiteSpace(data) ? null : DateTime.Parse(data);break;
                    case "description": res.Description = data.ToString();break;
                    case "popularity": res.Popularity = data switch
                    {
                        float f => f,
                        double dou => (float) dou,
                        _ => (float) double.Parse(data, CultureInfo.InvariantCulture)
                    };break;
                    case "image": res.Image = new(data.ToString().StartsWith("http") ? data.ToString() : SiteUrl + ImageBasePath + data.ToString());break;
                    case "genres": res.AddGenre(data);break;
                    case "genre_ids": res.AddGenre(ResolveGenresWithIds(data is JArray ? Array.Empty<object>() : data));break;
                    case "url": res.Url = data;break;
                    case "status": res.Status = data;break;
                    case "18+": res.Adult = data is true;break;
                }
            }
            catch (Exception e)
            {
                CoreUtils.AddExceptionError($"la \"désérialisation\" d'un result (\"{res.Name}\", data en cause: {data})", e);
            }
        }
        
        if(res.Url == null)
            res.Url = new(SiteUrl + TypeIdBySearchType((ResultType?) (res.IsFilm ? ResultType.Movies :
                res.IsAnime ? ResultType.Anime : res.IsSerie ? ResultType.Series : null) ?? resultType ) + res.Id);
        
        return res;
    }

    public Uri GetOtherNamesUrl(object id, ResultType type = ResultType.All)
    {
        var typeStr = TypeIdBySearchType(type);

        var url = $"{ApiUrl}{typeStr}{id}/{OtherNamesUrl}";

        return new(url + GetApiUrlPart(url));
    }

    private string TypeIdBySearchType(ResultType type) =>  type switch
    {
        ResultType.Anime when !string.IsNullOrWhiteSpace(AnimeIdUrl) => AnimeIdUrl,
        ResultType.Series when !string.IsNullOrWhiteSpace(TvIdUrl) => TvIdUrl,
        ResultType.Movies when !string.IsNullOrWhiteSpace(MoviesIdUrl) => MoviesIdUrl,
        _ => TvIdUrl ?? AnimeIdUrl ?? MoviesIdUrl ?? string.Empty
    };
}