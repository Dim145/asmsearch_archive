using AnimeSearch.Controllers.api;
using AnimeSearch.Database;
using AnimeSearch.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AnimeSearch.Core;
using AnimeSearch.Models.Search;

namespace AnimeSearch.Services
{
    public class DiscordService : BaseService
    {
        private readonly AsmsearchContext _database;

        private DiscordClient client;

        public DiscordService(AsmsearchContext database): base("Bot Discord", TimeSpan.FromDays(1), "Contrôle le bot permettant d'éxécuter des recherches directement depuis Discord.")
        {
            _database = database;

            ExecutionCode();
        }

        public override Task ExecutionCode()
        {
            if(!IsRunning) try
            {
                client = new(new()
                {
                    Token = _database.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.Name == Utilities.SETTING_DISCORD_BOT_NAME).GetAwaiter().GetResult()?.GetValueObject(),
                    TokenType = TokenType.Bot
                });

                client.MessageCreated += async (s, e) =>
                {
                    string message = e.Message.Content.ToLower();
                    string authorName = e.Author.Username;

                    if (message.FirstOrDefault() == '!')
                    {
                        message = message[1..];

                        if (message == "i want you!" && !string.IsNullOrWhiteSpace(Utilities.DISCORD_INVITE_LINK))
                        {
                            await e.Message.RespondAsync($"Voici mon lien: {Utilities.DISCORD_INVITE_LINK}");
                        }
                        else if (message == "commands")
                        {
                            var builder = new DiscordEmbedBuilder();

                            builder.AddField("!i want you!", "Donne un lien d'invitation pour le bot AsmSearch.");
                            builder.AddField("!search {recherche}", "Exécute une recherche selon les mots-clés qui suivent. (il faut retirer les '{}') ");
                            builder.AddField("!infos {recherche}", "Donne des informations basiques sur la recherche.");
                            builder.AddField("!msearch {recherche}", "Exécute une recherche multiple et donnes les différents liens de recherches.");

                            await e.Message.RespondAsync(builder.Build());
                        }
                        else
                        {
                            foreach (BotCommands cmd in Enum.GetValues<BotCommands>())
                            {
                                string name = Enum.GetName(cmd).ToLowerInvariant();

                                if (message.ToLowerInvariant().StartsWith(name + " "))
                                    ExecCommandByName(cmd, message[name.Length..], e.Message, authorName);
                            }
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                Utilities.AddExceptionError("le redémmarrage du bot discord", ex);
                _ = StopAsync(new());

                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            if(client != null)
            {
                var list = await _database.Recherches.Where(s => s.Nb_recherches > 3).Select(s => s.recherche).Distinct().ToListAsync(cancellationToken);

                var name = list.Count == 0 ? string.Empty : list[Utilities.RANDOM.Next(list.Count)];

                try
                {
                    await client.ConnectAsync(new(string.IsNullOrWhiteSpace(name) ? "des animes" : name, ActivityType.Watching));
                    Utilities.SetDiscordLink(client.CurrentUser.Id);
                    await base.StartAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    Utilities.AddExceptionError("le démmarrage du bot Discord", ex);
                    await StopAsync(cancellationToken);
                }
            }
            else
            {
                await StopAsync(cancellationToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if(client != null)
                await client.DisconnectAsync();

            await base.StopAsync(cancellationToken);
        }

        private async Task Search(string search, DiscordMessage message, string authorName)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                await message.RespondAsync("Pourquoi me dérange tu pour rien ?!");
                return;
            }

            var user = await _database.Users.FirstOrDefaultAsync(u => u.UserName == authorName);

            if (user == null)
            {
                user = new Users() { UserName = authorName };
                user.Derniere_visite = DateTime.Now;

                _database.Users.Add(user);
            }
            else
            {
                user.Derniere_visite = DateTime.Now;
                _database.Update(user);
            }

            await _database.SaveChangesAsync();

            await message.RespondAsync("La recherche est lancée, veuillez patientez... (2min max)");

            var res = SiteSearch.OptiSearch(search, _database, null, await _database.Sites.Where(s => s.Etat == EtatSite.VALIDER).ToListAsync());

            if (res.Result.GetUrl() != null && res.SearchResults.Values.Any(m => m.NbResults > 0))
            {
                Recherche r = await _database.Recherches.FirstOrDefaultAsync(item => item.User_ID == user.Id && item.recherche == search);

                if (r == null)
                {
                    r = new()
                    {
                        User_ID = user.Id,
                        recherche = res.Result.GetName(),
                        Nb_recherches = 1,
                        Derniere_Recherche = DateTime.Now,
                        Source = SearchSource.Discord
                    };

                    await _database.Recherches.AddAsync(r);
                }
                else
                {
                    r.Nb_recherches++;
                    r.Derniere_Recherche = DateTime.Now;
                    r.Source = SearchSource.Discord;

                    _database.Recherches.Update(r);
                }

                await _database.SaveChangesAsync();

                List<string[]> strRes = new();

                var keys = res.SearchResults.Keys.ToArray();

                int maxLength = res.SearchResults.Values.Max(s => s.NbResults.ToString().Length);

                foreach (string name in res.SearchResults.Keys)
                {
                    var site = res.SearchResults[name];
                    var url = new Uri(string.IsNullOrWhiteSpace(site.Url) ? site.SiteUrl : site.Url);

                    strRes.Add(new[] { name, site.NbResults.ToString(), url.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped) });
                }

                var builder = new DiscordEmbedBuilder();

                foreach (string[] tab in strRes)
                {
                    builder.AddField($"{tab[0]} :", $"{tab[1]} sur ce [site]({tab[2]})");
                }

                await message.RespondAsync(builder.Build());
            }
            else
            {
                await message.RespondAsync("Je n'ai pas trouver de résultats pour cette recherches...");
            }
        }

        private async Task Msearch(string search, DiscordMessage message, string authorName)
        {
            await message.RespondAsync("Je recherche des résultats...");

            var res = SiteSearch.MultiSearch(search).Take(25);

            if (res == null || !res.Any())
            {
                await message.RespondAsync("Aucuns résultats pour votre recherche...");
            }
            else
            {
                var builder = new DiscordEmbedBuilder();

                foreach (ModelMultiSearch model in res)
                {
                    try
                    {
                        builder.AddField($"{model.Name} :", $"Type: {model.Type}\n{(!model.Date.HasValue ? "" : $"Sortie le {model.Date.GetValueOrDefault():dd/MM/yyyy}.\n")}[lien de recherche]({new Uri(Utilities.BASE_URL + model.Lien).GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped)})");

                        if (builder.ImageUrl is null)
                            builder.WithImageUrl(model.Img);
                    }
                    catch(Exception ex)
                    {
                        Utilities.AddExceptionError("construction multi-search discord", ex);
                    }
                }

                await message.RespondAsync(builder.Build());
            }
        }

        private async Task Infos(string search, DiscordMessage message, string authorName)
        {
            await message.RespondAsync("Recherche des infos en cours...");

            var res = SiteSearch.SearchResult(search);
            var infos = await SiteSearch.GetInfosAndNba(res, null);
            var genres = res.GetGenres();

            if (res.GetUrl() != null)
            {
                var builder = new DiscordEmbedBuilder();

                builder.AddField("Nom (Anglais): ", res.GetName());

                if (res.GetImage() != null)
                    builder.WithImageUrl(res.GetImage());

                if (genres?.Length > 0)
                    builder.AddField("genres: ", string.Join(", ", genres));

                builder.AddField("Date de sortie: ", res.GetRealeaseDate().GetValueOrDefault().ToString("dd/MM/yyyy"));

                infos[0] = new Uri(infos[0]).GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped);


                if (infos[1] != null || !infos[0].Contains("404"))
                    builder.AddField("Autres : ", $"{(infos[0].Contains("404") ? "" : $"[+ d'infos]({infos[0]})\n{(infos[1] != null ? $"[bande-annonce]({infos[1]})" : "")}")}");

                await message.RespondAsync(builder);
            }
            else
            {
                await message.RespondAsync("Hummmm :thinking: ... Je n'ai pas trouvé de résultats.");
            }
        }

        private void ExecCommandByName(BotCommands command, string search, DiscordMessage message, string authorName)
        {
            MethodInfo method = null;

            try
            {
                method = GetType().GetMethod(Enum.GetName(command).ToUpperCamelCase(), BindingFlags.NonPublic | BindingFlags.Instance);
            }
            catch (Exception)
            {

            }

            if (method != null)
            {
                var res = method.Invoke(this, Utilities.Tab(search, message, authorName));

                if (res is not null and Task t)
                    t.Wait();
            }
        }
    }
}
