using System;

namespace AnimeSearch.Database
{
    public class Citations: IEquatable<Citations>
    {
        public int Id { get; set; }
        public string AuthorName { get; set; }
        public string Contenue { get; set; }
        public int? UserId { get; set; }
        public bool IsValidated { get; set; }
        public DateTime? DateAjout { get; set; }

        public virtual Users User { get; set; }

        public bool Equals(Citations other)
        {
            if (other == null || string.IsNullOrWhiteSpace(AuthorName) || string.IsNullOrWhiteSpace(Contenue))
                return false;

            if (string.IsNullOrWhiteSpace(other.Contenue) || string.IsNullOrWhiteSpace(other.AuthorName))
                return false;

            return AuthorName == other.AuthorName && Contenue == other.Contenue;
        }

        public static bool operator ==(Citations c1, Citations c2) => ((object) c1) == c2 || ((object)c1) != null && c1.Equals(c2);
        public static bool operator !=(Citations c1, Citations c2) => ((object)c1) == null && ((object)c2) != null || ((object)c1) != null && ((object)c2) == null || !(((object)c1) == null && ((object)c2) == null) || ((object)c1) != c2 && !c1.Equals(c2);
    }
}
