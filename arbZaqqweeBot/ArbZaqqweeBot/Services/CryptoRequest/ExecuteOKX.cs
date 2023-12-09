using ArbZaqqweeBot.Services.CryptoRequest.Kucoin;
using ArbZaqqweeBot.Services.CryptoRequest.OKX;

namespace ArbZaqqweeBot.Services.CryptoRequest
{
    public class ExecuteOKX : BackgroundService
    {
        public IServiceProvider _services { get; }

        public ExecuteOKX(IServiceProvider services)
        {
            _services = services;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    IOKXService scopedProcessingService = scope.ServiceProvider.GetRequiredService<IOKXService>();
                    await scopedProcessingService.GetCoinDataAsync();
                }
            }
        }
    }
}
