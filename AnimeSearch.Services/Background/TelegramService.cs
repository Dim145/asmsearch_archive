using System.Reflection;
using System.Text.RegularExpressions;
using AnimeSearch.Core;
using AnimeSearch.Core.Extensions;
using AnimeSearch.Core.Models.Search;
using AnimeSearch.Core.ViewsModel;
using AnimeSearch.Data;
using AnimeSearch.Data.Models;
using AnimeSearch.Services.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AnimeSearch.Services.Background;

public class TelegramService : BaseService
{
    private readonly AsmsearchContext _database;
    private readonly SiteSearchService _siteSearchService;
    private readonly ApiService _apiService;
    private readonly ReceiverOptions receiverOptions;
    private readonly IServiceScope scope;

    private CancellationTokenSource cts;
    private TelegramBotClient client;

    private readonly Dictionary<long, BotCommands> previousCommand;

    public TelegramService(AsmsearchContext database, IServiceScopeFactory serviceScopeFactory) : base("Bot Télégram", TimeSpan.FromHours(1), "Contrôle le bot permettant d'éxécuter des recherches directement depuis Télégram.")
    {
        _database = database;

        scope = serviceScopeFactory.CreateScope();
        _siteSearchService = scope.ServiceProvider.GetService<SiteSearchService>();
        _apiService = scope.ServiceProvider.GetService<ApiService>();

        receiverOptions = new()
        {
            AllowedUpdates = { },
        };

        previousCommand = new();

        ExecutionCode();
    }

    public override Task ExecutionCode()
    {
        if (!IsRunning && (cts == null || cts.IsCancellationRequested))
        {
            try
            {
                var setting = _database.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.Name == DataUtils.SettingTokenTelegramName).GetAwaiter().GetResult();

                string token = setting.GetValueObject();

                client = new TelegramBotClient(token);

                var me = client.GetMeAsync().GetAwaiter().GetResult();

                if (me == null || string.IsNullOrWhiteSpace(me.Username)) throw new Exception("Bot user is null");

                ServiceUtils.SetTelegramLink(me.Username);
            }
            catch (Exception ex)
            {
                CoreUtils.AddExceptionError("le redémarrage du bot Télégram", ex);

                StopAsync(default);
            }

            StartAsync(new());
        }

        return Task.CompletedTask;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        if(IsRunning)
            return Task.CompletedTask;

        cts = new();

        if(client != null)
            client.StartReceiving(HandleUpdate, HandleError, receiverOptions, cts.Token);

        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        if (cts != null)
        {
            cts.Cancel();
            cts = null;
        }

