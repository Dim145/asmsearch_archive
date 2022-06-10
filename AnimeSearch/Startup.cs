using AnimeSearch.Database;
using AnimeSearch.Services;
using JavaScriptEngineSwitcher.Extensions.MsDependencyInjection;
using JavaScriptEngineSwitcher.V8;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using React.AspNet;
using System;
using System.Globalization;

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
            services.AddDbContext<AsmsearchContext>(x => x.UseSqlServer(Configuration.GetConnectionString("SQLServer")),
                ServiceLifetime.Transient, ServiceLifetime.Singleton);

            services.AddControllersWithViews().AddNewtonsoftJson();
            services.AddRouting(options => options.LowercaseUrls = true);
            services.AddSession(options => options.IdleTimeout = TimeSpan.FromHours(1));

            services.AddHostedService<CitationService>();
            services.AddHostedService<ResteEnVieService>();
            services.AddHostedService<DonsTimeoutService>();
            services.AddHostedService<CheckSites>();

            services.AddTransient<MailService>();

            services.AddSingleton(new Utilities(Configuration));

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddReact();

            // Make sure a JS engine is registered, or you will get an error!
            services.AddJsEngineSwitcher(options => options.DefaultEngineName = V8JsEngine.EngineName).AddV8();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var supportedCultures = new[] { new CultureInfo("fr-FR") };
            
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
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseHttpMethodOverride();

            app.UseHttpsRedirection();

            // Initialise ReactJS.NET. Must be before static files.
            app.UseReact(config =>
            {
                Utilities.ConfigReact = config;

                // If you want to use server-side rendering of React components,
                // add all the necessary JavaScript files here. This includes
                // your components as well as all of their dependencies.
                // See http://reactjs.net/ for more information. Example:
                //config
                //  .AddScript("~/js/First.jsx")
                //  .AddScript("~/js/Second.jsx");
                config.UseDebugReact = Utilities.IN_DEV;

                config.AddScriptWithoutTransform("~/lib/react/react-table.min.js");
                config.AddScript("~/js/react/Test.jsx");

                // If you use an external build too (for example, Babel, Webpack,
                // Browserify or Gulp), you can improve performance by disabling
                // ReactJS.NET's version of Babel and loading the pre-transpiled
                // scripts. Example:
                //config
                //  .SetLoadBabel(false)
                //  .AddScriptWithoutTransform("~/js/bundle.server.js");
            });

            app.UseStaticFiles();

            app.UseRouting();
            app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
