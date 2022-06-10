using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace AnimeSearch
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var app = CreateHostBuilder(args).Build();

            using var scope = app.Services.CreateScope();
            
            var fs= new FirstStartup(scope.ServiceProvider);

            await fs.MigrateDbIfPendings();

            _ = fs.SetupBdForFirstTime(); // rien ne sert d'attendre la création des premières données. Elle ne sont pas utilis au démarage

            app.Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseSentry(o =>
                    {
                        o.MaxQueueItems = 50;

                    }).UseStartup<Startup>();
                });
    }
}
