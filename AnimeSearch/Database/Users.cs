using System;
using System.Collections.Generic;

namespace AnimeSearch.Database
{
    public partial class Users
    {
        public Users()
        {
            this.IPs = new HashSet<IP>();
            this.Recherches = new HashSet<Recherche>();
            this.Citations = new HashSet<Citations>();
            this.Dons = new HashSet<Don>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Navigateur { get; set; }
        public DateTime? Derniere_visite { get; set; }
        public DateTime? Dernier_Acces_Admin { get; set; }

        public virtual ICollection<IP> IPs { get; set; }
        public virtual ICollection<Recherche> Recherches { get; set; }
        public virtual ICollection<Citations> Citations { get; set; }
        public virtual ICollection<Don> Dons { get; set; }
    }
}
