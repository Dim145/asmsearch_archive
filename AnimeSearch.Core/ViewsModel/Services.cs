namespace AnimeSearch.Core.ViewsModel;

public class Services
{
    public string Name { get; set; }
    public string Desc { get; set; }
    public bool IsRunning { get; set; }
    public string Id { get; set; }
    public TimeSpan? TimeBeforeNext { get; set; }
}