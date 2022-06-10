using AnimeSearch.Database;
using AnimeSearch.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

using static AnimeSearch.Utilities;

namespace AnimeSearch
{
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
                await _database.TypeSites.AddRangeAsync(TypeEnum.TabTypes.Select(t => new TypeSite() { Name = t }));

                isDatabaseWritten = true;
            }

            if (!await _database.Roles.AnyAsync())
            {
                await _roleManager.CreateAsync(SUPER_ADMIN_ROLE);
                await _roleManager.CreateAsync(ADMIN_ROLE);

                isDatabaseWritten = true;
            }

            if (!await _database.Users.AnyAsync())
            {
                Users admin = new() { UserName = "Super-Admin", Derniere_visite = DateTime.Now };
                Users guest = new() { UserName = GUEST, Derniere_visite = DateTime.Now };

                await _userManager.CreateAsync(admin, "Admin183!!");
                await _userManager.AddToRoleAsync(admin, SUPER_ADMIN_ROLE.Name);
                await _database.Users.AddAsync(guest);

                isDatabaseWritten = true;
            }

            if(!await _database.Settings.AnyAsync())
            {
                Setting[] tab = new Setting[]
                {
                    new() { Name = SETTING_DON_NAME             , TypeValue = typeof(double  ).FullName, JsonValue = "0"           , IsDeletable = false, Description = "Montant (en €) des dons à atteindre pour rentabiliser les couts du serveurs." },
                    new() { Name = SETTING_PAYPAL_MAIL_NAME     , TypeValue = typeof(string  ).FullName, JsonValue = "\"\""        , IsDeletable = false, Description = "Mail du compte paypal sur lequel les dons seront fait." },
                    new() { Name = SETTING_TOKEN_TELEGRAM_NAME  , TypeValue = typeof(string  ).FullName, JsonValue = "\"\""        , IsDeletable = false, Description = "Token du bot donné par \"Bot Father\" qui permet la connection et la création du lien d'invitation." },
                    new() { Name = SETTING_TIME_BEFORE_SS_NAME  , TypeValue = typeof(TimeSpan).FullName, JsonValue = "\"00:05:00\"", IsDeletable = false, Description = "Temps (en heure) avant que la recherche ne puisse plus utilisé la sauvegarde pour accéléré le processus." },
                    new() { Name = SETTING_ADS_ID_NAME          , TypeValue = typeof(string  ).FullName, JsonValue = "\"\""        , IsDeletable = false, Description = "Identifiant publicitaire venant de https://www.media.net/ . Il est possible de changer, mais pour cela il faudras changer le code HTML." },
                    new() { Name = SETTING_GOOGLE_SEARCH_ID_NAME, TypeValue = typeof(string  ).FullName, JsonValue = "\"\""        , IsDeletable = false, Description = "Identifiant à fournir à google search console pour identifier le site.\nCet identifiant seras placer dans la balise méta portant le nom 'google-site-verification'" },
                    new() { Name = SETTING_DISCORD_BOT_NAME     , TypeValue = typeof(string  ).FullName, JsonValue = "\"\""        , IsDeletable = false, Description = "Token du bot discord donné par la page développeur de discord qui se trouve à cette adresse: https://discord.com/developers/applications"},
                    new() { Name = SETTING_OPENNODE_APIKEY      , TypeValue = typeof(string  ).FullName, JsonValue = "\"\""        , IsDeletable = false, Description = "Clé d'api du site 'https://www.opennode.com/' pour les dons en BitCoin.\nIl faut les droits de withdrawls."}
                };

                foreach (Setting s in tab)
                    await _database.Settings.AddAsync(s);

                isDatabaseWritten = true;
            }

            if(isDatabaseWritten)
                await _database.SaveChangesAsync();
        }
    }
}
