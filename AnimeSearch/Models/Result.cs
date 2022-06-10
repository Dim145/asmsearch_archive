using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AnimeSearch.Models
{
    public class Result
    {
        public static readonly CultureInfo ENGLISH = new("en");

        protected int id;
        protected string name;
        protected string type;
        protected Uri url;
        protected Uri image;
        protected CultureInfo language;
        protected DateTime realeaseDate;
        protected Dictionary<CultureInfo, List<string>> otherNames;
        protected List<string> genres;
        protected string status;

        public Result()
        {
            this.otherNames = new();
        }

        public int GetId() => this.id;
        public void SetId(int id) => this.id = id;

        public string GetName() => this.name;
        public void SetName(string name) => this.name = name;

        public string GetTypeResult() => this.type;
        public void SetTypeResult(string type) => this.type = type;

        public Uri GetUrl() => this.url;
        public void SetUrl(Uri url) => this.url = url;

        public Uri GetImage() => this.image;
        public void SetImage(Uri url) => this.image = url;

        public CultureInfo GetLanguage() => this.language;
        public void SetLanguage(CultureInfo language) => this.language = language;

        public DateTime? GetRealeaseDate() => this.realeaseDate;
        public void SetRealeaseDate(DateTime? date)
        {
            if (date == null) return;

            this.realeaseDate = (DateTime) date;
        }

        public void AddOtherName(CultureInfo lang, string name)
        {
            if (lang == null)
                lang = ENGLISH;

            List<string> list = this.otherNames.GetValueOrDefault(lang);

            if (list == null)
            {
                list = new();

                this.otherNames.Add(lang, list);
            }

            list.Add(name);
        }

        public void AddOtherName(string name)
        {
            this.AddOtherName(this.language, name);
        }

        public void AddOtherNames(CultureInfo lang, List<string> names)
        {
            foreach(string name in names)
                this.AddOtherName(lang, name);
        }

        public string GetOtherName()
        {
            if (this.language != null && this.otherNames.ContainsKey(this.language))
                return this.otherNames.GetValueOrDefault(this.language).FirstOrDefault();

            return this.otherNames.GetValueOrDefault(ENGLISH)?.FirstOrDefault();
        }

        public List<string> GetOtherNames(CultureInfo lang)
        {
            return this.otherNames.GetValueOrDefault(lang);
        }

        public Dictionary<CultureInfo, List<string>> GetAllOtherNames()
        {
            return this.otherNames;
        }

        public void AddNameInLanguage()
        {
            bool isPresent = false;

            foreach (List<string> l in this.otherNames.Values)
                if (l.Contains(this.name))
                    isPresent = true;

            if (!isPresent)
            {
                List<string> listEn = this.otherNames.ContainsKey(ENGLISH) ? this.otherNames.GetValueOrDefault(ENGLISH) : null;

                if (listEn == null)
                {
                    listEn = new();

                    this.otherNames.Add(ENGLISH, listEn);
                }

                listEn.Add(this.name);
            }
        }

        public IEnumerable<string> GetAllOtherNamesList()
        {
            List<string> list = new();

            foreach (List<string> l in this.otherNames.Values)
                list.AddRange(l);

            return list;
        }

        public IEnumerable<string> GetAllOtherNamesList(List<CultureInfo> ordrePref)
        {
            if (ordrePref == null || ordrePref.Count == 0)
                return this.GetAllOtherNamesList();

            List<string> list = new();

            // Ajoute dans l'ordre
            foreach (CultureInfo cu in ordrePref)
                if (this.otherNames.ContainsKey(cu))
                    list.AddRange(this.otherNames.GetValueOrDefault(cu));

            //Ajoute ceux qu'il reste
            foreach (List<string> l in this.otherNames.Values)
                foreach (string s in l)
                    if (!list.Contains(s))
                        list.Add(s);

            return list;
        }

        public string[] GetGenres()
        {
            return this.genres?.ToArray();
        }

        public void AddGenre(IEnumerable<string> genres)
        {
            if (this.genres == null)
                this.genres = new();

            this.genres.AddRange(genres);
        }

        public void SetStatus(string status)
        {
            this.status = status;
        }

        public string GetStatus()
        {
            return this.status;
        }

        public bool IsAnime()
        {
            return GetTypeResult() == "Animation" || GetTypeResult() == "tv";
        }

        public bool IsSerie()
        {
            return GetTypeResult() == "Scripted" || GetTypeResult() == "tv";
        }

        public bool IsFilm()
        {
            return GetTypeResult() == "movie";
        }

        public override bool Equals(object obj)
        {
            if (obj is Result other)
            {
                return other != null && GetName().ToLowerInvariant() == other.GetName().ToLowerInvariant();
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
