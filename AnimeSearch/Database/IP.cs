using System;

namespace AnimeSearch.Database
{
    public partial class IP
    {
        public int Id { get; set; }
        public string Adresse_IP { get; set; }
        public int Users_ID { get; set; }
        public DateTime? Derniere_utilisation { get; set; }
        public string Localisation { get; set; }

        public virtual Users User { get; set; }
    }
}
