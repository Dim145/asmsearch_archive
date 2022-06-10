using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AnimeSearch.Models.Results
{
    public class TheMovieDbResult : Result
    {
        private TheMovieDbGenres[] genres_tmp = null;

        public int Id { get => id; set => this.SetId(value); }
        public string Name { get => name; set => this.SetName(value); }
        public string Title { get => name; set => this.SetName(value); }
        public CultureInfo Original_language { get => language; set => SetLanguage(value); }
        public string Original_title { get => GetOtherName(); set => AddOtherName(value); }
        public string Original_Name { get => GetOtherName(); set => AddOtherName(value); }
        public string Media_type { get => type; set => this.SetTypeResult(value); }
        public Dictionary<CultureInfo, List<string>> OthersName { get => this.otherNames; }
        public DateTime? Release_date { get => realeaseDate; set => SetRealeaseDate(value); }
        public DateTime? First_air_date { get => realeaseDate; set => SetRealeaseDate(value); }
        public string Overview { get => GetDescription(); set => SetDescription(value); }
        public float Popularity { get => GetPopularity(); set => SetPopularity(value); }
        public Uri Poster_path { get => image;
            set => SetImage(value == null ? null : new Uri((value.ToString().StartsWith(Utilities.BASE_URL_IMAGE_TMDB) ? "" : Utilities.BASE_URL_IMAGE_TMDB) + value.OriginalString)); }
        public bool Adult { get; set; }

        public int[] Genre_ids
        {
            get => null; set
            {
                if(value != null && value.Length > 0)
                {
                    TheMovieDbGenres[] genres = Utilities.TMBD_GENRES.Where(p => value.Contains(p.Id)).ToArray();

                    genres_tmp = genres;

                    AddGenre(genres.Select(p => p.Name).ToArray());
                }
            }
        }

        public TheMovieDbGenres[] Genres
        {
            get => genres_tmp; set
            {
                genres_tmp = value;

                if (value == null)
                    return;

                string[] genres = new string[value.Length];

                for (int i = 0; i < value.Length; i++)
                    if(value[i].Name != null)
                        genres[i] = value[i].Name;

                AddGenre(genres);
            }
        }

        public Uri Url { get => url; set => SetUrl(value); }

        public string Status { get => GetStatus(); set => SetStatus(value); }

        private string keyWords = null;
        private readonly Task keyWordTask;

        public TheMovieDbResult()
        {
            keyWordTask = Task.Run(async () =>
            {
                await Task.Delay(100);

                var val = await Utilities.GetAndDeserialiseAnonymousFromUrl("https://api.themoviedb.org/3/" + Media_type + "/" + Id + "/keywords?api_key=" + Utilities.MOVIEDB_API_KEY, new
                {
                    id = 0,
                    results = Array.Empty<Dictionary<string, object>>(),
                    keywords = Array.Empty<Dictionary<string, object>>()
                });

                if (val != null && (val.results != null || val.keywords != null))
                {
                    keyWords = string.Join(",", (val.results ?? val.keywords).Select(dic => dic.GetValueOrDefault("name")).Where(str => str != null)).ToLowerInvariant();

                    if (keyWords.Contains("hentai", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (!GetGenres().Contains("hentai"))
                            AddGenre("hentai");
                    }
                }
            });
        }

        public bool IsHentai()
        {
            if (keyWords == null)
                keyWordTask.Wait();

            var genres = GetGenres();

            if (genres != null && genres.Contains("hentai"))
                return true;

            return Adult && IsAnime();
        }

        public bool IsFilmAnimation()
        {
            var genres = GetGenres();

            return IsFilm() && genres != null && genres.Contains("Animation");
        }
    }
}
