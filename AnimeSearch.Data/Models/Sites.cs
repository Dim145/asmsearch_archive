using System.ComponentModel.DataAnnotations;
using AnimeSearch.Core;
using AnimeSearch.Core.Models.Search;
using AnimeSearch.Core.Models.Sites;

namespace AnimeSearch.Data.Models;

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
    public int NbClick { get; set; } = 0;
    public string CheminToNbResult { get; set; }

    public Search ToDynamicSite(bool isPost, HttpClient client = null)
    {
        return isPost ? 
            new SiteDynamiquePost(Title, Url, UrlSearch ?? "", UrlIcon, CheminBaliseA, PostValues, IdBase, TypeSite, Is_inter, client, CheminToNbResult) : 
            new SiteDynamiqueGet(Title, Url, UrlSearch, UrlIcon, CheminBaliseA, IdBase, TypeSite, Is_inter, client, CheminToNbResult);
    }
}