using AnimeSearch.Core;
using AnimeSearch.Data;
using AnimeSearch.Data.Models;
using AnimeSearch.Services.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace AnimeSearch.Api.Controllers;

[ApiController]
[Authorize]
[Route("/api/account")]
public class AccountApiController : BaseApiController
{
    private readonly UserManager<Users> _userManager;
    private readonly SignInManager<Users> _signInManager;
    private readonly DatasUtilsService _datasUtilsService;

    public AccountApiController(AsmsearchContext database, 
        UserManager<Users> userManager, 
        SignInManager<Users> signInManager,
        DatasUtilsService datasUtilsService): base(database)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _datasUtilsService = datasUtilsService;
    }
    
    [HttpPost("datas/delete")]
    public async Task<ActionResult> DeletePersonnalDatasPost([FromForm] string mdp)
    {
        if (await _userManager.HasPasswordAsync(currentUser))
        {
            if (!await _userManager.CheckPasswordAsync(currentUser, mdp))
            {
                return Unauthorized("Mot de passe incorrect");
            }
        }
        
        var userId = await _userManager.GetUserIdAsync(currentUser);

        try
        {
            if(await _userManager.IsInRoleAsync(currentUser, DataUtils.SuperAdminRole.Name) && _database.UserRoles.Count(ur => ur.RoleId == _database.Roles.FirstOrDefault(r => r.Name == DataUtils.SuperAdminRole.Name).Id) <= 1)
            {
                return StatusCode(StatusCodes.Status423Locked, "Vous êtes le seul Super-Admin, vous supprimer ne permettrait plus de gérer le site.");
            }

            _database.IPs.RemoveRange(await _database.IPs.Where(ip => ip.Users_ID == currentUser.Id).ToArrayAsync());
            var rs = await _database.Recherches.Where(r => r.User_ID == currentUser.Id).ToArrayAsync();

            var guest = await _database.Users.FirstOrDefaultAsync(u => u.UserName == ApiUtils.GUEST);

            if(guest != null) // on ne supprime pas vraiment la recherche pour concervé les historiques/top (Guest par défault)
            {
                foreach (var r in rs)
                    r.User_ID = guest.Id;

                _database.Recherches.UpdateRange(rs);
            }

            await _database.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(currentUser);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Unexpected error occurred deleting user with ID '{userId}'.");
            }

            await _signInManager.SignOutAsync();

            return Ok();
        }
        catch(Exception e)
        {
            CoreUtils.AddExceptionError($"la suppression de l'utilisatier {userId}({currentUser.UserName})", e);

            return StatusCode(StatusCodes.Status500InternalServerError, "Une erreur interne est survenue");
        }
    }

    [HttpGet("download/datas")]
    public async Task<IActionResult> DownloadPersonnalData()
    {
        var datas = await _datasUtilsService.GetAllPersonnalDatas(User.Identity?.Name);
        
        if(datas == null)
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        
        return new FileContentResult(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(datas)), "application/json");
    }
}