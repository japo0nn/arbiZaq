using arbiZaqTransaction.Services.Transactor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace arbiZaqTransaction.Services
{
    public class Executor : BackgroundService
    {
        public IServiceProvider _services { get; }

        public Executor(IServiceProvider services)
        {
            _services = services;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    ITransactor scopedProcessingService = scope.ServiceProvider.GetRequiredService<ITransactor>();
                    await scopedProcessingService.StartProcess();
                }
            }
        }
    }
}
