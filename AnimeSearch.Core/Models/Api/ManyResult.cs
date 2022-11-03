namespace AnimeSearch.Core.Models.Api;

public class ManyResult
{
    public Result[] Results { get; set; }
    public int TotalPage { get; set; }
    public int Page { get; set; }
}