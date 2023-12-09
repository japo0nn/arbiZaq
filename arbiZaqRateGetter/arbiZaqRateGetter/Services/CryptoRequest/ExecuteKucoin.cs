﻿using arbiZaqRateGetter.Services.CryptoRequest.Kucoin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace arbiZaqRateGetter.Services.CryptoRequest
{
    public class ExecuteKucoin : BackgroundService
    {
        public IServiceProvider _services { get; }

        public ExecuteKucoin(IServiceProvider services)
        {
            _services = services;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    IKucoinService scopedProcessingService = scope.ServiceProvider.GetRequiredService<IKucoinService>();
                    await scopedProcessingService.GetTickersAsync();
                }
            }
        }
    }
}
