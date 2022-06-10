using AnimeSearch.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AnimeSearch.Database
{
    public class Sites
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public string Url { get; set; }
        public string UrlSearch { get; set; }
        [Required]
        public string UrlIcon { get; set; }
        [Required]
        public string CheminBaliseA { get; set; }
        public string IdBase { get; set; }
        public string TypeSite { get; set; }
        public bool Is_inter { get; set; }
        public EtatSite Etat { get; set; }
        public Dictionary<string, string> PostValues { get; set; }
    }
}
