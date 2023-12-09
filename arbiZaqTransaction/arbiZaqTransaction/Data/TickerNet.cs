using arbiZaqTransaction.Data.Abstract;

namespace arbiZaqTransaction.Data
{
    public class TickerNet
    {
        public Guid? TickerId { get; set; }
        public Ticker? Ticker { get; set; }

        public Guid? NetworkId { get; set; }
        public Network? Network { get; set; }
    }
}
