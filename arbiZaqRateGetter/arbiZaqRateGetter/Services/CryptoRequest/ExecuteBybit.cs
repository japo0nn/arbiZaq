using arbiZaqRateGetter.Services.CryptoRequest.Bybit;
using arbiZaqRateGetter.Services.CryptoRequest.OKX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace arbiZaqRateGetter.Services.CryptoRequest
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
                    await scopedProcessingService.GetTickersAsync();
                }
            }
        }
    }
}
