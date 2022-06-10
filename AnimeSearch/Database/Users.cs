using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace AnimeSearch.Database
{
    public sealed class Users: IdentityUser<int>
    {
        public Users()
        {
            IPs = new HashSet<IP>();
            Recherches = new HashSet<Recherche>();
            Citations = new HashSet<Citations>();
            Dons = new HashSet<Don>();
            SavedSearch = new HashSet<SavedSearch>();
        }

        public string Navigateur { get; set; }
        
        [PersonalDataAttribute]
        public DateTime? Derniere_visite { get; set; }
        public DateTime? Dernier_Acces_Admin { get; set; }

        public ICollection<IP> IPs { get; set; }
        public ICollection<Recherche> Recherches { get; set; }
        public ICollection<Citations> Citations { get; set; }
        public ICollection<Don> Dons { get; set; }
        public ICollection<SavedSearch> SavedSearch { get; set; }
    }
}
