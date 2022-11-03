using AnimeSearch.Core.Models;
using AnimeSearch.Core.Models.Api;

namespace AnimeSearch.Core.ViewsModel;

public class ModelAPI
{
    public string Search { get; set; }
    public Result Result { get; set; }
    public string InfoLink { get; set; }
    public string Bande_Annone { get; set; }

    public Dictionary<string, ModelSearchResult> SearchResults { get; } = new();
}