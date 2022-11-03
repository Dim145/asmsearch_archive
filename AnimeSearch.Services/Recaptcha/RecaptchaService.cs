using AnimeSearch.Core;
using AnimeSearch.Data;
using Newtonsoft.Json;

namespace AnimeSearch.Services.Recaptcha;

public class RecaptchaService
{
    private HttpClient Client { get; }
    private AsmsearchContext Database { get; }
    
    private string SecretKey { get; }

    public RecaptchaService(HttpClient client, AsmsearchContext database)
    {
        Client = client;
        Database = database;

        SecretKey = Database.Settings.GetValueOrDefault(DataUtils.SettingRecaptchaSecretKey)?.ToString();
    }

    public async Task<bool> IsValid(string token)
    {
        if (string.IsNullOrWhiteSpace(SecretKey) || string.IsNullOrWhiteSpace(token))
            return false;
        
        var response = await Client.GetAsync($"https://www.google.com/recaptcha/api/siteverify?secret={SecretKey}&response={token}");

        if (response.IsSuccessStatusCode)
        {
            var result = JsonConvert.DeserializeAnonymousType(await response.Content.ReadAsStringAsync(), new
            {
                success = false,
                challenge_ts = DateTime.MinValue,
                hostname = string.Empty,
                score = 0f
            });

            return (result?.score ?? 0) > 0.7f;
        }

        return false;
    }
}