using ArbZaqqweeBot.Context;
using ArbZaqqweeBot.Data;
using ArbZaqqweeBot.Services.TelegramBot;
using Microsoft.EntityFrameworkCore;

namespace ArbZaqqweeBot.Services.Analyzer
{
    public class AnalyzerService : IAnalyzerService
    {
        private readonly AppDbContext _context;
        private readonly IBotActions _botActions;

        public AnalyzerService(AppDbContext context, IBotActions botActions)
        {
            _context = context;
            _botActions = botActions;
        }

        public async Task AnalyzeTickersAsync()
        {
            try
            {
                var tickerCount = await _context.Tickers.GroupBy(x => x.Symbol).CountAsync();

                var checkedTickers = new List<Guid>();

                var i = 0;
                while (i < tickerCount)
                {
                    var ticker = await _context.Tickers
                        .Include(x => x.Networks.OrderBy(x => x.Fee))
                        .Include(x => x.Exchanger)
                        .Where(x => !checkedTickers.Contains(x.Id))
                        .OrderBy(x => x.Id)
                        .Skip(i)
                        .FirstOrDefaultAsync();

                    if (ticker != null)
                    {
                        var otherTickers = await _context.Tickers
                            .Include(x => x.Networks.OrderBy(x => x.Fee))
                            .Include(x => x.Exchanger)
                            .Where(x => x.Symbol == ticker.Symbol)
                            .ToListAsync();

                        foreach (var firstTicker in otherTickers)
                        {
                            foreach (var secondTicker in otherTickers)
                            {
                                if (firstTicker.BuyPrice < secondTicker.SellPrice)
                                {
                                    await CheckPair(firstTicker, secondTicker);
                                }
                                else if (firstTicker.SellPrice > secondTicker.BuyPrice)
                                {
                                    await CheckPair(secondTicker, firstTicker);
                                }
                                checkedTickers.Add(secondTicker.Id);
                            }
                        }
                        checkedTickers.AddRange(otherTickers.Select(x => x.Id));
                    }
                    else
                    {
                        break;
                    }
                    i++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public async Task CheckPair(Ticker buyTicker, Ticker sellTicker)
        {
            var buyNetworks = buyTicker.Networks;
            var sellNetworks = sellTicker.Networks;

            foreach (var buyNet in buyNetworks)
            {
                if (!buyNet.WithdrawEnable) continue;

                foreach (var sellNet in sellNetworks)
                { 
                    if (!sellNet.DepositEnable) continue;

                    if (sellNet.Name.ToUpper() == buyNet.Name.ToUpper() 
                        || buyNet.ShortName.ToUpper().Replace(" ", "").Replace("-", "") == sellNet.ShortName.ToUpper().Replace(" ", "").Replace("-", "")
                        || buyNet.Name.ToUpper() == sellNet.ShortName.ToUpper().Replace(" ", "").Replace("-", "")
                        || buyNet.ShortName.ToUpper().Replace(" ", "").Replace("-", "") == sellNet.Name.ToUpper()

                        || buyNet.Name.ToUpper().Contains(sellNet.Name.ToUpper())
                        || buyNet.Name.ToUpper().Contains(sellNet.ShortName.ToUpper().Replace(" ", "").Replace("-", ""))
                        || sellNet.Name.ToUpper().Contains(buyNet.Name.ToUpper())
                        || sellNet.Name.ToUpper().Contains(buyNet.ShortName.ToUpper().Replace(" ", "").Replace("-", ""))

                        || buyNet.ShortName.ToUpper().Replace(" ", "").Replace("-", "").Contains(sellNet.Name.ToUpper())
                        || buyNet.ShortName.ToUpper().Replace(" ", "").Replace("-", "").Contains(sellNet.ShortName.ToUpper().Replace(" ", "").Replace("-", ""))
                        || sellNet.ShortName.ToUpper().Replace(" ", "").Replace("-", "").Contains(buyNet.Name.ToUpper())
                        || sellNet.ShortName.ToUpper().Replace(" ", "").Replace("-", "").Contains(buyNet.ShortName.ToUpper().Replace(" ", "").Replace("-", "")))
                    {
                        var buyPrice = buyTicker.BuyPrice;
                        var sellPrice = sellTicker.SellPrice;

                        var firstPair = buyNet.Coin;
                        var secondPair = buyTicker.Symbol.Replace(firstPair, "");

                        /*var withdrawPairNetworks = await _context.Networks
                                    .Where(x => x.Coin == secondPair && x.ExchangerId == sellTicker.ExchangerId
                                        && x.Fee != null && x.Fee != 0 && x.WithdrawEnable == true)
                                    .OrderBy(x => x.Fee)
                                    .ToListAsync();

                        foreach (var withdrawNetwork in withdrawPairNetworks)
                        {
                            var depositPairNetwork = await _context.Networks
                                .SingleOrDefaultAsync(x =>
                                    (x.Name.ToUpper() == withdrawNetwork.Name.ToUpper()
                                    || withdrawNetwork.ShortName.ToUpper().Replace(" ", "") == x.ShortName.ToUpper().Replace(" ", "")
                                    || withdrawNetwork.Name.ToUpper() == x.ShortName.ToUpper().Replace(" ", "")
                                    || withdrawNetwork.ShortName.ToUpper().Replace(" ", "") == x.Name.ToUpper()

                                    || withdrawNetwork.Name.ToUpper().Contains(x.Name.ToUpper())
                                    || withdrawNetwork.Name.ToUpper().Contains(x.ShortName.ToUpper().Replace(" ", ""))
                                    || x.Name.ToUpper().Contains(withdrawNetwork.Name.ToUpper())
                                    || x.Name.ToUpper().Contains(withdrawNetwork.ShortName.ToUpper().Replace(" ", ""))

                                    || withdrawNetwork.ShortName.ToUpper().Replace(" ", "").Contains(x.Name.ToUpper())
                                    || withdrawNetwork.ShortName.ToUpper().Replace(" ", "").Contains(x.ShortName.ToUpper().Replace(" ", ""))
                                    || x.ShortName.ToUpper().Replace(" ", "").Contains(withdrawNetwork.Name.ToUpper())
                                    || x.ShortName.ToUpper().Replace(" ", "").Contains(withdrawNetwork.ShortName.ToUpper().Replace(" ", "")))
                                    && x.ExchangerId == buyTicker.ExchangerId
                                    && x.Coin == secondPair && x.DepositEnable == true);

                            if (depositPairNetwork != null)
                            {*/
                                if (buyPrice > 0 && sellPrice > 0)
                                {
                                    var buyPair = 100 / buyPrice;
                                    var sellPair = buyPair * sellPrice;
                                    var spread = (sellPair / (100 + (buyNet.Fee / buyPrice) /*+ (withdrawNetwork.Fee)*/ + 1)) - 1;
                                    var pair = await _context.Pairs
                                        .Include(x => x.BuyTicker).ThenInclude(x => x.Exchanger)
                                        .Include(x => x.SellTicker).ThenInclude(x => x.Exchanger)
                                        .FirstOrDefaultAsync(x => x.BuyTickerId == buyTicker.Id &&
                                            x.SellTickerId == sellTicker.Id);

                                    if (pair != null && pair.Spread > spread) continue;

                                    if (pair == null && spread >= (decimal)0.001)
                                    {
                                        var newPair = new Pair
                                        {
                                            BuyTickerId = buyTicker.Id,
                                            SellTickerId = sellTicker.Id,
                                            Spread = (decimal)spread,
                                            IsSend = true,
                                            BuyNetworkId = buyNet.Id,
                                            SellNetworkId = sellNet.Id,
/*                                            WithdrawId = withdrawNetwork.Id,
                                            DepositId = depositPairNetwork.Id,*/
                                        };
                                        if (spread > (decimal)0.1) newPair.IsValid = false;

                                        await _context.Pairs.AddAsync(newPair);

                                        await _botActions.SendMessageAsync($"<b>{buyTicker.Exchanger.Name} -> {sellTicker.Exchanger.Name}</b>\n" +
                                                $"Пара: {buyTicker.Symbol.ToUpper()}\n" +
                                                $"Покупка: {buyPrice} ({buyTicker.UpdateTime})\n" +
                                                $"Продажа: {sellPrice} ({sellTicker.UpdateTime})\n" +
                                                $"Спред: {Math.Round((decimal)spread * 100, 3)}% (с учетом комиссии)");
                                        await _context.SaveChangesAsync();
                                        break;
                                    }

                                    if (pair != null && spread > (decimal)0.1 && !pair.IsValid) pair.IsValid = false;

                                    if (pair != null)
                                    {
                                        if (spread >= (decimal)0.001)
                                        {
                                            pair.Spread = (decimal)spread;
                                            pair.BuyNetworkId = buyNet.Id;
                                            pair.SellNetworkId = sellNet.Id;
/*                                            pair.WithdrawId = withdrawNetwork.Id;
                                            pair.DepositId = depositPairNetwork.Id;*/

                                            if (!pair.IsSend)
                                            {
                                                await _botActions.SendMessageAsync($"<b>{buyTicker.Exchanger.Name} -> {sellTicker.Exchanger.Name}</b>\n" +
                                                    $"Пара: {buyTicker.Symbol.ToUpper()}\n" +
                                                    $"Покупка: {buyPrice} ({buyTicker.UpdateTime})\n" +
                                                    $"Продажа: {sellPrice} ({sellTicker.UpdateTime})\n" +
                                                    $"Спред: {Math.Round((decimal)spread * 100, 3)}% (с учетом комиссии)");
                                            }
                                        }
                                        else if (spread < (decimal)0.001)
                                        {
                                            pair.Spread = (decimal)spread;
                                            pair.IsSend = false;
                                            pair.BuyNetworkId = buyNet.Id;
                                            pair.SellNetworkId = sellNet.Id;
/*                                            pair.WithdrawId = withdrawNetwork.Id;
                                            pair.DepositId = depositPairNetwork.Id;*/
                                        }

                                        await _context.SaveChangesAsync();
                                        break;
                                    }

                                }
                            /*}
                        }*/
                    }
                }
            }
        }
    }
}


