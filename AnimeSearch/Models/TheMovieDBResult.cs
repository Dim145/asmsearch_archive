using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AnimeSearch.Models
{
    public class TheMovieDBResult : Result
    {
        private static readonly HttpClient client = new();
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
        public Uri Poster_path { get => image;
            set => SetImage(value == null ? null : new Uri("https://www.themoviedb.org/t/p/w600_and_h900_bestv2" + value.OriginalString)); }
        public bool Adult { get; set; }

        public int[] Genre_ids
        {
            get => null; set
            {
                if(value != null && value.Length > 0)
                {
                    string[] genres = Utilities.TMBD_GENRES.Where(p => value.Contains(p.Id)).Select(p => p.Name).ToArray();

                    AddGenre(genres);
                }
            }
        }

        public TheMovieDBGenres[] Genres
        {
            get => null; set
            {
                if (value == null)
                    return;

                List<string> genres = new();

                foreach (TheMovieDBGenres genre in value)
                    if(genre.Name != null)
                        genres.Add(genre.Name);

                AddGenre(genres);
            }
        }

        public string Status { get => GetStatus(); set => SetStatus(value); }

        private string keyWords = null;
        private readonly Task keyWordTask;

        public TheMovieDBResult()
        {
            keyWordTask = Task.Run(async () =>
            {
                await Task.Delay(100);

                HttpResponseMessage response = await client.GetAsync("https://api.themoviedb.org/3/" + Media_type + "/" + Id + "/keywords?api_key=" + Utilities.MOVIEDB_API_KEY);

                if (response.IsSuccessStatusCode)
                {
                    var val = JsonConvert.DeserializeAnonymousType(await response.Content.ReadAsStringAsync(), new
                    {
                        id = 0,
                        results = Array.Empty<Dictionary<string, object>>(),
                        keywords = Array.Empty<Dictionary<string, object>>()
                    });

                    if (val != null && (val.results != null || val.keywords != null))
                    {
                        keyWords = string.Join(",", (val.results ?? val.keywords).Select(dic => dic.GetValueOrDefault("name")).Where(str => str != null)).ToLowerInvariant();

                        if (keyWords.Contains("hentai"))
                        {
                            if (!GetGenres().Contains("hentai"))
                                AddGenre(new string[] { "hentai" });
                        }
                    }
                }
            });
        }

        public bool IsHentai()
        {
            if (keyWords == null)
                keyWordTask.Wait();

            if (GetGenres().Contains("hentai"))
                return true;

            return Adult && IsAnime();
        }

        public bool IsFilmAnimation()
        {
            return IsFilm() && GetGenres().Contains("Animation");
        }
    }
}
