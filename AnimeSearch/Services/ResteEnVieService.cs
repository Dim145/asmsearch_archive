using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AnimeSearch.Services
{
    /// <summary>
    ///     Ce service permet au serveur de rester actif. Dans des serveur gratuit et/ou hébergé par un tier, les servers sont eteint après un vertains temps d'inactivité.
    /// </summary>
    public class ResteEnVieService : BaseService
    {
        private HttpClient Client { get; }

        public ResteEnVieService(): base("Reste en vie service", TimeSpan.FromMinutes(2.5), "Exécute une requête vers la page d'accueil toutes les 2-3 minutes. Cela permet de maintenir le serveur actif plus longtemps.")
        {
            Client = new();
        }

        public override async Task ExecutionCode()
        {
            try
            {
                HttpResponseMessage res = await Client.GetAsync(Utilities.BASE_URL + "api");
            }
            catch (Exception e)
            {
                Utilities.AddExceptionError("ResteEnvieService", e);
            }
        }
    }
}
