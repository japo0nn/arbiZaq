using arbiZaqRateGetter.Context;
using arbiZaqRateGetter.Services.TelegramBot;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace arbiZaqRateGetter.Services.PairAnalyzer
{
    public class PairsAnalyzer : IPairsAnalyzer
    {
        private readonly ArbiZaqDbContext _context;
        private readonly IBotActions _botActions;

        public PairsAnalyzer(ArbiZaqDbContext context, IBotActions botActions)
        {
            _context = context;
            _botActions = botActions;
        }

        public async Task AnalyzePairs()
        {
            var pairs = await _context.Pairs
                .Include(x => x.BuyTicker).ThenInclude(x => x.Exchanger)
                .Include(x => x.SellTicker).ThenInclude(x => x.Exchanger)
                .Include(x => x.BuyNetwork)
                .Include(x => x.SellNetwork)
                /*.Include(x => x.Withdraw)*/
                .ToListAsync();

            foreach (var pair in pairs)
            {
                var buyPrice = pair.BuyTicker.BuyPrice;
                var sellPrice = pair.SellTicker.SellPrice;

                if (!pair.BuyNetwork.WithdrawEnable || !pair.SellNetwork.DepositEnable)
                {
                    _context.Pairs.Remove(pair);
                    await _context.SaveChangesAsync();
                }

                var sellPair = 100 / buyPrice * sellPrice;
                var spread = (sellPair / (100 + (pair.BuyNetwork.Fee / buyPrice) /*+ pair.Withdraw.Fee*/ + 1)) - 1;

                pair.Spread = (decimal)spread;

                if (spread >= (decimal)0.001)
                {
                    if (!pair.IsSend) 
                    {
                        await _botActions.SendMessageAsync($"<b>{pair.BuyTicker.Exchanger.Name} -> {pair.SellTicker.Exchanger.Name}</b>\n" +
                                                $"Пара: {pair.BuyTicker.Symbol.ToUpper()}\n" +
                                                $"Покупка: {buyPrice} ({pair.BuyTicker.UpdateTime})\n" +
                                                $"Продажа: {sellPrice} ({pair.SellTicker.UpdateTime})\n" +
                                                $"Спред: {Math.Round((decimal)spread * 100, 3)}% (с учетом комиссии)");
                        pair.IsSend = true;
                    }
                }
                else
                {
                    pair.IsSend = false;
                }

                await _context.SaveChangesAsync();
            }
        }
    }
}
