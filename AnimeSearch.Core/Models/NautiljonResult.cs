using HtmlAgilityPack;

namespace AnimeSearch.Core.Models;

public class NautiljonResult
{
    public Uri UriDoc { get; set; }
    public Uri[] Urls { get; set; }
    public HtmlDocument HtmlDoc { get; set; }
    public List<Uri> BandeAnnonces { get; } = new();
    public List<Uri> PosterLink { get; } = new();
}