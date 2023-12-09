using arbiZaqRateGetter.Services.CryptoRequest.Huobi;
using arbiZaqRateGetter.Services.CryptoRequest.Kucoin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace arbiZaqRateGetter.Services.CryptoRequest
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
                    await scopedProcessingService.GetTickersAsync();
                }
            }
        }
    }
}
