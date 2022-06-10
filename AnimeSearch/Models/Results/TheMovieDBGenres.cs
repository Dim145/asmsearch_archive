namespace AnimeSearch.Models.Results
{
    public class TheMovieDbGenres
    {
        public string Name { get; set; }
        public int Id { get; set; }

        public override string ToString() => Id + ":" + Name;
    }
}
