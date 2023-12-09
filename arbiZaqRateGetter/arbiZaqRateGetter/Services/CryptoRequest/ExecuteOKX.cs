using arbiZaqRateGetter.Services.CryptoRequest.Kucoin;
using arbiZaqRateGetter.Services.CryptoRequest.OKX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace arbiZaqRateGetter.Services.CryptoRequest
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
                    await scopedProcessingService.GetTickersAsync();
                }
            }
        }
    }
}
