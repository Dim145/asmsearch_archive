using AnimeSearch.Core.Models;

namespace AnimeSearch.Core.Extensions;

public static class NautiljonFilterExtension
{
    public static string ToUrlParams(this NautiljonFilter filter) => filter switch
    {
        NautiljonFilter.Anime => "animes/?formats_include%5B%5D=1&encourss_exclude%5B%5D=6&encourss_exclude%5B%5D=7&q=",
        NautiljonFilter.FilmAnimation => "animes/?formats_include%5B%5D=3&formats_include%5B%5D=8&q=",
        NautiljonFilter.None => "animes/?q="
    };
}