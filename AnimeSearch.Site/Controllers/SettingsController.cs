using AnimeSearch.Core;
using AnimeSearch.Data;
using AnimeSearch.Data.Models;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace AnimeSearch.Site.Controllers;

[Route("settings")]
[Authorize(Roles = "Super-Admin")]
public class SettingsController : BaseController
{
    private readonly UserManager<Users> _userManager;

    public SettingsController(AsmsearchContext database, UserManager<Users> um): base(database)
    {
        _userManager = um;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await base.OnActionExecutionAsync(context, next);

        ViewData["currentUser"] = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await _database.Settings.ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Details(string id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var setting = await _database.Settings.FirstOrDefaultAsync(m => m.Name == id);

        if (setting == null)
        {
            return NotFound();
        }

        return View(setting);
    }

    [HttpGet("new")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost("new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Description,TypeValue,JsonValue,IsDeletable")] Setting setting)
    {
        if (ModelState.IsValid)
        {
            if (!await _userManager.IsInRoleAsync(currentUser, DataUtils.SuperAdminRole.Name))
                setting.IsDeletable = true;

            if (!setting.JsonValue.StartsWith("\"") && setting.TypeValue != "number")
                setting.JsonValue = '"' + setting.JsonValue.Replace("\"", "\\\"") + '"';

            if (setting.TypeValue == "Script")
            {
                var node = HtmlNode.CreateNode(setting.JsonValue);

                setting.TypeValue = node.GetType().FullName;
                setting.SetValue(node, new HtmlNodeConverter());
            }
            else
            {
                setting.TypeValue = GetTypeFullName(setting.TypeValue);
                setting.SetValue(setting.GetValueObject());
            }
            
            _database.Add(setting);

            await _database.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        return View(setting);
    }

    [HttpGet("{id}/edit")]
    public async Task<IActionResult> Edit(string id)
    {
        if (id == null)
            return NotFound();

        var setting = await _database.Settings.FindAsync(id);

        if (setting == null)
            return NotFound();

        return View(setting);
    }

    [HttpPost("{id}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, [Bind("Name,Description,TypeValue,JsonValue,IsDeletable")] Setting setting)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var baseSetting = await _database.Settings.FirstOrDefaultAsync(s => s.Name == setting.Name);

                if (baseSetting == null)
                    return NotFound();

                if (baseSetting.IsDeletable != setting.IsDeletable && await _userManager.IsInRoleAsync(currentUser, DataUtils.SuperAdminRole.Name))
                    baseSetting.IsDeletable = setting.IsDeletable;

                if (!setting.JsonValue.StartsWith("\"") && baseSetting.TypeValue != "number")
                    setting.JsonValue = '"' + setting.JsonValue.Replace("\\\"", "\"").Replace("\"", "\\\"") + '"';

                baseSetting.JsonValue = setting.JsonValue;
                baseSetting.Description = setting.Description;

                var converter = new HtmlNodeConverter();
                var baseType = baseSetting.TypeValue;
                
                baseSetting.SetValue(baseSetting.GetValueObject(converters: converter), converter);

                if (baseType != baseSetting.TypeValue && baseType == typeof(HtmlNode).FullName)
                    baseSetting.TypeValue = typeof(HtmlNode).FullName;

                _database.Update(baseSetting);

                await _database.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException e)
            {
                if (!SettingExists(setting.Name))
                {
                    return NotFound();
                }
                else
                {
                    CoreUtils.AddExceptionError("la modification du param " + id, e, currentUser.UserName);

                    throw;
                }
            }

            return RedirectToAction(nameof(Index));
        }

        return View(setting);
    }

    [HttpGet("{id}/Delete")]
    public async Task<IActionResult> Delete(string id)
    {
        if (id == null)
            return NotFound();

        var setting = await _database.Settings.FirstOrDefaultAsync(m => m.Name == id);

        if (setting == null)
            return NotFound();
        else if (!setting.IsDeletable)
            return BadRequest();

        return View(setting);
    }

    [HttpPost("{id}/Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var setting = await _database.Settings.FindAsync(id);

        if (setting == null || !setting.IsDeletable)
            return BadRequest();

        _database.Settings.Remove(setting);

        await _database.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private bool SettingExists(string id)
    {
        return _database.Settings.Any(e => e.Name == id);
    }

    private static string GetTypeFullName(string str)
    {
        return (str.ToLower() switch
        {
            "number" => typeof(double),
            "time" => typeof(TimeSpan),
            _ => typeof(string),
        }).FullName;
    }
}