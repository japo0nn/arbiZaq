using arbiZaqRateGetter.Services.CryptoRequest.Kucoin;
using arbiZaqRateGetter.Services.CryptoRequest.MEXC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace arbiZaqRateGetter.Services.CryptoRequest
{
    public class ExecuteMEXC : BackgroundService
    {
        public IServiceProvider _services { get; }

        public ExecuteMEXC(IServiceProvider services)
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
                    await scopedProcessingService.GetTickersAsync();
                }
            }
        }
    }
}
