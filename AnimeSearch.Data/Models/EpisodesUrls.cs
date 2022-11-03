namespace AnimeSearch.Data.Models;

public class EpisodesUrls
{
    public int ApiId { get; set; }
    public int SearchId { get; set; }
    public int SeasonNumber { get; set; }
    public int EpisodeNumber { get; set; }
    public Uri Url { get; set; }
    public bool Valid { get; set; } = true;
    
    public virtual ApiObject Api { get; set; }
}