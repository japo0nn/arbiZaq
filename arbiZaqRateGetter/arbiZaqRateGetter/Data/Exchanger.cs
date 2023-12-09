using arbiZaqRateGetter.Data.Abstract;

namespace arbiZaqRateGetter.Data
{
    public class Exchanger : Entity
    {
        public string Name { get; set; }
        public List<Ticker> TickerList {  get; set; }
    }
}
