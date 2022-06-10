using System;
using System.Threading.Tasks;

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

        public async Task UpdateLocalisation()
        {
            if (string.IsNullOrWhiteSpace(Adresse_IP))
                return;

            try
            {
                var localisation = await Utilities.GetAndDeserialiseAnonymousFromUrl("http://ip-api.com/json/" + Adresse_IP, new
                {
                    country = string.Empty,
                    regionName = string.Empty,
                    city = string.Empty,
                    status = string.Empty
                }, null, TimeSpan.FromSeconds(3));

                if (localisation != null && localisation.status == "success")
                    Localisation = localisation.country + "/" + localisation.regionName + "/" + localisation.city;
            }
            catch (Exception e)
            {
                Utilities.AddExceptionError("IP-Localisation", e);
            }
        }
    }
}
