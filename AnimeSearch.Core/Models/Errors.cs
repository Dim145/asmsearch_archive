namespace AnimeSearch.Core.Models;

public class Errors
{
    public string Zone { get; set; }
    public Exception Exception { get; set; }
    public DateTime Date { get; set; }
    public string HtmlResponse { get; set; }
    public string UserName { get; set; }

    public override string ToString()
    {
        return $"Erreur dans {Zone} à {Date:dd/MM/yyyy HH:mm:ss}:\n{Exception.Message}";
    }
}