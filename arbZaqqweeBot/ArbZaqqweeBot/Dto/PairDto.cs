using ArbZaqqweeBot.Dto.Abstract;

namespace ArbZaqqweeBot.Dto
{
    public class PairDto : MainDto
    {
        public string Symbol { get; set; }
        public TickerDto BuyTicker { get; set; }
        public TickerDto SellTicker { get; set; }
        public decimal Spread { get; set; } = 0;
    }
}
