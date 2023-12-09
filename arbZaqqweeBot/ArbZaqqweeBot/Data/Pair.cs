using ArbZaqqweeBot.Data.Abstract;

namespace ArbZaqqweeBot.Data
{
    public class Pair : Entity
    {
        public Guid BuyTickerId { get; set; }
        public Ticker BuyTicker { get; set; }
        public Guid SellTickerId { get; set; }
        public Ticker SellTicker { get; set; }
        public decimal Spread { get; set; } = 0;
        public bool IsSend { get; set; } = false;
        public bool IsValid { get; set; } = true;

        public Guid BuyNetworkId { get; set; } // Currency Network
        public Network BuyNetwork { get; set; }
        public Guid SellNetworkId { get; set; }
        public Network SellNetwork { get; set; }

        /*public Guid WithdrawId { get; set; } // USDT Network
        public Network Withdraw { get; set; }
        public Guid DepositId { get; set; }
        public Network Deposit { get; set; }*/
    }
}
