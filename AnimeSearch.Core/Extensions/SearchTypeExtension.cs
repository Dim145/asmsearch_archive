using AnimeSearch.Core.Models.Api;
using AnimeSearch.Core.ViewsModel;

namespace AnimeSearch.Core.Extensions;

public static class SearchTypeExtension
{
    public static ResultType? ToResultType(this SearchType type) => type switch
    {
        SearchType.Movies => ResultType.Movies,
        SearchType.Series => ResultType.Series,
        SearchType.All => ResultType.All,
        _ => null
    };
}