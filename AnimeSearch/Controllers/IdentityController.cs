using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AnimeSearch.Database;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnimeSearch.Controllers;

[Route("account")]
public class IdentityController: BaseController
{
    private readonly UserManager<Users> _userManager;
    private readonly SignInManager<Users> _signInManager;
    
    public IList<AuthenticationScheme> ExternalLogins { get; set; }

    public string ReturnUrl { get; set; }

    [TempData]
    public string ErrorMessage { get; set; }

    public IdentityController(AsmsearchContext database, UserManager<Users> userManager, SignInManager<Users> signInManager) : base(database)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet("login")]
    public async Task<ActionResult> LoginGet(string returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        returnUrl ??= Url.Content("~/");

        if ((User.Identity?.IsAuthenticated).GetValueOrDefault(false))
            return LocalRedirect(returnUrl);

        // Clear the existing external cookie to ensure a clean login process
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        ReturnUrl = returnUrl;

        return View("Login");
    }

    [HttpPost("login")]
    public async Task<ActionResult> LoginPost([Bind] InputLoginModel InputLogin, string returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        
        if ((User.Identity?.IsAuthenticated).GetValueOrDefault(false))
            return LocalRedirect(returnUrl);

        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        
        if (ModelState.IsValid)
        {
            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, set lockoutOnFailure: true
            var result = await _signInManager.PasswordSignInAsync(InputLogin.UserName, InputLogin.Password, InputLogin.RememberMe, lockoutOnFailure: false);
            
            if (result.Succeeded)
            {
                
                return LocalRedirect(returnUrl);
            }
            
            if (result.IsLockedOut)
            {

                return RedirectToAction("Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");

                return View("Login");
            }
        }

        // If we got this far, something failed, redisplay form
        return View("Login");
    }

    [HttpGet("logout")]
    public ActionResult LogoutGet()
    {
        return View("Logout");
    }

    [HttpPost("logout")]
    public async Task<ActionResult> LogoutPost(string returnUrl = null)
    {
        if ((User.Identity?.IsAuthenticated).GetValueOrDefault(false))
        {
            await _signInManager.SignOutAsync();
            
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        return View("Logout");
    }

    [HttpGet("register")]
    public async Task<ActionResult> RegisterGet(string returnUrl = null)
    {
        ReturnUrl = returnUrl;
        
        if ((User.Identity?.IsAuthenticated).GetValueOrDefault(false))
            return LocalRedirect(returnUrl ?? Url.Content("~/"));
        
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        ViewData["rurl"] = ReturnUrl;
        
        return View("register");
    }

    [HttpPost("register")]
    public async Task<ActionResult> RegisterPost([Bind] InputRegisterModel InputRegister, string returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        
        if ((User.Identity?.IsAuthenticated).GetValueOrDefault(false))
            return LocalRedirect(returnUrl);
        
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        
        if (ModelState.IsValid)
        {
            IdentityResult result;

            if (InputRegister.UserName == Utilities.GUEST)
            {
                result = IdentityResult.Failed(new IdentityError() { Description = "Ce nom d'utilisateur est révervé par le système." });
            }
            else
            {
                var user = await _userManager.FindByNameAsync(InputRegister.UserName) ?? await _database.Users.FirstOrDefaultAsync(u => u.UserName == InputRegister.UserName);

                if (user != null)
                {
                    if (user.SecurityStamp == null && user.PasswordHash == null)
                    {
                        result = await _userManager.AddPasswordAsync(user, InputRegister.Password);
                    }
                    else
                    {
                        result = IdentityResult.Failed(new IdentityError { Description = "Un utilisateur existe déjà avec ce Pseudo" });
                    }
                }
                else
                {
                    user = new() { UserName = InputRegister.UserName };
                    result = await _userManager.CreateAsync(user, InputRegister.Password);
                }

                if (result != null && result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: InputRegister.RememberMe);
                    
                    return LocalRedirect(returnUrl);
                }
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        // If we got this far, something failed, redisplay form
        return View("register");
    }

    [HttpGet("lockout")]
    public ActionResult Lockout()
    {
        return View();
    }
    
    public class InputLoginModel
    {
        [Required]
        [Display(Name = "Pseudo")]
        [DataType(DataType.Text)]
        public string UserName { get; set; }

        [Required]
        [Display(Name = "Mot de passe")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
    
    public class InputRegisterModel: InputLoginModel
    {
        [Required]
        [StringLength(100, ErrorMessage = "La longueur du {0} doit être d'au moins {2} et au maximum de {1} caractères", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public new string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmation du Mot de passe")]
        [Compare("Password", ErrorMessage = "Les deux mots de passes ne correspondent pas.")]
        public string ConfirmPassword { get; set; }
    }
}