using AnimeSearch.Data;
using AnimeSearch.Data.Models;
using AnimeSearch.Services.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AnimeSearch.Api.Attributes;

public class SwaggerAuthorize: IAuthorizeData
{
    public string Policy { get; set; }
    public string Roles
    {
        get => string.Join(",", GetRoles());
        set { }
    }

    public string AuthenticationSchemes { get; set; }
    
    private AsmsearchContext Database { get; }
    private int MinLevel { get; }

    public SwaggerAuthorize(IServiceProvider services, int minLevel)
    {
        Database = services?.GetService<AsmsearchContext>();
        MinLevel = minLevel;
    }

    private List<string> GetRoles()
    {
        var roles = Database?.Roles.Where(r => r.NiveauAutorisation >= MinLevel).ToList() ?? new();
        
        if(!roles.Any())
            roles.Add(DataUtils.SuperAdminRole);

        return roles.Select(r => r.Name).ToList();
    }
}