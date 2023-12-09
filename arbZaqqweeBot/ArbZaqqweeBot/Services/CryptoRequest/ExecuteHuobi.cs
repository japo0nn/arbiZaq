using ArbZaqqweeBot.Services.CryptoRequest.Huobi;
using ArbZaqqweeBot.Services.CryptoRequest.OKX;

namespace ArbZaqqweeBot.Services.CryptoRequest
{
    public class ExecuteHuobi : BackgroundService
    {
        public IServiceProvider _services { get; }

        public ExecuteHuobi(IServiceProvider services)
        {
            _services = services;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    IHuobiService scopedProcessingService = scope.ServiceProvider.GetRequiredService<IHuobiService>();
                    await scopedProcessingService.GetCoinDataAsync();
                }
            }
        }
    }
}
