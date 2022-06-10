using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnimeSearch.Services
{
    public abstract class BaseService: BackgroundService
    {
        public string Title { get; private set; }
        public TimeSpan Periode { get; private set; }
        public Timer Timer { get; private set; }
        public bool IsRunning { get; private set; }
        public string Descr { get; private set; }

        public BaseService(string title, TimeSpan periode, string descr = "")
        {
            Title = title;
            Periode = periode;
            Descr = descr;
            IsRunning = false;

            Utilities.SERVICES.Add(this);
        }

        protected override sealed Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(() =>
        {
            Timer = new(async (obj) => { if (IsRunning) await ExecutionCode(); }, null, TimeSpan.Zero, Periode);

        }, stoppingToken);

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await base.StartAsync(cancellationToken);

            IsRunning = true;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);

            IsRunning = false;
        }

        public abstract Task ExecutionCode();

        public override bool Equals(object obj)
        {
            if(obj is not null and BaseService other)
            {
                return Title == other.Title && Timer == other.Timer;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Title.GetHashCode() ^ Timer.GetHashCode();
        }
    }
}
