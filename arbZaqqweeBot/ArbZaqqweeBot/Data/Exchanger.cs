using ArbZaqqweeBot.Data.Abstract;

namespace ArbZaqqweeBot.Data
{
    public class Exchanger : Entity
    {
        public string Name { get; set; }
        public List<Ticker> TickerList {  get; set; }
    }
}
