using arbiZaqTransaction;
using arbiZaqTransaction.Context;
using arbiZaqTransaction.Services;
using arbiZaqTransaction.Services.TickerChecker;
using arbiZaqTransaction.Services.Transactor;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddDbContext<ArbiZaqDbContext>();

        services.AddHostedService<Executor>();

        services.AddScoped<ITransactor, Transactor>();
        services.AddScoped<IChecker, Checker>();
    })
    .Build();

await host.RunAsync();
