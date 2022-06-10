using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnimeSearch.Database;
using AnimeSearch.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AnimeSearch.Core;

public sealed class DatabaseCom
{
    public static async Task<bool> SaveRechercheResult(AsmsearchContext database, string name, ModelAPI model,
        bool isForUpdate = false)
    {
        if (database == null || string.IsNullOrWhiteSpace(name) || model == null)
            return false;

        Users user = await database.Users.FirstOrDefaultAsync(u => u.UserName == name);

        if (user == null) return false;

        SavedSearch ss = isForUpdate
            ? await database.SavedSearch.FirstOrDefaultAsync(s => s.Search == model.Search && s.UserId == user.Id)
            : new();

        if (ss == null)
        {
            ss = new();
            isForUpdate = false;
        }

        ss.Search = model.Search;
        ss.UserId = user.Id;
        ss.Results = model;
        ss.DateSauvegarde = DateTime.Now;

        try
        {
            EntityEntry entry = null;

            if (isForUpdate)
                entry = database.SavedSearch.Update(ss);
            else
                entry = await database.SavedSearch.AddAsync(ss);

            bool res = await database.SaveChangesAsync() > 0;

            entry.State =
                EntityState.Detached; // inutile de continuer à suivre l'élément puisqu'il ne seras plus utilisée.

            return res;
        }
        catch (Exception e)
        {
            Utilities.AddExceptionError($"la sauvegarde d'une recherche ({ss.Search}, {user.UserName})", e);

            return false;
        }
    }

    public static Task<List<SavedSearch>> GetAllSavedSearchs(AsmsearchContext database, int id)
    {
        return database.SavedSearch.Where(ss => ss.UserId == id).AsNoTracking().ToListAsync();
    }

    public static Task<SavedSearch> GetSavedSearch(AsmsearchContext _database, string search, int id)
    {
        return _database.SavedSearch.AsNoTracking().FirstOrDefaultAsync(ss => ss.UserId == id && ss.Search == search);
    }

    public static Task<int> DeleteSave(AsmsearchContext _database, string search, int userId)
    {
        try
        {
            _database.SavedSearch.Remove(new() {Search = search, UserId = userId});

            return _database.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Utilities.AddExceptionError(
                $"dans la suppression de la recherche sauvegardée '{search}' pour le user_id: {userId}", e);
            return Task.FromResult(-1);
        }
    }

    public static Task<int> DeleteManySaves(AsmsearchContext _database, IEnumerable<SavedSearch> list)
    {
        try
        {
            _database.SavedSearch.RemoveRange(list);

            return _database.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Utilities.AddExceptionError($"la suppression d'une liste de sauvegardes (nb: {list.Count()})", e);

            return Task.FromResult(-1);
        }
    }

    public static Task<Recherche[]> GetRecherchesDatas(AsmsearchContext _database)
    {
        return _database.Recherches.OrderByDescending(r => r.Nb_recherches).ToArrayAsync().ContinueWith(t =>
        {
            Recherche[] all = t.Result;

            Dictionary<string, int> recherches = new();

            foreach (Recherche recherche in all)
            {
                string strRech = recherche.recherche?.ToLowerInvariant();
                int nb = 0;

                if (recherches.ContainsKey(strRech))
                {
                    nb = recherches.GetValueOrDefault(strRech);
                    recherches.Remove(strRech);
                }

                nb += recherche.Nb_recherches;

                recherches.Add(strRech, nb);
            }

            return recherches
                .OrderBy(keyValue => keyValue.Key)
                .OrderByDescending(keyValue => keyValue.Value)
                .Select((kv) => new Recherche() {recherche = kv.Key, Nb_recherches = kv.Value})
                .ToArray();
        });
    }

    public static async Task<List<Citations>> GetCitations(AsmsearchContext _database)
    {
        return await _database.Citations.ToListAsync();
    }

    public static async Task<List<Sites>> GetAllSites(AsmsearchContext _database)
    {
        return await _database.Sites.ToListAsync();
    }

    public static async Task<List<Recherche>> RecherchesByUser(AsmsearchContext _database, int id)
    {
        return await _database.Recherches.Where(ip => ip.User_ID == id).ToListAsync();
    }

    public static async Task<List<IP>> IpsByUser(AsmsearchContext _database, int id)
    {
        return await _database.IPs.Where(ip => ip.Users_ID == id).ToListAsync();
    }

    public static async Task<bool> UpdateUserRole(UserManager<Users> _userManager, int userId, List<string> roles)
    {
        try
        {
            Users u = await _userManager.FindByIdAsync(userId.ToString());

            await _userManager.RemoveFromRolesAsync(u, await _userManager.GetRolesAsync(u));
            await _userManager.AddToRolesAsync(u, roles);

            return true;
        }
        catch (Exception e)
        {
            Utilities.AddExceptionError("Update roles API", e);

            return false;
        }
    }

    public static async Task<List<Users>> GetAllUsers(AsmsearchContext _database)
    {
        return await _database.Users.ToListAsync();
    }

