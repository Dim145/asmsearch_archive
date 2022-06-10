namespace AnimeSearch.Models
{
    public class TheMovieDBGenres
    {
        public string Name { get; set; }
        public int Id { get; set; }

        public override string ToString() => Id + ":" + Name;
    }
}
