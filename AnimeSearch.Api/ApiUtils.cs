using AnimeSearch.Core.Models.Search;
using AnimeSearch.Data;
using Microsoft.EntityFrameworkCore;

namespace AnimeSearch.Api;

public static class ApiUtils
{
    public static string GUEST => DataUtils.Guest;
    
    public static string BALISE_SCRIPT_DEBUT => "<script";
    public static string BALISE_SCRIPT_FIN => "</script>";
    public static string BALISE_LINK_DEBUT => "<link";
    public static string BALISE_LINK_FIN => ">";
    
    public static async Task CreateOrUpdateSearch(string username, string search, AsmsearchContext _database, SearchSource source = SearchSource.Api)
    {
        var user = await _database.Users.FirstOrDefaultAsync(u => u.UserName == (username ?? GUEST));

        var r = await _database.Recherches.FirstOrDefaultAsync(r => r.User_ID == user.Id && r.recherche == search);

        if (r == null)
        {
            r = new()
            {
                User_ID = user.Id,
                recherche = search,
                Nb_recherches = 1,
                User = user,
                Derniere_Recherche = DateTime.Now,
                Source = source
            };

            await _database.Recherches.AddAsync(r);
        }
        else
        {
            r.Source = source;
            r.Derniere_Recherche = DateTime.Now;
            r.Nb_recherches++;

            _database.Recherches.Update(r);
        }

        await _database.SaveChangesAsync();
    }
}