using System.Drawing;
using AnimeSearch.Core;
using AnimeSearch.Core.Models.Api;
using AnimeSearch.Core.ViewsModel;
using AnimeSearch.Data;
using AnimeSearch.Data.Models;
using FluentEmail.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AnimeSearch.Services.Database;

public class DatasUtilsService
{
    private AsmsearchContext Database { get; }
    private UserManager<Users> UserManager { get; }
    private RoleManager<Roles> RoleManager { get; }

    public DatasUtilsService(AsmsearchContext database, UserManager<Users> userManager, RoleManager<Roles> roleManager)
    {
        Database = database;
        UserManager = userManager;
        RoleManager = roleManager;
    }

    public async Task<bool> SaveRechercheResult(string name, ModelAPI model, bool isForUpdate = false)
    {
        if (Database == null || string.IsNullOrWhiteSpace(name) || model == null)
            return false;

        var user = await Database.Users.FirstOrDefaultAsync(u => u.UserName == name);

        if (user == null) return false;

        var ss = isForUpdate
            ? await Database.SavedSearch.FirstOrDefaultAsync(s => s.Search == model.Search && s.UserId == user.Id)
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
            EntityEntry entry;

            if (isForUpdate)
                entry = Database.SavedSearch.Update(ss);
            else
                entry = await Database.SavedSearch.AddAsync(ss);

            var res = await Database.SaveChangesAsync() > 0;

            // inutile de continuer à suivre l'élément puisqu'il ne seras plus utilisée.
            entry.State = EntityState.Detached;

            return res;
        }
        catch (Exception e)
        {
            CoreUtils.AddExceptionError($"la sauvegarde d'une recherche ({ss.Search}, {user.UserName})", e);

            return false;
        }
    }
    
    public async Task<List<SavedSearch>> GetAllSavedSearchs(int userId)
    {
        return await Database.SavedSearch.Where(ss => ss.UserId == userId).AsNoTracking().ToListAsync();
    }

    public async Task<SavedSearch> GetSavedSearch(string search, int userId)
    {
        return await Database.SavedSearch.AsNoTracking().FirstOrDefaultAsync(ss => ss.UserId == userId && ss.Search == search);
    }

    public async Task<int> DeleteSave(string search, int userId)
    {
        try
        {
            Database.SavedSearch.Remove(new() {Search = search, UserId = userId});

            return await Database.SaveChangesAsync();
        }
        catch (Exception e)
        {
            CoreUtils.AddExceptionError(
                $"dans la suppression de la recherche sauvegardée '{search}' pour le user_id: {userId}", e);
            return await Task.FromResult(-1);
        }
    }

    public async Task<int> DeleteManySaves(IEnumerable<SavedSearch> list)
    {
        var savedSearches = list as SavedSearch[] ?? list.ToArray();
        
        try
        {
            Database.SavedSearch.RemoveRange(savedSearches);

            return await Database.SaveChangesAsync();
        }
        catch (Exception e)
        {
            CoreUtils.AddExceptionError($"la suppression d'une liste de sauvegardes (nb: {savedSearches.Length})", e);

            return await Task.FromResult(-1);
        }
    }

    public async Task<Recherche[]> GetRecherchesDatas()
    {
        var all = await Database.Recherches.OrderByDescending(r => r.Nb_recherches).ToArrayAsync();

        Dictionary<string, int> recherches = new();

        foreach (var recherche in all)
        {
            var strRech = recherche.recherche?.ToLowerInvariant();
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
            .ThenByDescending(keyValue => keyValue.Value)
            .Select(kv => new Recherche {recherche = kv.Key, Nb_recherches = kv.Value})
            .ToArray();
    }

    public async Task<List<Citations>> GetCitations()
    {
        return await Database.Citations.ToListAsync();
    }

    public async Task<List<Sites>> GetAllSites()
    {
        return await Database.Sites.ToListAsync();
    }

    public async Task<List<Recherche>> RecherchesByUser(int id)
    {
        return await Database.Recherches.Where(ip => ip.User_ID == id).ToListAsync();
    }

    public async Task<List<IP>> IpsByUser(int id)
    {
        return await Database.IPs.Where(ip => ip.Users_ID == id).ToListAsync();
    }

    public async Task<bool> UpdateUserRole(int userId, List<string> roles)
    {
        try
        {
            var u = await UserManager.FindByIdAsync(userId.ToString());

            await UserManager.RemoveFromRolesAsync(u, await UserManager.GetRolesAsync(u));
            await UserManager.AddToRolesAsync(u, roles);

            return true;
        }
        catch (Exception e)
        {
            CoreUtils.AddExceptionError("Update roles API", e);

            return false;
        }
    }

    public async Task<List<Users>> GetAllUsers()
    {
        return await Database.Users.ToListAsync();
    }

    public async Task<string> RemoveRole(string roleName)
    {
        if (roleName == DataUtils.AdminRole.Name || roleName == DataUtils.SuperAdminRole.Name)
            return "On ne supprime pas les rôles admin et super-admin !";

        try
        {
            var role = await RoleManager.FindByNameAsync(roleName);
            await RoleManager.DeleteAsync(role);

            return string.Empty;
        }
        catch (Exception e)
        {
            CoreUtils.AddExceptionError($"Suppression de role ({roleName})", e);

            return "Erreur interne";
        }
    }

    public async Task<string> AddRole(string roleName)
    {
        if (await RoleManager.RoleExistsAsync(roleName))
            return "Le rôle existe déjà";

        try
        {
            await RoleManager.CreateAsync(new(roleName));

            return string.Empty;
        }
        catch (Exception e)
        {
            CoreUtils.AddExceptionError($"la création du role '{roleName}'", e);

            return "erreur interne";
        }
    }

    public async Task<List<Don>> DonsByUser(int userId)
    {
        return await Database.Dons.Where(d => d.User_id == userId).ToListAsync();
    }

    public async Task<bool> DeleteDon(Guid id)
    {
        if (Guid.Empty != id)
        {
            Don don = await Database.Dons.FirstOrDefaultAsync(d => d.Id == id);

            if (don != null && !don.Done && DateTime.Now.Subtract(don.Date) >= TimeSpan.FromHours(1))
            {
                Database.Dons.Remove(don);
                await Database.SaveChangesAsync();

                return true;
            }
        }

        return false;
    }

    public async Task<int> SetCitattionState(int id, bool isValidated)
    {
        var citation = await Database.Citations.FirstOrDefaultAsync(c => c.Id == id);
        
        if (citation != null)
        {
            citation.IsValidated = isValidated;
            Database.Citations.Update(citation);

            return Database.SaveChanges();
        }

        return -1;
    }

    public async Task<int> SetSiteValidationState(string url, EtatSite etat)
    {
        Sites s = await Database.Sites.FirstOrDefaultAsync(c => c.Url == url);

        if (s != null)
        {
            s.Etat = etat;
            Database.Sites.Update(s);

            return await Database.SaveChangesAsync();
        }

        return -1;
    }

    public async Task<List<TypeSite>> GetAllTypeSites()
    {
        return await Database.TypeSites.ToListAsync();
    }

    public async Task<int> SuppTypes(TypeSite type)
    {
        try
        {
            Database.TypeSites.Remove(type);

            return await Database.SaveChangesAsync();
        }
        catch (Exception e)
        {
            CoreUtils.AddExceptionError($"la suppression de Types({type.Id})", e);
        }

        return -1;
    }

    public async Task<TypeSite> AddType(string name)
    {
        if (!await Database.TypeSites.AnyAsync(t => t.Name == name))
            try
            {
                var type = new TypeSite {Name = name};
                Database.TypeSites.Add(type);

                await Database.SaveChangesAsync();
                
                return type;
            }
            catch (Exception e)
            {
                CoreUtils.AddExceptionError($"l'ajout de Types({name})", e);
            }

        return null;
    }

    public async Task<List<Roles>> GetRoles()
    {
        return await Database.Roles.ToListAsync();
    }

    public async Task<int> DeleteSite(Sites site)
    {
        Database.Sites.Remove(site);

        return await Database.SaveChangesAsync();
    }

    public async Task<Dictionary<string, object>> GetAllPersonnalDatas(string username)
    {
        var currentUser = await Database.Users.FirstOrDefaultAsync(u => u.UserName == username);

        if (currentUser == null)
            return null;

        // Only include personal data for download
        var personalData = new Dictionary<string, object>();
        var personalDataProps = typeof(Users).GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));

        foreach (var p in personalDataProps)
            personalData.Add(p.Name, p.GetValue(currentUser)?.ToString() ?? "null");

        personalData.Add("recherches", Database.Recherches.Where(r => r.User_ID == currentUser.Id).Select(r => new { r.recherche, r.Nb_recherches, Source = Enum.GetName(r.Source), r.Derniere_Recherche }).ToArray());
        personalData.Add("ips", Database.IPs.Where(r => r.Users_ID == currentUser.Id).Select(i => new { i.Adresse_IP, i.Derniere_utilisation, i.Localisation }).ToArray());
        personalData.Add("saved_search", Database.SavedSearch.Where(r => r.UserId == currentUser.Id).Select(s => new { s.Search, s.DateSauvegarde, s.Results }).ToArray());

        return personalData;
    }

    public async Task<int> DeleteApi(int id = -1, string name = "")
    {
        var api = Database.Apis.FirstOrDefault(a => a.Id == id || a.Name == name);

        if (api == null) return -1;
        
        Database.Remove(api);

        var res = await Database.SaveChangesAsync();
        
        if(res > 0)
            DataUtils.Apis.RemoveAt(DataUtils.Apis.FindIndex(a => a.Id == api.Id));

        return res;
    }

    public async Task<int> DeleteApis(List<ApiObject> apis)
    {
        if (Database == null || apis == null || apis.Count == 0)
            return -1;
        
        Database.RemoveRange(apis);

        var res = await Database.SaveChangesAsync();
        
        if(res > 0)
            apis.ForEach(a => DataUtils.Apis.RemoveAt(DataUtils.Apis.FindIndex(api => api.Id == a.Id)));

        return res;
    }

    public async Task<bool> UpsertApi(ApiObject api)
    {
        if (api is null or {IsValid: false}) return false;

        EntityEntry<ApiObject> entityEntry;
        if (await Database.Apis.AnyAsync(a => a.Id == api.Id))
            entityEntry = Database.Apis.Update(api);
        else
            entityEntry = await Database.Apis.AddAsync(api);

        var res = await Database.SaveChangesAsync() > 0;

        entityEntry.State = EntityState.Detached;

        if (res)
        {
            if (DataUtils.Apis.All(a => a.Id != api.Id))
                DataUtils.Apis.Add(api);
            else
                DataUtils.Apis[DataUtils.Apis.FindIndex(a => a.Id == api.Id)] = api;

            if (!string.IsNullOrWhiteSpace(api.GenresMoviesUrl) || !string.IsNullOrWhiteSpace(api.GenresTvUrl))
            {
                var tmp = await api.WaitGenres();

                var genres = tmp.DistinctBy(g => new {g.Id, IdApi = g.ApiId}).ToList();
            
                tmp.Where(g => !genres.Contains(g)).ForEach(g =>
                {
                    var genreTmp = genres.FirstOrDefault(genre => genre.ApiId == g.ApiId && genre.Id == g.Id);

                    if (genreTmp != null)
                        genreTmp.Type = SearchType.All;
                });

                foreach (var genre in genres)
                {
                    var g = await Database.Genres.FirstOrDefaultAsync(g => g.ApiId == api.Id && g.Id == genre.Id);

                    if (g != null)
                    {
                        g.Name = genre.Name;

                        Database.Genres.Update(g);
                    }
                    else
                    {
                        await Database.Genres.AddAsync(genre);
                    }
                }

                await Database.SaveChangesAsync();
                api.Genres = genres;
            }
        }

        return res;
    }

    public async Task<bool> SiteClick(string url)
    {
        var site = await Database.Sites.FirstOrDefaultAsync(s => s.Url == url);

        if (site is null) return false;

        site.NbClick++;

        Database.Sites.Update(site);

        return await Database.SaveChangesAsync() == 1;
    }

    public async Task<int> DeleteDomain(IEnumerable<Uri> urls)
    {
        Database.Domains.RemoveRange(urls.Select(u => new Domains{Url = u}));
        
        return await Database.SaveChangesAsync();
    }
}