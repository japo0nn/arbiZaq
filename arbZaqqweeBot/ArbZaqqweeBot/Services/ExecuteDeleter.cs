using ArbZaqqweeBot.Services.DuplicateDeleter;

namespace ArbZaqqweeBot.Services
{
    public class ExecuteDeleter : BackgroundService
    {
        public IServiceProvider _services { get; }

        public ExecuteDeleter(IServiceProvider services)
        {
            _services = services;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    IDeleter scopedProcessingService = scope.ServiceProvider.GetRequiredService<IDeleter>();
                    await scopedProcessingService.DeleteDuplicates();
                }
            }
        }
    }
}
