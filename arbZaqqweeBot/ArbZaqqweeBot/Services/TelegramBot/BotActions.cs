using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace ArbZaqqweeBot.Services.TelegramBot
{
    public class BotActions : IBotActions
    {
        static readonly string chanelId = "-1001957071804";
        static ITelegramBotClient bot = new TelegramBotClient("6496738525:AAFvFCMDF_138WYSLPQZf8yyThHyt5ANS0M");
        private IServiceScopeFactory _scopeFactory;

        public BotActions(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task SendMessageAsync(string message)
        {
            await bot.SendTextMessageAsync(
                chatId: chanelId,
                text: message,
                parseMode: ParseMode.Html,
                disableNotification: false);
        }
    }
}
