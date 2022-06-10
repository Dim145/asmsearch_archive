using AnimeSearch.Attributes;
using AnimeSearch.Database;
using AnimeSearch.Services;
using BlazorTable;
using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace AnimeSearch
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddServerSideBlazor((a) => a.DisconnectedCircuitMaxRetained = 5);

            services.AddBlazoredLocalStorage();

            services.AddBlazorTable();
            services.AddSweetAlert2(options => options.Theme = SweetAlertTheme.Dark);

            services.AddIdentity<Users, Roles>(options =>
           {
               options.Password.RequiredLength = 6;
               options.Password.RequireLowercase = true;
               options.Password.RequireUppercase = true;
               options.Password.RequireNonAlphanumeric = false;
               options.Password.RequireDigit = true;
           })
            .AddEntityFrameworkStores<AsmsearchContext>();

            services.ConfigureApplicationCookie(o =>
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

            services.AddControllersWithViews().AddNewtonsoftJson();

            services.AddRouting(options => options.LowercaseUrls = true);

            services.AddSingleton(Utilities.GetInstance(Configuration));

            services.AddDbContext<AsmsearchContext>(x => x.UseSqlServer(Utilities.SQL_SERVER_CONNECTIONS_STRING),
                ServiceLifetime.Transient, ServiceLifetime.Transient);

            services.AddHostedService<CitationService>();
            services.AddHostedService<ResteEnVieService>();
            services.AddHostedService<DonsTimeoutService>();
            services.AddHostedService<CheckSites>();

            services.AddHostedService<DiscordService>();
            services.AddHostedService<TelegramService>();

            services.AddTransient<MailService>();

            services.AddSentry();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var supportedCultures = Utilities.Tab( new CultureInfo("fr-FR") );
            
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("fr-FR"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                Utilities.IN_DEV = true;
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
                FileProvider = new PhysicalFileProvider(Path.Combine(env.WebRootPath, "lib/live/api")),
                RequestPath = "/live2dapi",
                ContentTypeProvider = provider
            });

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSentryTracing();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}"
                );
                
                endpoints.MapBlazorHub();
            });
        }
    }
}
