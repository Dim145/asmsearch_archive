using AnimeSearch.Core.Models;
using AnimeSearch.Core.Models.Api;
using Microsoft.AspNetCore.Mvc;

namespace AnimeSearch.Core.ViewsModel;

public class AdvanceSearch
{
    public int Page { get; set; } = 1;
    public string[] WithGenres { get; set; }
    public string[] WithoutGenres { get; set; }
    public string Q { get; set; }
    [BindProperty]
    public DateTime? After { get; set; }
    [BindProperty]
    public DateTime? Before { get; set; }
    public Sort? SortBy { get; set; }
    public bool AndGenre { get; set; } = true;
    public SearchType SearchIn { get; set; } = SearchType.All;

    public bool IsEmpty()
    {
        return 
            (WithGenres    == null || WithGenres.Length    == 0) && 
            (WithoutGenres == null || WithoutGenres.Length == 0) && 
            After  == null && 
            Before == null && 
            string.IsNullOrWhiteSpace(Q);
    }
}