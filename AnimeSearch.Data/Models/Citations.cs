namespace AnimeSearch.Data.Models;

public class Citations: IEquatable<Citations>
{
    public int Id { get; set; }
    public string AuthorName { get; set; }
    public string Contenue { get; set; }
    public int? UserId { get; set; }
    public bool IsValidated { get; set; }

    public bool IsCurrent { get; set; } = false;
    
    public DateTime? DateAjout { get; set; }

    public virtual Users User { get; set; }

    public bool Equals(Citations other)
    {
        return Equals((object) other);
    }
        
    public override bool Equals(object other)
    {
        if (other is not Citations c) return false;
            
        if (string.IsNullOrWhiteSpace(AuthorName) || string.IsNullOrWhiteSpace(Contenue))
            return false;

        if (string.IsNullOrWhiteSpace(c.Contenue) || string.IsNullOrWhiteSpace(c.AuthorName))
            return false;

        return AuthorName == c.AuthorName && Contenue == c.Contenue;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public static bool operator ==(Citations c1, Citations c2) => ReferenceEquals(c1, c2) || (object) c1 != null && c1.Equals(c2);
    public static bool operator !=(Citations c1, Citations c2) => !ReferenceEquals(c1, c2) && ((object) c1 == null || !c1.Equals(c2));
}