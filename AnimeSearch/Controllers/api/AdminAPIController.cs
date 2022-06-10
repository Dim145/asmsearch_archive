using AnimeSearch.Attributes;
using AnimeSearch.Database;
using AnimeSearch.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnimeSearch.Core;
using static AnimeSearch.Core.DatabaseCom;

namespace AnimeSearch.Controllers.api
{
    [LevelAuthorize(1)]
    [Route("/adminapi")]
    [ApiController]
    public class AdminAPIController : ControllerBase
    {
        private readonly AsmsearchContext _database;

        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<Roles> _roleManager;

        public AdminAPIController(AsmsearchContext database, UserManager<Users> userManager,
            RoleManager<Roles> roleManager)
        {
            _database = database;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet("users")]
        [LevelAuthorize(2)]
        public async Task<ActionResult<List<Users>>> GetAllUsersAsync()
        {
            return Ok(await GetAllUsers(_database));
        }

        [Authorize(Roles = "Super-Admin")]
        [HttpGet("deleteRole/{roleName}")]
        public async Task<IActionResult> RemoveRole(string roleName)
        {
            if (roleName == Utilities.SUPER_ADMIN_ROLE.Name || roleName == Utilities.ADMIN_ROLE.Name)
                return BadRequest("On ne supprime pas les deux rôles permettant de géré le site.");

            string res = await DatabaseCom.RemoveRole(_roleManager, roleName);

            return string.IsNullOrWhiteSpace(res) ? Ok() : BadRequest(res);
        }

        [Authorize(Roles = "Super-Admin")]
        [HttpPost("addRole")]
        public async Task<IActionResult> AddRole(string roleName)
        {
            string res = await DatabaseCom.AddRole(_roleManager, roleName);

            return string.IsNullOrWhiteSpace(res) ? Ok() : BadRequest(res);
        }

        [Authorize(Roles = "Super-Admin")]
        [HttpPost("updateUserRoles")]
        public async Task<IActionResult> UpdateUserRoles(int userId, List<string> roles)
        {
            return await UpdateUserRole(_userManager, userId, roles) ? Ok() : BadRequest();
        }

        [Authorize(Roles = "Super-Admin")]
        [HttpGet("roles")]
        public async Task<ActionResult<List<Roles>>> GetRoles()
        {
            return Ok(await DatabaseCom.GetRoles(_database));
        }

        [HttpGet("ipsByUsers/{id}")]
        [LevelAuthorize(2)]
        public async Task<ActionResult<List<IP>>> GetAllIpsByUser(int id)
        {
            return Ok(await IpsByUser(_database, id));
        }

        [HttpGet("recherchesByUser/{id}")]
        [LevelAuthorize(2)]
        public async Task<ActionResult<List<Recherche>>> GetAllRecherchesByUser(int id)
        {
            return Ok(await RecherchesByUser(_database, id));
        }

        [HttpGet("sites")]
        public async Task<ActionResult<List<Sites>>> GetAllSites()
        {
            return Ok(await DatabaseCom.GetAllSites(_database));
        }

        [HttpGet("citations")]
        public ActionResult<List<Citations>> GetAllCitationsAsync()
        {
            return Ok(GetCitations(_database).GetAwaiter().GetResult());
        }

        [HttpGet("dons/{id}")]
        [LevelAuthorize(6)]
        public async Task<ActionResult<List<Don>>> GetAllDonForUser(int id)
        {
            return Ok(await DonsByUser(_database, id));
        }

        [LevelAuthorize(6)]
        [HttpPost("rdons")]
        public async Task<ActionResult<bool>> DeleteDon([FromForm] Guid id)
        {
            return Ok(await DatabaseCom.DeleteDon(_database, id));
        }

        [HttpPost("CCVS")]
        [LevelAuthorize(3)]
        public async Task<IActionResult> ChangeCitationValidateState([FromForm] int id, [FromForm] bool isValidated)
        {
            if (id > 0) return Ok(await SetCitattionState(_database, id, isValidated));

            return BadRequest();
        }

        [HttpPost("CSVS")]
        [LevelAuthorize(3)]
        public async Task<IActionResult> ChangeSiteValidateState([FromForm] string url, [FromForm] EtatSite etat)
        {
            if (!string.IsNullOrWhiteSpace(url)) return Ok(await SetSiteValidationState(_database, url, etat));

            return BadRequest();
        }

        [HttpGet("types")]
        [LevelAuthorize(4)]
        public async Task<ActionResult<List<TypeSite>>> GetAllTypesSites()
        {
            return Ok(await GetAllTypeSites(_database));
        }

        [HttpGet("suppType")]
        [LevelAuthorize(4)]
        public async Task<IActionResult> SuppType(int id)
        {
            if (id > 0)
            {
                return Ok(await SuppTypes(_database, new() {Id = id}));
            }

            return BadRequest();
        }

        [HttpPost("addType")]
        [LevelAuthorize(4)]
        public async Task<IActionResult> AddType([FromForm] string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                return Ok(await DatabaseCom.AddType(_database, name));
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
                Utilities.SUPER_ADMIN_ROLE.Name);

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
                UrlIcon = ms.UrlIcon
            };

            if (!string.IsNullOrWhiteSpace(ms.SpostValues))
            {
                site.PostValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(ms.SpostValues);
                ms.PostValues = site.PostValues;
            }

            if (site.UrlSearch == null)
                site.UrlSearch = string.Empty;

            if (site.PostValues != null && site.PostValues.Count == 0)
                site.PostValues = null;

            bool urlChange = ms.Url != ms.UrlChange;

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

            return Ok(await DatabaseCom.DeleteSite(_database, new() {Url = url}));
        }

        public class ModelSiteBdd : Sites
        {
            public string SpostValues { get; set; }
            public string UrlChange { get; set; }
        }
    }
}
