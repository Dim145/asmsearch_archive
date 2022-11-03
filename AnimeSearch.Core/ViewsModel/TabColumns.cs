using System.Drawing;
using System.Linq.Expressions;

namespace AnimeSearch.Core.ViewsModel;

public class TabColumns<T>
{
    public string Title { get; set; }
    public Expression<Func<T, object>> Colonnes { get; set; }
    public bool Sortable { get; set; } = true;
    public bool Filterable { get; set; } = true;
    public int Width { get; set; } = 50;
    public Func<T, string> Template { get; set; }

    public bool TextCenter { get; set; } = false;

    public bool SortDefault { get; set; } = false;

    public bool DefaultSortingDescending { get; set; } = false;

    public Func<T, Color> CellColor { get; set; } = (c) => Color.Empty;
}