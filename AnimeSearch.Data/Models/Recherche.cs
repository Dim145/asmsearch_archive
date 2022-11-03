using AnimeSearch.Core.Models.Search;

namespace AnimeSearch.Data.Models;

public class Recherche
{
    public int Id { get; set; }
    public int User_ID { get; set; }
    public string recherche { get; set; }
    public int Nb_recherches { get; set; }
    public DateTime? Derniere_Recherche { get; set; }
    public SearchSource Source { get; set; }

    public virtual Users User { get; set; }

    public Recherche Clone()
    {
        Recherche r = new();

        _ = GetType().GetProperties().Select(p => { p.SetValue(r, p.GetValue(this)); return true; }).ToArray();

        return r;
    }
}