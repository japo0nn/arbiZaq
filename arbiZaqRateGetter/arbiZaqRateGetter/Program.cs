using arbiZaqRateGetter;
using arbiZaqRateGetter.Context;
using arbiZaqRateGetter.Services;
using arbiZaqRateGetter.Services.CryptoRequest;
using arbiZaqRateGetter.Services.CryptoRequest.Binance;
using arbiZaqRateGetter.Services.CryptoRequest.Bybit;
using arbiZaqRateGetter.Services.CryptoRequest.Huobi;
using arbiZaqRateGetter.Services.CryptoRequest.Kucoin;
using arbiZaqRateGetter.Services.CryptoRequest.MEXC;
using arbiZaqRateGetter.Services.CryptoRequest.OKX;
using arbiZaqRateGetter.Services.PairAnalyzer;
using arbiZaqRateGetter.Services.TelegramBot;
using Microsoft.EntityFrameworkCore;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddDbContext<ArbiZaqDbContext>();

        services.AddScoped<IBinanceService, BinanceService>();
        services.AddScoped<IKucoinService, KucoinService>();
        services.AddScoped<IOKXService, OKXService>();
        services.AddScoped<IHuobiService, HuobiService>();
        services.AddScoped<IBybitService, BybitService>();
        services.AddScoped<IMexcService, MexcService>();

        services.AddHostedService<ExecuteBinance>();
        services.AddHostedService<ExecuteKucoin>();
        services.AddHostedService<ExecuteOKX>();
        services.AddHostedService<ExecuteHuobi>();
        services.AddHostedService<ExecuteBybit>();
        services.AddHostedService<ExecuteMEXC>();

        services.AddScoped<IPairsAnalyzer, PairsAnalyzer>();
        services.AddHostedService<ExecutePairsAnalyzer>();

        services.AddScoped<IBotActions, BotActions>();
    })
    .Build();

await host.RunAsync();
