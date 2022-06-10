using AnimeSearch.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AnimeSearch.Services
{
    public class DonsTimeoutService : BaseService
    {
        private readonly AsmsearchContext _database;

        private DateTime heureDernierPassage;

        public DonsTimeoutService(AsmsearchContext database) : base("Dons TimeOut Service", TimeSpan.FromMinutes(30), "Vérirife que les dons sont bien validés au bout de 30 minutes, les supprime le cas échéant.")
        {
            _database = database;

            Utilities.LAST_DONS_SERVICE = this;
        }

        public override async Task ExecutionCode()
        {
            // séparé en deux where car la première est traduit en SQL et la seconde condition est trop complexe pour être traduite.
            // La seconde condition s'applique donc sur le tableau directement après la récupération
            _database.Dons.RemoveRange((await _database.Dons.Where(d => !d.Done).ToArrayAsync()).Where(d => DateTime.Now.Subtract(d.Date) >= Periode * 2));
            await _database.SaveChangesAsync();

            heureDernierPassage = DateTime.Now;
        }

        public TimeSpan GetTempsProchainPassage()
        {
            return Periode - DateTime.Now.Subtract(heureDernierPassage);
        }
    }
}
