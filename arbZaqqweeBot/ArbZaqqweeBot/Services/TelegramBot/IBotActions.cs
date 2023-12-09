using Telegram.Bot.Requests;

namespace ArbZaqqweeBot.Services.TelegramBot
{
    public interface IBotActions
    {
        Task SendMessageAsync(string message);
    }
}
