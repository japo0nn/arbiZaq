using arbiZaqRateGetter.Data.Abstract;

namespace arbiZaqRateGetter.Data
{
    public class Ticker : Entity
    {
        public Guid ExchangerId { get; set; }
        public Exchanger Exchanger { get; set; }
        public DateTime? UpdateTime { get; set; } = DateTime.UtcNow;
        public string Symbol { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public decimal Volume { get; set; }
        public List<TickerNet> TickerNets { get; set; } = new();
        public List<Network> Networks { get; set; } = new();
    }
}
