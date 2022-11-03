using AnimeSearch.Core;
using AnimeSearch.Core.Extensions;
using AnimeSearch.Core.Models.Api;
using AnimeSearch.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AnimeSearch.Data;

public class FirstStartup
{
    private readonly AsmsearchContext _database;
    private readonly UserManager<Users> _userManager;
    private readonly RoleManager<Roles> _roleManager;

    public FirstStartup(IServiceProvider provider)
    {
        _database = provider.GetService<AsmsearchContext>();
        _roleManager = provider.GetRequiredService<RoleManager<Roles>>();
        _userManager = provider.GetRequiredService<UserManager<Users>>();
    }

    public async Task MigrateDbIfPendings()
    {
        if(await _database.Database.CanConnectAsync())
        {
            if ((await _database.Database.GetPendingMigrationsAsync()).Any())
                await _database.Database.MigrateAsync();
        }
    }

    public async Task SetupBdForFirstTime()
    {
        bool isDatabaseWritten = false;

        if (!await _database.TypeSites.AnyAsync())
        {
            await _database.TypeSites.AddRangeAsync(TypeEnum.TabTypes.Select(t => new TypeSite { Name = t }));

            isDatabaseWritten = true;
        }

        if (!await _database.Roles.AnyAsync())
        {
            await _roleManager.CreateAsync(DataUtils.SuperAdminRole);
            await _roleManager.CreateAsync(DataUtils.AdminRole);

            isDatabaseWritten = true;
        }

        if (!await _database.Users.AnyAsync())
        {
            Users admin = new() { UserName = "Super-Admin", Derniere_visite = DateTime.Now };
            Users guest = new() { UserName = DataUtils.Guest, Derniere_visite = DateTime.Now };

            await _userManager.CreateAsync(admin, "Admin183!!");
            await _userManager.AddToRoleAsync(admin, DataUtils.SuperAdminRole.Name);
            await _database.Users.AddAsync(guest);

            isDatabaseWritten = true;
        }

        if(!await _database.Settings.AnyAsync())
        {
            Setting[] tab = {
                new() { Name = DataUtils.SettingDonName           , TypeValue = typeof(double  ).FullName, JsonValue = "0"           , IsDeletable = false, Description = "Montant (en €) des dons à atteindre pour rentabiliser les couts du serveurs." },
                new() { Name = DataUtils.SettingPaypalMailName    , TypeValue = typeof(string  ).FullName, JsonValue = "\"\""        , IsDeletable = false, Description = "Mail du compte paypal sur lequel les dons seront fait." },
                new() { Name = DataUtils.SettingTokenTelegramName , TypeValue = typeof(string  ).FullName, JsonValue = "\"\""        , IsDeletable = false, Description = "Token du bot donné par \"Bot Father\" qui permet la connection et la création du lien d'invitation." },
                new() { Name = DataUtils.SettingTimeBeforeSsName  , TypeValue = typeof(TimeSpan).FullName, JsonValue = "\"00:05:00\"", IsDeletable = false, Description = "Temps (en heure) avant que la recherche ne puisse plus utilisé la sauvegarde pour accéléré le processus." },
                new() { Name = DataUtils.SettingAdsIdName         , TypeValue = typeof(string  ).FullName, JsonValue = "\"\""        , IsDeletable = false, Description = "Identifiant publicitaire venant de https://www.media.net/ . Il est possible de changer, mais pour cela il faudras changer le code HTML." },
                new() { Name = DataUtils.SettingGoogleSearchIdName, TypeValue = typeof(string  ).FullName, JsonValue = "\"\""        , IsDeletable = false, Description = "Identifiant à fournir à google search console pour identifier le site.\nCet identifiant seras placer dans la balise méta portant le nom 'google-site-verification'" },
                new() { Name = DataUtils.SettingDiscordBotName    , TypeValue = typeof(string  ).FullName, JsonValue = "\"\""        , IsDeletable = false, Description = "Token du bot discord donné par la page développeur de discord qui se trouve à cette adresse: https://discord.com/developers/applications"},
                new() { Name = DataUtils.SettingOpennodeApikey    , TypeValue = typeof(string  ).FullName, JsonValue = "\"\""        , IsDeletable = false, Description = "Clé d'api du site 'https://www.opennode.com/' pour les dons en BitCoin.\nIl faut les droits de withdrawls."},
                new() { Name = DataUtils.SettingHotJarId          , TypeValue = typeof(int     ).FullName, JsonValue = "0"        , IsDeletable = true , Description = "Identifiant donné par le site 'https://insights.hotjar.com/' afin de suivre les actions utilisateurs."},
                new() { Name = DataUtils.SettingRecaptchaSiteKey  , TypeValue = typeof(string  ).FullName, JsonValue = "\"\""        , IsDeletable = false, Description = "Clé publique (site) pour le recaptcha donnée par Google." },
                new() { Name = DataUtils.SettingRecaptchaSecretKey, TypeValue = typeof(string  ).FullName, JsonValue = "\"\""        , IsDeletable = false, Description = "Clé privée (secret) pour le recaptcha donnée par Google." },
            };

            foreach (var s in tab)
                await _database.Settings.AddAsync(s);

            isDatabaseWritten = true;
        }

        if (!await _database.ApiSortTypes.AnyAsync())
        {
            await _database.ApiSortTypes.AddRangeAsync(Enum.GetValues<Sort>().Select(s => new ApiSort {Label = s.ToTitle()}));

            isDatabaseWritten = true;
        }

        if (!await _database.ApiFilterTypes.AnyAsync())
        {
            await _database.ApiFilterTypes.AddRangeAsync(Enum.GetValues<Filter>().Select(f => new ApiFilter() { Label = f.ToString()}));

            isDatabaseWritten = true;
        }

        if (!await _database.Apis.AnyAsync())
        {
            var api = new ApiObject
            {
                Name = "TvMaze",
                Description = "Test",
                ApiUrl = "https://api.tvmaze.com/",
                SingleSearchUrl = "singlesearch/",
                SearchUrl = "search/",
                GlobalSearchUrl = "shows?q=",
                AnimeSearchUrl = "shows?q=",
                MoviesSearchUrl = "shows?q=",
                TvSearchUrl = "shows?q=",
                AnimeIdUrl = "shows/",
                MoviesIdUrl = "shows/",
                TvIdUrl = "shows/",
                SiteUrl = "https://www.tvmaze.com/",
                DiscoverUrl = null,
                PathToResults = "show",
                OtherNamesUrl = "akas",
                PathInOnResObject = "name|country.code"
            };
            
            api.TableFields.Add("id", "id");
            api.TableFields.Add("name", "name");
            api.TableFields.Add("type", "type");
            api.TableFields.Add("image", "image");
            api.TableFields.Add("language", "language");
            api.TableFields.Add("url", "url");
            api.TableFields.Add("premiered", "release_date");
            api.TableFields.Add("genres", "genres");
            api.TableFields.Add("status", "status");
            api.TableFields.Add("score", "popularity");
            api.TableFields.Add("rating", "popularity");

            await _database.Apis.AddAsync(api);

            isDatabaseWritten = true;
        }

        if(isDatabaseWritten)
            await _database.SaveChangesAsync();
    }
}