    public static async Task<string> RemoveRole(RoleManager<Roles> _roleManager, string roleName)
    {
        if (roleName == Utilities.ADMIN_ROLE.Name || roleName == Utilities.SUPER_ADMIN_ROLE.Name)
            return "On ne supprime pas les rôles admin et super-admin !";

        try
        {
            Roles role = await _roleManager.FindByNameAsync(roleName);
            await _roleManager.DeleteAsync(role);

            return string.Empty;
        }
        catch (Exception e)
        {
            Utilities.AddExceptionError($"Suppression de role ({roleName})", e);

            return "Erreur interne";
        }
    }

    public static async Task<string> AddRole(RoleManager<Roles> _roleManager, string roleName)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
            return "Le rôle existe déjà";

        try
        {
            await _roleManager.CreateAsync(new(roleName));

            return string.Empty;
        }
        catch (Exception e)
        {
            Utilities.AddExceptionError($"la création du role '{roleName}'", e);

            return "erreur interne";
        }
    }

    public static async Task<List<Don>> DonsByUser(AsmsearchContext _database, int UserId)
    {
        return await _database.Dons.Where(d => d.User_id == UserId).ToListAsync();
    }

    public static async Task<bool> DeleteDon(AsmsearchContext _database, Guid id)
    {
        if (Guid.Empty != id)
        {
            Don don = await _database.Dons.FirstOrDefaultAsync(d => d.Id == id);

            if (don != null && !don.Done && DateTime.Now.Subtract(don.Date) >= TimeSpan.FromHours(1))
            {
                _database.Dons.Remove(don);
                await _database.SaveChangesAsync();

                return true;
            }
        }

        return false;
    }

    public static Task<int> SetCitattionState(AsmsearchContext _database, int id, bool isValidated)
    {
        return _database.Citations.FirstOrDefaultAsync(c => c.Id == id).ContinueWith(res =>
        {
            if (res.Result != null)
            {
                res.Result.IsValidated = isValidated;
                _database.Citations.Update(res.Result);

                return _database.SaveChanges();
            }

            return -1;
        });
    }

    public static Task<int> SetSiteValidationState(AsmsearchContext _database, string url, EtatSite etat)
    {
        Sites s = _database.Sites.FirstOrDefaultAsync(c => c.Url == url).GetAwaiter().GetResult();

        if (s != null)
        {
            s.Etat = etat;
            _database.Sites.Update(s);

            return _database.SaveChangesAsync();
        }

        return Task.Run(() => -1);
    }

    public static Task<List<TypeSite>> GetAllTypeSites(AsmsearchContext _database)
    {
        return _database.TypeSites.ToListAsync();
    }

    public static Task<int> SuppTypes(AsmsearchContext _database, TypeSite type)
    {
        try
        {
            _database.TypeSites.Remove(type);

            return _database.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Utilities.AddExceptionError($"la suppression de Types({type.Id})", e);
        }

        return Task.Run(() => -1);
    }

    public static Task<int> AddType(AsmsearchContext _database, string name)
    {
        if (!_database.TypeSites.Any(t => t.Name == name))
            try
            {
                var type = new TypeSite() {Name = name};
                _database.TypeSites.Add(type);

                return Task.Run(async () =>
                {
                    await _database.SaveChangesAsync();
                    return type.Id;
                });
            }
            catch (Exception e)
            {
                Utilities.AddExceptionError($"l'ajout de Types({name})", e);
            }

        return Task.Run(() => -1);
    }

    public static Task<List<Roles>> GetRoles(AsmsearchContext _database)
    {
        return _database.Roles.ToListAsync();
    }

    public static Task<int> DeleteSite(AsmsearchContext _database, Sites site)
    {
        _database.Sites.Remove(site);

        return _database.SaveChangesAsync();
    }

    public static async Task<Dictionary<string, object>> GetAllPersonnalDatas(AsmsearchContext database, string username)
    {
        var currentUser = await database.Users.FirstOrDefaultAsync(u => u.UserName == username);

        if (currentUser == null)
            return null;

        // Only include personal data for download
        var personalData = new Dictionary<string, object>();
        var personalDataProps = typeof(Users).GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));

        foreach (var p in personalDataProps)
            personalData.Add(p.Name, p.GetValue(currentUser)?.ToString() ?? "null");

        personalData.Add("recherches", database.Recherches.Where(r => r.User_ID == currentUser.Id).Select(r => new { r.recherche, r.Nb_recherches, Source = Enum.GetName(r.Source), r.Derniere_Recherche }).ToArray());
        personalData.Add("ips", database.IPs.Where(r => r.Users_ID == currentUser.Id).Select(i => new { i.Adresse_IP, i.Derniere_utilisation, i.Localisation }).ToArray());
        personalData.Add("saved_search", database.SavedSearch.Where(r => r.UserId == currentUser.Id).Select(s => new { s.Search, s.DateSauvegarde, s.Results }).ToArray());

        return personalData;
    }
}