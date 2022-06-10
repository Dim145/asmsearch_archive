using System;
using System.Collections.Generic;
using System.Globalization;

namespace AnimeSearch.Models
{
    public class TVMazeResult: Result
    {
        private Dictionary<string, Uri> images = new();

        public int Id { get => id; set => SetId(value); }

        public string Name { get => name; set => SetName(value); }
        public string Type { get => type; set => SetTypeResult(value); }

        public Dictionary<string, Uri> Image { get => images; set 
            {
                if(value != null && value.GetValueOrDefault("original") != null)
                    this.SetImage(value.GetValueOrDefault("original"));

                this.images = value;
            } 
        }

        public CultureInfo Language { get => language; set => SetLanguage(value); }
        public Uri    Url { get => url; set => SetUrl(value); }

        public Dictionary<CultureInfo, List<string>> OthersName { get => this.otherNames; }

        public DateTime? Premiered { get => realeaseDate; set => SetRealeaseDate(value); }

        public string[] Genres { get => GetGenres(); set => AddGenre(value); }

        public string Status { get => GetStatus(); set => SetStatus(value); }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