        return base.StopAsync(cancellationToken);
    }

    private async Task HandleUpdate(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message       ) return;
        if (update.Message!.Type != MessageType.Text) return;

        long chatId     = update.Message.Chat.Id;
        string message  = update.Message.Text;
        string username = update.Message!.From!.Username;

        var user = await _database.Users.FirstOrDefaultAsync(u => u.UserName == username, cancellationToken);

        if (user == null)
        {
            user = new Users() { UserName = username };
            user.Derniere_visite = DateTime.Now;

            _database.Users.Add(user);
        }
        else
        {
            user.Derniere_visite = DateTime.Now;
            _database.Update(user);
        }

        await _database.SaveChangesAsync(cancellationToken);

        if (message.StartsWith("/"))
        {
            message = message[1..];

            foreach(BotCommands cmd in Enum.GetValues<BotCommands>())
            {
                if (Enum.GetName(cmd).ToLower() == message.Trim())
                {
                    long userId = update.Message.From.Id;

                    if (previousCommand.ContainsKey(update.Message.From.Id))
                        previousCommand.Remove(update.Message.From.Id);

                    previousCommand[update.Message.From.Id] = cmd;

                    await client.SendTextMessageAsync(chatId, "Veuillez saisir une recherche", cancellationToken: cancellationToken);
                }
                else
                {
                    string name = Enum.GetName(cmd).ToLower() + " ";

                    if (message.StartsWith(name))
                    {
                        ExecCommandByName(cmd, message[name.Length..], user, chatId);
                    }
                }
            }
        }
        else
        {
            if(previousCommand.ContainsKey(update.Message.From.Id))
            {
                BotCommands? command = previousCommand[update.Message.From.Id];
                previousCommand.Remove(update.Message.From.Id);

                if(command != null)
                    ExecCommandByName(command.GetValueOrDefault(), message, user, chatId);
            }
            else
            {
                await client.SendTextMessageAsync(chatId, "Je ne suis point disposé à converser avec vous...", cancellationToken: cancellationToken);
            }
        }
    }

    private Task HandleError(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
    {
        CoreUtils.AddExceptionError("bot télégram", exception);

        return StopAsync(cancellationToken);
    }

    private async Task Search(string search, Users databaseUser, long chatId)
    {
        await client.SendTextMessageAsync(chatId, $"Je lance une recherche pour les mots clé '{search}'...");

        var res = await _siteSearchService.OptiSearch(search, null, await _database.Sites.Where(s => s.Etat == EtatSite.VALIDER).ToListAsync());

        if (res != null && res.Result.Url != null && res.SearchResults.Values.Any(m => m.NbResults > 0))
        {
            Recherche r = await _database.Recherches.FirstOrDefaultAsync(item => item.User_ID == databaseUser.Id && item.recherche == search);

            if (r == null)
            {
                r = new()
                {
                    User_ID = databaseUser.Id,
                    recherche = res.Result.Name,
                    Nb_recherches = 1,
                    Derniere_Recherche = DateTime.Now,
                    Source = SearchSource.Telegram
                };

                await _database.Recherches.AddAsync(r);
            }
            else
            {
                r.Nb_recherches++;
                r.Derniere_Recherche = DateTime.Now;
                r.Source = SearchSource.Telegram;

                _database.Recherches.Update(r);
            }

            await _database.SaveChangesAsync();

            string response = "Résultats:\n";

            foreach (string siteName in res.SearchResults.Keys)
            {
                var sn = Regex.Replace(siteName, @"([()!\-|\\*\.=+])", @"\$1");
                var site = res.SearchResults[siteName];
                var url = new Uri(string.IsNullOrWhiteSpace(site.Url) ? site.SiteUrl : site.Url).GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped);
                url = Regex.Replace(url, @"([()!\-|\\*\.=+])", @"\$1");

                response += $"{site.NbResults} résultats pour le site [{sn}]({url})\n";
            }

            await client.SendTextMessageAsync(chatId, response, ParseMode.MarkdownV2);
        }
    }

    private async Task Infos(string search, Users databaseUser, long chatId)
    {
        await client.SendTextMessageAsync(chatId, "Recherche des infos en cours...");

        var res = await _apiService.SearchResult(search);
        var infos = await _siteSearchService.GetInfosAndNba(res, null);
        var genres = res.GetGenres();

        if (res.Url != null)
        {
            string response = $"Résultat: {Regex.Replace(res.Name, @"([()!\-|\\*\.=+])", @"\$1")}\n";                    

            if (genres?.Length > 0)
                response += $"genres: {Regex.Replace(string.Join(", ", genres), @"([()!\-|\\*\.=+])", @"\$1")}\n";

            response += $"Date de sortie: {res.ReleaseDate.GetValueOrDefault():dd/MM/yyyy}\n";


            if (infos[1] != null || !infos[0].Contains("404"))
                response += $"Autres : \n{(infos[0].Contains("404") ? "" : $"[\\+ d'infos]({Regex.Replace(infos[0], @"([()!\-|\\*\.=+])", @"\$1")})\n{(infos[1] != null ? $"[bande\\-annonce]({Regex.Replace(infos[1], @"([()!\-|\\*\.=+])", @"\$1")})" : "")}")}";

            if (res.Image != null)
                await client.SendPhotoAsync(chatId, photo: new(res.Image), caption: response, parseMode: ParseMode.MarkdownV2);
            else
                await client.SendTextMessageAsync(chatId, response, ParseMode.MarkdownV2);
        }
        else
        {
            await client.SendTextMessageAsync(chatId, "Hummmm ... Je n'ai pas trouvé de résultats.");
        }
    }

    private async Task Msearch(string search, Users databaseUser, long chatId)
    {
        await client.SendTextMessageAsync(chatId, "Je recherche des résultats...");

        ModelMultiSearch[] res = null;//await _siteSearchService.MultiSearch(search);

        if (res == null || res.Length == 0)
        {
            await client.SendTextMessageAsync(chatId, "Aucuns résultats pour votre recherche...");
        }
        else
        {
            string response = $"{res.Length} résultats de trouvée: \n\n";

            foreach (ModelMultiSearch model in res)
            {
                string name = Regex.Replace(model.Name, @"([()!\-|\\*\.=+])", @"\$1");
                string date = model.Date.GetValueOrDefault().ToString("dd/MM/yyyy");
                string url  = new Uri(CoreUtils.BaseUrl + model.Lien).GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped);
                url = Regex.Replace(url, @"([()!\-|\\*\.=+])", @"\$1");

                response += $"[{name}]({url}):\nType: {model.Type}{(!model.Date.HasValue ? "" : $"\nSortie le {date}")}\n\n";
            }

            await client.SendTextMessageAsync(chatId, response, ParseMode.Markdown);
        }
    }

    private void ExecCommandByName(BotCommands command, string search, Users databaseUser, long chatId)
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
            var res = method.Invoke(this, CoreUtils.Tab(search, databaseUser, chatId));

            if (res is not null and Task t)
                t.Wait();
        }
    }
}