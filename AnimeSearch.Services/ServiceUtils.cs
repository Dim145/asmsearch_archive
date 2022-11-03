using AnimeSearch.Data.Models;
using AnimeSearch.Services.Background;
using AnimeSearch.Services.HangFire;
using Hangfire;
using Hangfire.Storage;
using HtmlAgilityPack;

namespace AnimeSearch.Services;

public static class ServiceUtils
{
    public static List<BaseService> SERVICES { get; } = new();
    public static string TELEGRAM_INVITE_LINK { get; private set; } = string.Empty;
    public static string DISCORD_INVITE_LINK { get; private set; } = string.Empty;
    
    public static Random RANDOM { get; } = new(new Random().Next(int.MaxValue));
    
    public static bool SetTelegramLink(string username)
    {
        if (!string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(TELEGRAM_INVITE_LINK))
            return (TELEGRAM_INVITE_LINK = "https://t.me/" + username) != "https://t.me/";

        return false;
    }

    public static bool SetDiscordLink(ulong botId)
    {
        if (botId > 0 && string.IsNullOrWhiteSpace(DISCORD_INVITE_LINK))
            return (DISCORD_INVITE_LINK = $"https://discord.com/api/oauth2/authorize?client_id={botId}&permissions=274878008384&scope=bot") != "";

        return false;
    }

    /// <summary>
    ///     Exécute une requête sur une adresse URL puis renvoi la réponse de celui-ci.
    /// </summary>
    /// <param name="url">Une URL (ex = "https://google.com")</param>
    /// <param name="client"></param>
    /// <returns>True si le site répond, false sinon</returns>
    public static async Task<bool> TestUrl(string url, HttpClient client)
    {
        try
        {
            if(!url.StartsWith("https"))
            {
                HttpClientHandler handler = new()
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };

                client = new(handler);
            }

            var response = await client.GetAsync(url);

            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    public static async Task<bool> IsDomainUrl(string url, HttpClient client)
    {
        try
        {
            if(!url.StartsWith("https"))
            {
                HttpClientHandler handler = new()
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };

                client = new(handler);
            }

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode) 
                return false;

            return (await response.Content.ReadAsStringAsync()).Contains("ASM Search");

        }
        catch (Exception)
        {
            return false;
        }
    }

    public static TimeSpan GetTimeBeforeService(Type hangFireService)
    {
        var name = hangFireService.Name;

        name = name[..name.IndexOf("Service", StringComparison.InvariantCulture)];

        var job = JobStorage.Current.GetConnection().GetRecurringJobs()
            .FirstOrDefault(j => j.Id == name);

        if (job is null)
            return TimeSpan.MinValue;

        return DateTime.UtcNow - (job.NextExecution ?? DateTime.UtcNow);
    }
    
    public static TimeSpan GetTimeBeforeService<T>() where T: IHangFireService
    {
        return GetTimeBeforeService(typeof(T));
    }
}