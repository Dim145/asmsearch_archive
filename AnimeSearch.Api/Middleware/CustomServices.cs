using AnimeSearch.Data;
using AnimeSearch.Data.Models;
using AnimeSearch.Services.Background;
using AnimeSearch.Services.Database;
using AnimeSearch.Services.HangFire;
using AnimeSearch.Services.Mails;
using AnimeSearch.Services.Recaptcha;
using AnimeSearch.Services.Search;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace AnimeSearch.Api.Middleware;

public static class CustomServices
{
    public static WebApplicationBuilder AddApiServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddIdentity<Users, Roles>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = true;
            })
            .AddEntityFrameworkStores<AsmsearchContext>();
        
        builder.Services.AddRouting(options => options.LowercaseUrls = true);
        
        builder.Services.AddDbContext<AsmsearchContext>(x =>
            {
                x.UseSqlServer(builder.Configuration.GetConnectionString("SQLServer"),
                    o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
            },
            ServiceLifetime.Transient, ServiceLifetime.Transient);

        var mailSettings = builder.Configuration.GetSection("MailSettings");

        builder.Services.AddFluentEmail(mailSettings["Mail"])
            .AddRazorRenderer()
            .AddSmtpSender(
                mailSettings["Host"],
                int.TryParse(mailSettings["Port"], out var port) ? port : 465,
                mailSettings["Username"],
                mailSettings["Password"]);

        builder.Services.AddHttpClient<ApiService>();
        builder.Services.AddHttpClient<DuckDuckGoSearch>();
        builder.Services.AddHttpClient<NautiljonService>();
        builder.Services.AddHttpClient<CheckDomainsService>();
        builder.Services.AddHttpClient<CheckEpUrlsService>();

        builder.Services.AddScoped<DuckDuckGoSearch>();
        builder.Services.AddScoped<NautiljonService>();
        builder.Services.AddScoped<ApiService>();
        builder.Services.AddScoped<SiteSearchService>();
        
        builder.Services.AddScoped<CheckSiteService>();
        builder.Services.AddScoped<CitationService>();
        builder.Services.AddScoped<KeepAliveService>();
        builder.Services.AddScoped<DonsTimeoutService>();
        builder.Services.AddScoped<RecaptchaService>();
        builder.Services.AddScoped<DatasUtilsService>();
        builder.Services.AddScoped<GenreMajService>();
        builder.Services.AddScoped<CheckDomainsService>();
        builder.Services.AddScoped<CheckEpUrlsService>();
        
        builder.Services.AddHostedService<DiscordService>();
        builder.Services.AddHostedService<TelegramService>();

        builder.Services.AddTransient<MailService>();

        builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(builder.Configuration.GetConnectionString("SQLServer"), new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true
            }));

        builder.Services.AddHangfireServer();

        builder.Services.AddControllers();

        return builder;
    }
}