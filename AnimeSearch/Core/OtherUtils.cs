using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AnimeSearch.Core;

public sealed class OtherUtils
{

    /// <summary>
    ///     Exécute une requête sur une adresse URL puis renvoi la réponse de celui-ci.
    /// </summary>
    /// <param name="url">Une URL (ex = "https://google.com")</param>
    /// <returns>True si le site répond, false sinon</returns>
    public static async Task<bool> TestUrl(string url)
    {
        try
        {
            var client = Utilities.CLIENT;

            if(!url.StartsWith("https"))
            {
                HttpClientHandler handler = new()
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) =>
                    {
                        return true;
                    }
                };

                client = new(handler);
            }

            HttpResponseMessage response = await client.GetAsync(url);

            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }
}