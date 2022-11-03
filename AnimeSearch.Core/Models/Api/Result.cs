using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimeSearch.Core.Models.Api;

public class Result
{
    private static readonly CultureInfo English = new("en");

    private DateTime _releaseDate;

    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public Uri Url { get; set; }
    public Uri Image { get; set; }
    public CultureInfo Language { get; set; }

    public DateTime? ReleaseDate
    {
        get => _releaseDate == default ? null : _releaseDate;
        set
        {
            if (value == null) return;

            _releaseDate = value.Value;
        }
    }

    public Dictionary<CultureInfo, List<string>> OtherNames { get; set; }
    public List<string> Genres { get; set; }
    public string Status { get; set; }
    public string Description { get; set; }
    public float? Popularity { get; set; }
    public bool Adult { get; set; }

    [JsonIgnore] public int IdApiFrom { get; set; }

    public string OtherName { 
        get => OtherNames?. GetValueOrDefault(Language != null && OtherNames.ContainsKey(Language)? Language : English)?.FirstOrDefault();
        set => AddOtherName(value);
    }

    public bool IsAnime => Type is "Animation" || (Type is "tv" && (GetGenres()?.Contains("Animation")).GetValueOrDefault(false));
    public bool IsSerie => Type is "Scripted" or "tv";
    public bool IsFilm => Type is "movie";
    public bool IsHentai => (GetGenres()?.Contains("hentai")).GetValueOrDefault(false) || Adult && IsAnime;
    
    public Dictionary<string, JToken> AddtionnalFields { get; set; }

    public Result() => OtherNames = new();

    public void AddOtherName(CultureInfo lang, string name)
    {
        if (lang == null)
            lang = English;

        List<string> list = OtherNames.GetValueOrDefault(lang);

        if (list == null)
        {
            list = new();

            OtherNames.Add(lang, list);
        }

        list.Add(name);
    }

    public void AddOtherName(string name)
    {
        AddOtherName(Language, name);
    }

    public void AddOtherNames(CultureInfo lang, List<string> names)
    {
        foreach(string name in names)
            AddOtherName(lang, name);
    }

    public List<string> GetOtherNames(CultureInfo lang) => OtherNames.GetValueOrDefault(lang);

    public void AddNameInLanguage()
    {
        bool isPresent = false;

        foreach (List<string> l in OtherNames.Values)
            if (l.Contains(Name))
                isPresent = true;

        if (!isPresent)
        {
            List<string> listEn = OtherNames.ContainsKey(English) ? OtherNames.GetValueOrDefault(English) : null;

            if (listEn == null)
            {
                listEn = new();

                OtherNames.Add(English, listEn);
            }

            listEn.Add(Name);
        }
    }

    public IEnumerable<string> GetAllOtherNamesList()
    {
        List<string> list = new();

        foreach (var l in OtherNames.Values)
            list.AddRange(l);

        return list;
    }

    public IEnumerable<string> GetAllOtherNamesList(List<CultureInfo> ordrePref)
    {
        if (ordrePref == null || ordrePref.Count == 0)
            return GetAllOtherNamesList();

        List<string> list = new();

        // Ajoute dans l'ordre
        foreach (var cu in ordrePref.Where(cu => OtherNames.ContainsKey(cu)))
            list.AddRange(OtherNames[cu]);

        //Ajoute ceux qu'il reste
        foreach (var s in OtherNames.Values.SelectMany(l => l.Where(s => !list.Contains(s))))
            list.Add(s);

        return list;
    }
        
    public string[] GetGenres()
    {
        return Genres?.ToArray();
    }

    public void AddGenre(params string[] genres)
    {
        Genres ??= new();

        Genres.AddRange(genres);
    }

    public override bool Equals(object obj)
    {
        if (obj is Result other)
        {
            return string.Equals(Name, other.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public bool IsFilmAnimation()
    {
        var genres = GetGenres();

        return IsFilm && genres != null && genres.Contains("Animation");
    }

    public bool Is(ResultType type) => type switch
    {
        ResultType.Anime => IsAnime,
        ResultType.Movies => IsFilm,
        ResultType.Series => IsSerie,
        ResultType.All => true,
        _ => false
    };

    public ResultType ToSearchType() => Enum.GetValues<ResultType>().LastOrDefault(Is);
}