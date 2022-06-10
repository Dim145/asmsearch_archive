using AnimeSearch.Database;
using AnimeSearch.Models;
using AnimeSearch.Models.Sites;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using AnimeSearch.Models.Search;

namespace AnimeSearch.Services
{
    public class CheckSites : BaseService
    {
        public static TimeSpan FREQUENCE { get; } = TimeSpan.FromHours(2);

        private readonly AsmsearchContext _database;

        public CheckSites(AsmsearchContext database): base("CheckSite Service", FREQUENCE, "Vérifie régulièrement que les sites enregistrés dans la base de données soient accessibles.")
        {
            _database = database;
        }

        public override async Task ExecutionCode()
        {
            try
            {
                foreach (Sites site in await _database.Sites.AsNoTracking().Where(s => s.Etat > EtatSite.NON_VALIDER).ToArrayAsync())
                {
                    try
                    {
                        Search s = site.PostValues != null && site.PostValues.Count > 0 ? new SiteDynamiquePost(site) : new SiteDynamiqueGet(site);

                        var response = await s.SearchAsync(s.GetTypeSite().Contains("séries") ? "lucifer" : "one piece");

                        if (s.GetNbResult() < 0) // < 0 = erreur (404/cloudflare)
                        {
                            if (site.Etat == EtatSite.VALIDER) // site l'etat est non valide, on ne change rien pour pas override les changement manuels.
                            {
                                site.Etat = string.IsNullOrWhiteSpace(s.SearchResult) || !s.SearchResult.ToLower().Contains("cloudflare") ? EtatSite.ERREUR_404 : EtatSite.ERREUR_CLOUDFLARE;
                                _database.Sites.Update(site);
                            }
                        }
                        else
                        {                          
                            var responseURI = response.RequestMessage.RequestUri;

                            string responseURL = $"{responseURI.Scheme}://{responseURI.Authority}/";

                            if (responseURL != site.Url)
                            {
                                if(!await _database.Sites.AnyAsync(s => s.Url == responseURL))
                                {
                                    _database.Sites.Remove(new() { Url = site.Url });
                                    await _database.SaveChangesAsync();

                                    if (site.UrlIcon.StartsWith(site.Url))
                                        site.UrlIcon = responseURL + site.UrlIcon[site.Url.Length..];

                                    site.Url = responseURL;
                                    site.Etat = EtatSite.VALIDER;

                                    await _database.Sites.AddAsync(site);
                                }
                            }
                            else if (site.Etat != EtatSite.VALIDER)  // on ne touche pas aux éléments déjà valide
                            {
                                site.Etat = EtatSite.VALIDER;
                                _database.Sites.Update(site);
                            }
                        }
                    }
                    catch (Exception e) // ne devrais pas arriver puisque les erreurs de recherches sont gérées.
                    {
                        Utilities.AddExceptionError($"CheckSite avec {site.Title}", e);
                        site.Etat = EtatSite.ERREUR_404; // erreur par défault en cas de refus complet de connexions
                    }
                }

                await _database.SaveChangesAsync();

                Utilities.LAST_DATE_CHECKSITE_EXECUTION = DateTime.Now;
            }
            catch (Exception e) // erreur BDD ou erreur de données (post null etc...)
            {
                Utilities.AddExceptionError($"CheckSite", e);
            }
        }
    }
}
