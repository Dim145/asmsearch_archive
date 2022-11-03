using AnimeSearch.Core;
using Hangfire;

namespace AnimeSearch.Services.HangFire;

public class KeepAliveService
{
    private HttpClient Client { get; }

    public KeepAliveService(HttpClient client)
    {
        Client = client;
    }


    public async Task Execute()
    {
        try
        {
            var res = await Client.GetAsync(CoreUtils.BaseUrl + "api");

            if (!res.IsSuccessStatusCode)
                throw new Exception($"bad response from me...\n{res.ReasonPhrase}");
        }
        catch (Exception e)
        {
            CoreUtils.AddExceptionError("ResteEnvieService", e);
            throw;
        }
    }

    public string GetCron() => $"*/{2} * * * *"; // toutes les deux minutes

    public string GetDescription() =>
        "Exécute une requête vers la page d'accueil toutes les 2 minutes. Cela permet de maintenir le serveur actif plus longtemps.";
}