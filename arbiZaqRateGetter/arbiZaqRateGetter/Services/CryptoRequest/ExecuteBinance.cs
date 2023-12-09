using arbiZaqRateGetter.Services.CryptoRequest.Binance;
using arbiZaqRateGetter.Services.CryptoRequest.Kucoin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace arbiZaqRateGetter.Services.CryptoRequest
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
                    await scopedProcessingService.GetTickersAsync();
                }
            }
        }
    }
}
