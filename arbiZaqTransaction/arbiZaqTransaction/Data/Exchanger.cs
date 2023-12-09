using arbiZaqTransaction.Data.Abstract;

namespace arbiZaqTransaction.Data
{
    public class Exchanger : Entity
    {
        public string Name { get; set; }
        public List<Ticker> TickerList {  get; set; }
    }
}
