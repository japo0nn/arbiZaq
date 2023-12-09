using ArbZaqqweeBot.Services.CryptoRequest.Binance;

namespace ArbZaqqweeBot.Services.CryptoRequest
{
    public class ExecuteBinance : BackgroundService
    {
        public IServiceProvider _services { get; }

        public ExecuteBinance(IServiceProvider services)
        {
            _services = services;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    IBinanceService scopedProcessingService = scope.ServiceProvider.GetRequiredService<IBinanceService>();
                    await scopedProcessingService.GetCoinDataAsync();
                }
            }
        }
    }
}
