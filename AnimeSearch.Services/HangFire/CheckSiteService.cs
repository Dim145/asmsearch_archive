using AnimeSearch.Core;
using AnimeSearch.Data;
using AnimeSearch.Data.Models;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace AnimeSearch.Services.HangFire;

public class CheckSiteService: IHangFireService
{
    private AsmsearchContext Database { get; }

    public CheckSiteService(AsmsearchContext database)
    {
        Database = database;
    }

    public string GetDescription() => "Vérifie régulièrement que les sites enregistrés dans la base de données soient accessibles.";

    public string GetCron()
    {
        return $"0 */{2} * * *"; // toute les deux heures
    }

    public async Task Execute()
    {
        try
        {
            foreach (Sites site in await Database.Sites.AsNoTracking().Where(s => s.Etat > EtatSite.NON_VALIDER).ToArrayAsync())
            {
                try
                {
                    var s = site.ToDynamicSite(site.PostValues is {Count: > 0});

                    var response = await s.SearchAsync(s.GetTypeSite().Contains("séries") ? "lucifer" : "one piece");

                    if (s.GetNbResult() < 0) // < 0 = erreur (404/cloudflare)
                    {
                        if (site.Etat == EtatSite.VALIDER) // site l'etat est non valide, on ne change rien pour pas override les changement manuels.
                        {
                            site.Etat = string.IsNullOrWhiteSpace(s.SearchResult) || !s.SearchResult.ToLower().Contains("cloudflare") ? EtatSite.ERREUR_404 : EtatSite.ERREUR_CLOUDFLARE;
                            Database.Sites.Update(site);
                        }
                    }
                    else
                    {                          
                        var responseURI = response.RequestMessage.RequestUri;

                        string responseURL = $"{responseURI.Scheme}://{responseURI.Authority}/";

                        if (responseURL != site.Url)
                        {
                            if(!await Database.Sites.AnyAsync(s => s.Url == responseURL))
                            {
                                Database.Sites.Remove(new() { Url = site.Url });
                                await Database.SaveChangesAsync();

                                if (site.UrlIcon.StartsWith(site.Url))
                                    site.UrlIcon = responseURL + site.UrlIcon[site.Url.Length..];

                                site.Url = responseURL;
                                site.Etat = EtatSite.VALIDER;

                                await Database.Sites.AddAsync(site);
                            }
                        }
                        else if (site.Etat != EtatSite.VALIDER)  // on ne touche pas aux éléments déjà valide
                        {
                            site.Etat = EtatSite.VALIDER;
                            Database.Sites.Update(site);
                        }
                    }
                }
                catch (Exception e) // ne devrais pas arriver puisque les erreurs de recherches sont gérées.
                {
                    CoreUtils.AddExceptionError($"CheckSite avec {site.Title}", e);
                    site.Etat = EtatSite.ERREUR_404; // erreur par défault en cas de refus complet de connexions
                }
            }

            await Database.SaveChangesAsync();
        }
        catch (Exception e) // erreur BDD ou erreur de données (post null etc...)
        {
            CoreUtils.AddExceptionError($"CheckSite", e);
            throw;
        }
    }
}