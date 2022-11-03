using AnimeSearch.Api.Attributes;
using AnimeSearch.Core;
using AnimeSearch.Data;
using AnimeSearch.Data.Models;
using AnimeSearch.Services.Database;
using FluentEmail.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace AnimeSearch.Api.Controllers;

[LevelAuthorize(1)]
[Route("/adminapi")]
[ApiController]
public class AdminApiController : BaseApiController
{
    private readonly UserManager<Users> _userManager;
    private readonly RoleManager<Roles> _roleManager;
    private readonly DatasUtilsService _datasUtilsService;

    public AdminApiController(AsmsearchContext database, UserManager<Users> userManager,
        RoleManager<Roles> roleManager, DatasUtilsService datasUtilsService): base(database)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _datasUtilsService = datasUtilsService;
    }

    [HttpGet("users")]
    [LevelAuthorize(2)]
    public async Task<ActionResult<List<Users>>> GetAllUsersAsync()
    {
        return Ok(await _datasUtilsService.GetAllUsers());
    }

    [Authorize(Roles = "Super-Admin")]
    [HttpGet("deleteRole/{roleName}")]
    public async Task<IActionResult> RemoveRole(string roleName)
    {
        if (roleName == DataUtils.SuperAdminRole.Name || roleName == DataUtils.AdminRole.Name)
            return BadRequest("On ne supprime pas les deux rôles permettant de géré le site.");

        string res = await _datasUtilsService.RemoveRole(roleName);

        return string.IsNullOrWhiteSpace(res) ? Ok() : BadRequest(res);
    }

    [Authorize(Roles = "Super-Admin")]
    [HttpPost("addRole")]
    public async Task<IActionResult> AddRole(string roleName)
    {
        string res = await _datasUtilsService.AddRole(roleName);

        return string.IsNullOrWhiteSpace(res) ? Ok() : BadRequest(res);
    }

    [Authorize(Roles = "Super-Admin")]
    [HttpPost("updateUserRoles")]
    public async Task<IActionResult> UpdateUserRoles(int userId, List<string> roles)
    {
        return await _datasUtilsService.UpdateUserRole(userId, roles) ? Ok() : BadRequest();
    }

    [Authorize(Roles = "Super-Admin")]
    [HttpGet("roles")]
    public async Task<ActionResult<List<Roles>>> GetRoles()
    {
        return Ok(await _datasUtilsService.GetRoles());
    }

    [HttpGet("ipsByUsers/{id}")]
    [LevelAuthorize(2)]
    public async Task<ActionResult<List<IP>>> GetAllIpsByUser(int id)
    {
        return Ok(await _datasUtilsService.IpsByUser(id));
    }

    [HttpGet("recherchesByUser/{id}")]
    [LevelAuthorize(2)]
    public async Task<ActionResult<List<Recherche>>> GetAllRecherchesByUser(int id)
    {
        return Ok(await _datasUtilsService.RecherchesByUser(id));
    }

    [HttpGet("sites")]
    public async Task<ActionResult<List<Sites>>> GetAllSites()
    {
        return Ok(await _datasUtilsService.GetAllSites());
    }

    [HttpGet("citations")]
    public ActionResult<List<Citations>> GetAllCitationsAsync()
    {
        return Ok(_datasUtilsService.GetCitations().GetAwaiter().GetResult());
    }

    [HttpGet("dons/{id}")]
    [LevelAuthorize(6)]
    public async Task<ActionResult<List<Don>>> GetAllDonForUser(int id)
    {
        return Ok(await _datasUtilsService.DonsByUser(id));
    }

    [LevelAuthorize(6)]
    [HttpPost("rdons")]
    public async Task<ActionResult<bool>> DeleteDon([FromForm] Guid id)
    {
        return Ok(await _datasUtilsService.DeleteDon(id));
    }

    [HttpPost("CCVS")]
    [LevelAuthorize(3)]
    public async Task<IActionResult> ChangeCitationValidateState([FromForm] int id, [FromForm] bool isValidated)
    {
        if (id > 0) return Ok(await _datasUtilsService.SetCitattionState(id, isValidated));

        return BadRequest();
    }

    [HttpPost("CSVS")]
    [LevelAuthorize(3)]
    public async Task<IActionResult> ChangeSiteValidateState([FromForm] string url, [FromForm] EtatSite etat)
    {
        if (!string.IsNullOrWhiteSpace(url)) return Ok(await _datasUtilsService.SetSiteValidationState(url, etat));

        return BadRequest();
    }

    [HttpGet("types")]
    [LevelAuthorize(4)]
    public async Task<ActionResult<List<TypeSite>>> GetAllTypesSites()
    {
        return Ok(await _datasUtilsService.GetAllTypeSites());
    }

    [HttpGet("suppType")]
    [LevelAuthorize(4)]
    public async Task<IActionResult> SuppType(int id)
    {
        if (id > 0)
        {
            return Ok(await _datasUtilsService.SuppTypes(new() {Id = id}));
        }

        return BadRequest();
    }

    [HttpPost("addType")]
    [LevelAuthorize(4)]
    public async Task<IActionResult> AddType([FromForm] string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            return Ok(await _datasUtilsService.AddType(name));
        }

        return BadRequest();
    }

    [HttpPost("majsite")]
    [LevelAuthorize(4)]
    public async Task<IActionResult> ModifySite([FromForm] ModelSiteBdd ms)
    {
        if (ms == null || string.IsNullOrWhiteSpace(ms.Url) || string.IsNullOrWhiteSpace(ms.Title) ||
            string.IsNullOrWhiteSpace(ms.UrlIcon) || string.IsNullOrWhiteSpace(ms.TypeSite))
            return UnprocessableEntity(
                "Données invalides, une url, un titre, une url d'icon et un type sont requis");

        bool isSuperAdmin = await _userManager.IsInRoleAsync(await _userManager.FindByNameAsync(User.Identity.Name),
            DataUtils.SuperAdminRole.Name);

        Sites site = new()
        {
            Url = string.IsNullOrWhiteSpace(ms.UrlChange) ? ms.Url : ms.UrlChange,
            UrlSearch = ms.UrlSearch,
            Title = ms.Title,
            CheminBaliseA = ms.CheminBaliseA,
            IdBase = ms.IdBase,
            Etat = ms.Etat,
            Is_inter = ms.Is_inter,
            TypeSite = ms.TypeSite,
            UrlIcon = ms.UrlIcon,
            CheminToNbResult = ms.CheminToNbResult
        };

        site.NbClick = await _database.Sites.Where(s => s.Url == site.Url).Select(s => s.NbClick).FirstOrDefaultAsync();

        if (!string.IsNullOrWhiteSpace(ms.SpostValues))
        {
            site.PostValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(ms.SpostValues);
            ms.PostValues = site.PostValues;
        }

        site.UrlSearch ??= string.Empty;

        if (site.PostValues is {Count: 0})
            site.PostValues = null;

        var urlChange = ms.Url != ms.UrlChange;

        if ((urlChange && !isSuperAdmin) || !(urlChange ^ await _database.Sites.AnyAsync(s => s.Url == site.Url)) ||
            (!TypeEnum.TabTypes.Contains(site.TypeSite) &&
             !await _database.TypeSites.AnyAsync(t => t.Name == site.TypeSite)) ||
            string.IsNullOrWhiteSpace(site.CheminBaliseA))
        {
            return BadRequest("Une des valeurs saisies n'est pas corrects.");
        }

        if (urlChange)
        {
            _database.Sites.Remove(new() {Url = ms.Url});
            await _database.Sites.AddAsync(site);
        }
        else
        {
            _database.Sites.Update(site);
        }

        await _database.SaveChangesAsync();

        return Ok();
    }

    [HttpGet("deleteSite")]
    [LevelAuthorize(6)]
    public async Task<IActionResult> DeleteSite(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest("Url vide");

        return Ok(await _datasUtilsService.DeleteSite(new() {Url = url}));
    }

    [LevelAuthorize(2)]
    [HttpGet("apis")]
    public OkObjectResult GetApis()
    {
        var list = _database.Apis
            .IgnoreAutoIncludes()
            .Include(a => a.Filters)
            .Include(a => a.Sorts)
            .ToList()
            .Where(a => a.IsValid);
        
        list.ForEach(a =>
        {
            a.Filters?.ForEach(f => f.ApiObject = null);
            a.Sorts?.ForEach(s => s.ApiObject = null);
        });
        
        return Ok(list);
    }

    [LevelAuthorize(5)]
    [HttpPost("apis")]
    public async Task<OkObjectResult> UpsertApi([FromBody] ApiObject api) => Ok(await _datasUtilsService.UpsertApi(api));

    [LevelAuthorize(6)]
    [HttpGet("apis/{id:int}/delete")]
    public async Task<OkObjectResult> DeleteApis(int id) => Ok(await _datasUtilsService.DeleteApi(id: id));

    [LevelAuthorize(6)]
    [HttpPost("deleteDomain")]
    public async Task<IActionResult> DeleteDomain([FromForm] Uri url) => Ok(await _datasUtilsService.DeleteDomain(new[]{url}) == 1);

    public class ModelSiteBdd : Sites
    {
        public string SpostValues { get; set; }
        public string UrlChange { get; set; }
    }
}