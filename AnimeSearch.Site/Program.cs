using System.Text.RegularExpressions;
using AnimeSearch.Api.Attributes;
using AnimeSearch.Api.Middleware;
using AnimeSearch.Core;
using AnimeSearch.Data;
using AnimeSearch.Services.HangFire;
using AnimeSearch.Site;
using AnimeSearch.Site.Authorizations;
using AnimeSearch.Site.MiddleWare;
using Hangfire;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiServices().AddCustomServices();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new(){Title = "AsmSearch api swagger", Version = "v1"});
    c.DocInclusionPredicate((_, apiDesc) => apiDesc.RelativePath?.Contains("api/") ?? false);
});

var app = builder.Build();

var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(SiteUtils.SupportedCultures.First())
    .AddSupportedCultures(SiteUtils.SupportedCultures)
    .AddSupportedUICultures(SiteUtils.SupportedCultures);

app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
    
    
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseStatusCodePages(new StatusCodePagesOptions()
{
    HandleAsync = (context) => Task.Run(() =>
    {
        var accept = context.HttpContext.Request.Headers.Accept.ToString();

        if (accept.Contains("html") || accept.Contains("*/*"))
            context.HttpContext.Response.Redirect($"/Error?c={context.HttpContext.Response.StatusCode}");
    })
});

app.UseHttpsRedirection();
app.UseStaticFiles();

var provider = new FileExtensionContentTypeProvider();
// Add new mappings
provider.Mappings[".mtn"] = "application/octet-stream";
provider.Mappings[".moc"] = "application/octet-stream";
provider.Mappings[".moc3"] = "application/octet-stream";
provider.Mappings[".cache"] = "application/octet-stream";

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.WebRootPath, "lib/live/api")),
    RequestPath = "/live2dapi",
    ContentTypeProvider = provider
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AsmSearch api swagger");
});

app.UseSentryTracing();

app.UseHangfireDashboard("/jobsStates", new DashboardOptions
{
    Authorization = new[]{ new HangfireDashboardAuth() },
    DashboardTitle = "<script type='text/javascript' src='/js/dashboard.js'></script><b>Hangfire</b>"
});

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}"
    );

    endpoints.Map("/swagger/index.html", endpoints.CreateApplicationBuilder().Build())
        .RequireAuthorization(new SwaggerAuthorize(app.Services, DataUtils.DroitAdd));
                
    endpoints.MapBlazorHub();
    endpoints.MapHangfireDashboard();
});

var services = app.Services.CreateScope();

var fs = new FirstStartup(services.ServiceProvider);

await fs.MigrateDbIfPendings();

var appTask = app.RunAsync();

try
{
    await fs.SetupBdForFirstTime()
        .ContinueWith(_ =>
        {
            DataUtils.Initialize(services.ServiceProvider);

            var hangFiresServices = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .Where(t => t.IsClass && !Regex.IsMatch(t.Name, "<[a-zA-Z]*>") && t.Namespace == "AnimeSearch.Services.HangFire")
                .ToList();
        
            foreach(var serviceType in hangFiresServices)
            {
                if (services.ServiceProvider.GetService(serviceType) is not IHangFireService service)
                    continue;
            
                RecurringJob.AddOrUpdate(serviceType.Name[..serviceType.Name.IndexOf("Service", StringComparison.InvariantCulture)],
                    () => service.Execute(), 
                    service.GetCron());
            }
        });
}
catch (Exception e)
{
    CoreUtils.AddExceptionError("l'initialisation de la bdd et des données", e, "System");
}

await appTask;