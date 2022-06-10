using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AnimeSearch.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnimeSearch.Controllers;

[Authorize]
[Route("account")]
public class AccountController : BaseController
{
    private readonly UserManager<Users> _userManager;
    private readonly SignInManager<Users> _signInManager;
    
    [TempData]
    private string StatusMessage { get; set; }

    public AccountController(AsmsearchContext database, UserManager<Users> userManager, SignInManager<Users> signInManager) : base(database)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public ActionResult IndexGet()
    {
        ViewData["StatusMessage"] = StatusMessage;
        ViewData["currentUser"] = currentUser;
        
        return View("Index", new ChangeProfilInput { UserName = currentUser.UserName, PhoneNumber = currentUser.PhoneNumber});
    }

    [HttpPost]
    public async Task<ActionResult> IndexPost([Bind] ChangeProfilInput input)
    {
        if (currentUser == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }
        
        ViewData["currentUser"] = currentUser;

        if (!ModelState.IsValid)
        {
            return View("Index", new ChangeProfilInput {  UserName = currentUser.UserName, PhoneNumber = currentUser.PhoneNumber});
        }

        var phoneNumber = currentUser.PhoneNumber;
        if (input.PhoneNumber != phoneNumber)
        {
            var user = await _userManager.FindByNameAsync(currentUser.UserName);
            var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, input.PhoneNumber);

            if (!setPhoneResult.Succeeded)
            {
                StatusMessage = "Unexpected error when trying to set phone number.";
                return RedirectToAction("IndexGet", "Account");
            }
        }

        await _signInManager.RefreshSignInAsync(currentUser);
        StatusMessage = "Your profile has been updated";
        
        return RedirectToAction("IndexGet", "Account");
    }

    [HttpGet("changePassword")]
    public ActionResult ChangePasswordGet()
    {
        if (currentUser == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }
        
        return View("ChangePassword");
    }

    [HttpPost("changePassword")]
    public async Task<ActionResult> ChangePasswordPost([Bind] ChangePasswordInput input)
    {
        if (!ModelState.IsValid)
        {
            return View("ChangePassword");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var changePasswordResult = await _userManager.ChangePasswordAsync(user, input.OldPassword, input.NewPassword);
        if (!changePasswordResult.Succeeded)
        {
            foreach (var error in changePasswordResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            
            return View("ChangePassword");
        }

        await _signInManager.RefreshSignInAsync(user);
        
        ViewData["StatusMessage"] = "Your password has been changed.";

        return View("ChangePassword");
    }

    [HttpGet("personnalHistory")]
    public ActionResult PersonnalHistory()
    {
        ViewData["currentUser"] = currentUser;
        
        return View();
    }

    [HttpGet("savedSearch")]
    public ActionResult SavedSearch()
    {
        ViewData["currentUser"] = currentUser;
        
        return View();
    }

    [HttpGet("datas")]
    public ActionResult PersonnalDatas()
    {
        return View();
    }

    public class ChangeProfilInput
    {
        [DisplayName("Pseudo")]
        public string UserName { get; set; }
        
        [Phone]
        [Display(Name = "Numéro de téléphone")]
        public string PhoneNumber { get; set; }
    }
    
    public class ChangePasswordInput
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe actuel")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Nouveau mot de passe")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmation mot de passe")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}