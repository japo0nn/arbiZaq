using ArbZaqqweeBot.Services.CryptoRequest.ByBit;
using ArbZaqqweeBot.Services.CryptoRequest.MEXC;

namespace ArbZaqqweeBot.Services.CryptoRequest
{
    public class ExecuteMexc : BackgroundService
    {
        public IServiceProvider _services { get; }

        public ExecuteMexc(IServiceProvider services)
        {
            _services = services;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    IMexcService scopedProcessingService = scope.ServiceProvider.GetRequiredService<IMexcService>();
                    await scopedProcessingService.GetCoinDataAsync();
                }
            }
        }
    }
}
