using AnimeSearch.Core.Models;

namespace AnimeSearch.Core.Extensions;

public static class SortExtension
{
    public static string ToTitle(this Models.Api.Sort sort)
    {
        return sort switch
        {
            Models.Api.Sort.DateA => "Date (+ récent en haut)",
            Models.Api.Sort.DateD => "Date (+ vieux en haut)",
            Models.Api.Sort.TitleA => "Titre (Alphabétique a-z)",
            Models.Api.Sort.TitleD => "Titre (Alphabétique z-a)",
            Models.Api.Sort.PopularityA => "Popularité (+ pop. en haut)",
            Models.Api.Sort.PopularityD => "Popularité (- pop. en haut)",
            _ => string.Empty
        };
    }
}