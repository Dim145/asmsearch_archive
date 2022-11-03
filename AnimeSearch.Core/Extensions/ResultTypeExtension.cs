using AnimeSearch.Core.Models;
using AnimeSearch.Core.Models.Api;

namespace AnimeSearch.Core.Extensions;

public static class ResultTypeExtension
{
    public static string ToTypeString(this ResultType type) => type switch
    {
        ResultType.Anime => "Animation",
        ResultType.Movies => "movie",
        ResultType.Series => "tv",
        _ => null
    };
}