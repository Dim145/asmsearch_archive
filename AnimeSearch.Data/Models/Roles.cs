using System.Drawing;
using Microsoft.AspNetCore.Identity;

namespace AnimeSearch.Data.Models;

public class Roles: IdentityRole<int>
{
    public int NiveauAutorisation { get; set; }

    public Color Color { get; set; }

    public Roles(string name) : this(name, 1) { }

    public Roles(string name, int niveauAcce = 1): base(name)
    {
        NiveauAutorisation = niveauAcce;
    }

    public override bool Equals(object obj)
    {
        if(obj is not null and Roles role)
            return Name == role.Name && NiveauAutorisation == role.NiveauAutorisation;

        return false;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode() + NiveauAutorisation.GetHashCode();
    }
}