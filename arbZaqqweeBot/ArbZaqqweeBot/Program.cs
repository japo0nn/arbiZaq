using ArbZaqqweeBot.Helpers;
using ArbZaqqweeBot.Services.Analyzer;
using ArbZaqqweeBot.Services.TelegramBot;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace ArbZaqqweeBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MapperInitalizer.Initialize();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}