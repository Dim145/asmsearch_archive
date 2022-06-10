using System;

namespace AnimeSearch.Database
{
    public class Don
    {
        public Guid Id { get; set; }
        public double Amout { get; set; }
        public DateTime Date { get; set; }
        public bool Done { get; set; }
        public int User_id { get; set; }


        public virtual Users User {get; set;}
    }
}
