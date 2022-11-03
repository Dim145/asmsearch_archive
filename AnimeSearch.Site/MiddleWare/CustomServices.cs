using AnimeSearch.Data;
using AnimeSearch.Data.Models;
using AnimeSearch.Services.Background;
using AnimeSearch.Services.HangFire;
using AnimeSearch.Services.Mails;
using AnimeSearch.Services.Search;
using Blazored.LocalStorage;
using BlazorTable;
using CurrieTechnologies.Razor.SweetAlert2;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;

namespace AnimeSearch.Site.MiddleWare;

public static class CustomServices
{
    /// <summary>
    /// Declare all builder.Services needed in the project
    /// This method is called in the Program.cs file
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static WebApplicationBuilder AddCustomServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddServerSideBlazor(a => a.DetailedErrors = true);

        builder.Services.AddBlazoredLocalStorage();

        builder.Services.AddBlazorTable();
        builder.Services.AddSweetAlert2(options => options.Theme = SweetAlertTheme.Dark);

        builder.Services.ConfigureApplicationCookie(o =>
        {
            o.ExpireTimeSpan = TimeSpan.FromDays(31);

            o.LoginPath = "/account/login";
            o.LogoutPath = "/account/logout";

            o.AccessDeniedPath = "/error";

            o.Events.OnRedirectToAccessDenied = ctx =>
            {
                var accept = ctx.Request.Headers.Accept.ToString();
                if (accept.Contains("html") || accept.Equals("*/*"))
                    ctx.Response.Redirect("/Error?c=401");
                else
                {
                    ctx.Response.StatusCode = 401;
                    ctx.Response.CompleteAsync();
                }

                return Task.CompletedTask;
            };
        });

        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
        
        builder.Services
            .AddControllersWithViews()
            .AddNewtonsoftJson()
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
            .AddDataAnnotationsLocalization();
        
        builder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            options.SetDefaultCulture(SiteUtils.SupportedCultures.First())
                .AddSupportedCultures(SiteUtils.SupportedCultures)
                .AddSupportedUICultures(SiteUtils.SupportedCultures);
        });
        
        builder.Services.AddSentry();

        return builder;
    }
}