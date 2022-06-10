using AnimeSearch.Database;
using AnimeSearch.Models;
using AnimeSearch.Models.Sites;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

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
                foreach (Sites site in await _database.Sites.Where(s => s.Etat > EtatSite.NON_VALIDER).ToArrayAsync())
                {
                    try
                    {
                        Search s = site.PostValues != null && site.PostValues.Count > 0 ? new SiteDynamiquePost(site) : new SiteDynamiqueGet(site);

                        s.SearchResult = await s.SearchAsync("one piece");

                        if (s.GetNbResult() < 0) // < 0 = erreur (404/cloudflare)
                        {
                            if (site.Etat == EtatSite.VALIDER) // site l'etat est non valide, on ne change rien pour pas override les changement manuels.
                            {
                                site.Etat = string.IsNullOrWhiteSpace(s.SearchResult) || !s.SearchResult.ToLower().Contains("cloudflare") ? EtatSite.ERREUR_404 : EtatSite.ERREUR_CLOUDFLARE;
                            }
                        }
                        else if (site.Etat != EtatSite.VALIDER)  // on ne touche pas aux éléments déjà valide
                        {
                            site.Etat = EtatSite.VALIDER;
                        }
                    }
                    catch (Exception e) // ne devrais pas arriver puisque les erreurs de recherches sont gérées.
                    {
                        Utilities.AddExceptionError($"CheckSite avec {site.Title}", e);
                        site.Etat = EtatSite.ERREUR_CLOUDFLARE; // erreur par défault en cas de refus complet de connexions
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
