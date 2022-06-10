using System.Collections.Generic;

namespace AnimeSearch.Models
{
    public class ModelAPI
    {
        public string Search { get; set; }
        public Result Result { get; set; }
        public string InfoLink { get; set; }
        public string Bande_Annone { get; set; }

        public Dictionary<string, ModelSearchResult> SearchResults = new();
    }
}
