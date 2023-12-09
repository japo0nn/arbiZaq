using arbiZaqRateGetter.Context;
using arbiZaqRateGetter.Data;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.Xml;
using System.Timers;
using Bybit.Net.Clients;
using Kucoin.Net.Clients;
using Kucoin.Net.Objects;

namespace arbiZaqRateGetter.Services.CryptoRequest.Kucoin
{
    public class KucoinService : IKucoinService
    {
        private readonly ArbiZaqDbContext _context;
        private const string ApiKey = "6524c0c0efe922000182fdce";
        private const string ApiSecret = "9aa9a9c5-45b1-4174-8053-6d9ed1fb51ab";
        private const string PassCode = "Kalibri";

        public KucoinService(ArbiZaqDbContext context)
        {
            _context = context;
        }

        public async Task GetTickersAsync()
        {
            var kucoinBase = await _context.Exchangers.SingleOrDefaultAsync(x => x.Name == "Kucoin");
            if (kucoinBase != null)
            {
                var kucoinRestClient = new KucoinRestClient(options =>
                {
                    options.ApiCredentials = new KucoinApiCredentials(ApiKey, ApiSecret, PassCode);
                });

                var response = await kucoinRestClient.SpotApi.ExchangeData.GetTickersAsync();

                if (response.Success)
                {
                    foreach (var item in response.Data.Data.ToList())
                    {
                        if (item.BestAskPrice == null || item.BestBidPrice == null || !item.Symbol.ToUpper().EndsWith("USDT")) continue;

                        var foundTicker = await _context.Tickers
                            .Include(x => x.Networks)
                            .SingleOrDefaultAsync(x => x.Symbol == item.Symbol.ToUpper().Replace("-", "")
                            && x.ExchangerId == kucoinBase.Id);

                        if (foundTicker != null)
                        {
                            foundTicker.BuyPrice = (decimal)item.BestAskPrice;
                            foundTicker.SellPrice = (decimal)item.BestBidPrice;
                            foundTicker.Volume = item.Volume != null ? (decimal)item.Volume : 0;
                            foundTicker.UpdateTime = DateTime.UtcNow;
                            var tickerNetworks = await _context.Networks.Where(x => x.ExchangerId == kucoinBase.Id && item.Symbol.ToUpper().Replace("USDT", "") == x.Coin).ToListAsync();
                            foreach (var network in tickerNetworks)
                            {
                                if (!foundTicker.Networks.Any(x => x.Id == network.Id))
                                {
                                    foundTicker.Networks.Add(network);
                                }
                            }
                            _context.Tickers.Update(foundTicker);
                        }
                        else
                        {
                            var tickerNetworks = await _context.Networks.Where(x => x.ExchangerId == kucoinBase.Id && item.Symbol.ToUpper().Replace("USDT", "") == x.Coin).ToListAsync();
                            Ticker ticker = new()
                            {
                                Symbol = item.Symbol.ToUpper().Replace("-", ""),
                                BuyPrice = (decimal)item.BestAskPrice,
                                SellPrice = (decimal)item.BestBidPrice,
                                ExchangerId = kucoinBase.Id,
                                Volume = item.Volume != null ? (decimal)item.Volume : 0,
                            };
                            await _context.Tickers.AddAsync(ticker);

                            foreach (var network in tickerNetworks)
                            {
                                ticker.Networks.Add(network);
                            }
                        }
                        await _context.SaveChangesAsync();
                    }
                }
            }
            else
            {
                await AddExchangerAsync();
            }
        }

        private async Task AddExchangerAsync()
        {
            var newEx = new Exchanger
            {
                Name = "Kucoin",
            };

            await _context.Exchangers.AddAsync(newEx);
            await _context.SaveChangesAsync();
        }
    }
}
