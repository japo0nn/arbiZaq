using ArbZaqqweeBot.Services.CryptoRequest.Binance;
using ArbZaqqweeBot.Services.CryptoRequest.ByBit;

namespace ArbZaqqweeBot.Services.CryptoRequest
{
    public class ExecuteBybit : BackgroundService
    {
        public IServiceProvider _services { get; }

        public ExecuteBybit(IServiceProvider services)
        {
            _services = services;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    IBybitService scopedProcessingService = scope.ServiceProvider.GetRequiredService<IBybitService>();
                    await scopedProcessingService.GetCoinDataAsync();
                }
            }
        }
    }
}
