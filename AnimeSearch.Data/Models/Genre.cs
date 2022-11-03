using AnimeSearch.Core.ViewsModel;

namespace AnimeSearch.Data.Models;

public class Genre
{
    public string Id { get; set; }
    public int ApiId { get; set; }
    
    public string Name { get; set; }
    public SearchType Type { get; set; } = SearchType.All;
    
    public virtual ApiObject Api { get; set; }

    public override string ToString() => Id + ":" + Name;
}