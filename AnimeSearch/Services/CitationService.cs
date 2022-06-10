using AnimeSearch.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AnimeSearch.Services
{
    public class CitationService : BaseService
    {
        private readonly AsmsearchContext _database;

        public CitationService(AsmsearchContext DataBase): base("Citations Services", TimeSpan.FromDays(1), "Sélectionne tous les jours une nouvelle citation aléatoirement.")
        {
            _database = DataBase;
        }

        public override async Task ExecutionCode()
        {
            try
            {
                Citations c = null;

                Citations[] citations = await _database.Citations.Where(c => c.IsValidated).ToArrayAsync();

                do
                {
                    c = citations[Utilities.RANDOM.Next(citations.Length)];
                }
                while (c == Utilities.CITATION_DU_JOUR);

                Utilities.CITATION_DU_JOUR = c;
            }
            catch (Exception e)
            {
                Utilities.AddExceptionError("CitationsService:", e);
            }
        }
    }
}
