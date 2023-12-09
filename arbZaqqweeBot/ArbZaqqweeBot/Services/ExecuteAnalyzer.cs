using ArbZaqqweeBot.Services.Analyzer;

namespace ArbZaqqweeBot.Services
{
    public class ExecuteAnalyzer : BackgroundService
    {
        public IServiceProvider _services { get; }

        public ExecuteAnalyzer(IServiceProvider services)
        {
            _services = services;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    IAnalyzerService scopedProcessingService = scope.ServiceProvider.GetRequiredService<IAnalyzerService>();
                    await scopedProcessingService.AnalyzeTickersAsync();
                }
            }
        }
    }
}
