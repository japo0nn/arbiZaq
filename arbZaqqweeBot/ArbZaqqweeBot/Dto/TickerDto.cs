using ArbZaqqweeBot.Data;
using ArbZaqqweeBot.Dto.Abstract;

namespace ArbZaqqweeBot.Dto
{
    public class TickerDto : MainDto
    {
        public ExchangerDto Exchanger { get; set; }
        public string Symbol { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
    }
}
