using arbiZaqRateGetter.Services.CryptoRequest.OKX;
using arbiZaqRateGetter.Services.PairAnalyzer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace arbiZaqRateGetter.Services
{
    public class ExecutePairsAnalyzer : BackgroundService
    {
        public IServiceProvider _services { get; }

        public ExecutePairsAnalyzer(IServiceProvider services)
        {
            _services = services;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    IPairsAnalyzer scopedProcessingService = scope.ServiceProvider.GetRequiredService<IPairsAnalyzer>();
                    await scopedProcessingService.AnalyzePairs();
                }
            }
        }
    }
}
