using AnimeSearch.Core.ViewsModel;

namespace AnimeSearch.Data.Models;

public class SavedSearch
{
    public string Search { get; set; }
    public int UserId { get; set; }
    public ModelAPI Results { get; set; }
    public DateTime DateSauvegarde { get; set; }

    public virtual Users User { get; set; }

    public override bool Equals(object obj)
    {
        if (obj == null) return false;

        if (obj is SavedSearch ss)
            return ss.UserId == UserId && ss.Search == Search;

        return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}