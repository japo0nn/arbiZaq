using arbiZaqRateGetter.Data.Abstract;
using System.Text.Json.Serialization;

namespace arbiZaqRateGetter.Data
{
    public class Network : Entity
    {
        public Guid ExchangerId {  get; set; }
        public Exchanger Exchanger { get; set; }
        public string Coin {  get; set; }
        public string Name { get; set; }
        public string ShortName {  get; set; }
        public string ChainId { get; set; }
        public decimal? Fee { get; set; }
        public bool DepositEnable { get; set; }
        public bool WithdrawEnable { get; set; }
        
        public List<TickerNet> TickerNets { get; set; } = new();
        public List<Ticker> Tickers { get; set; } = new();
    }
}
