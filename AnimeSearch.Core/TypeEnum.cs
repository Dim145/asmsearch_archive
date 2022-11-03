using System.Reflection;

namespace AnimeSearch.Core;

public class TypeEnum
{
    public const string ANIMES_DL                = "Download (animes)";
    public const string SERIES_DL                = "Download (séries)";
    public const string ANIMES_STREAM            = "Streaming (animes)";
    public const string SERIES_STREAM            = "Streaming (séries)";
    public const string ANIME_FA_STREAM          = "Streaming (animes/FA)";
    public const string ANIME_FA_DL              = "Download (animes/FA)";
    public const string ANIMES_DL_TORRENTS       = "Download (animes/Torrents)";
    public const string SERIES_DL_TORRENTS       = "Download (séries/Torrents)";
    public const string ANIMES_DL_STREAM         = "Download/Streaming (animes)";
    public const string SERIES_DL_STREAM         = "Download/Streaming (séries)";
    public const string ANIME_SERIES_DL_STREAM   = "Download/Streaming (animes/séries)";
    public const string ANIME_SERIES_STREAM      = "Streaming (animes/séries)";
    public const string FILM_STREAM              = "Streaming (films)";
    public const string FILM_DL                  = "Download (films)";
    public const string FILM_SERIES_STREAM       = "Streaming (films/séries)";
    public const string FILM_SERIES_DL           = "Download (films/séries)";
    public const string FILM_ANIME_SERIES_STREAM = "Streaming (all)";
    public const string FILM_ANIME_SERIES_DL     = "Download (all)" ;
    public const string HENTAI_STREAM            = "Streaming (hentai)";
    public const string HENTAI_DL                = "Download (hentai)";
    public const string HENTAI_ALL               = "Download/Streaming (hentai)";

    public static string[] TabTypes => typeof(TypeEnum).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
        .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
        .Select(x => x.GetRawConstantValue().ToString()).ToArray();
}