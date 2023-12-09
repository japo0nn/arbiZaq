using arbiZaqRateGetter.Context;
using arbiZaqRateGetter.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using Binance.Net.Clients;
using CryptoExchange.Net.Authentication;

namespace arbiZaqRateGetter.Services.CryptoRequest.Binance
{
    public class BinanceService : IBinanceService
    {
        private readonly ArbiZaqDbContext _context;
        private const string ApiKey = "1b88VNSVCZDKegpYOz6u0NO7vmnAyZRbzxzJ1qfwKyZUsIqQIRwJh17ZW4mYtaVF";
        private const string ApiSecret = "i3I5IN7XqHxPzFMkplyYDQgiISf0mDAIdeSaQAj9Q8gKmnlwPqakmqgXx1JTL1io";

        public BinanceService(ArbiZaqDbContext context)
        {
            _context = context;
        }

        public async Task GetTickersAsync()
        {
            var binanceBase = await _context.Exchangers.SingleOrDefaultAsync(x => x.Name == "Binance");
            if (binanceBase != null)
            {
                var binanceRestClient = new BinanceRestClient(options =>
                {
                    options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
                });

                var response = await binanceRestClient.SpotApi.ExchangeData.GetTickersAsync();

                if (response.Success)
                {
                    foreach (var item in response.Data.ToList())
                    {
                        if (item.BestBidPrice == 0 || item.BestAskPrice == 0 || !item.Symbol.ToUpper().EndsWith("USDT")) continue;

                        var foundTicker = await _context.Tickers
                            .Include(x => x.Networks)
                            .SingleOrDefaultAsync(x => x.Symbol == item.Symbol.ToUpper()
                            && x.ExchangerId == binanceBase.Id);

                        if (foundTicker != null)
                        {
                            foundTicker.BuyPrice = item.BestAskPrice;
                            foundTicker.SellPrice = item.BestBidPrice;
                            foundTicker.Volume = item.Volume;
                            foundTicker.UpdateTime = DateTime.UtcNow;
                            var tickerNetworks = await _context.Networks.Where(x => x.ExchangerId == binanceBase.Id && item.Symbol.ToUpper().Replace("USDT", "") == x.Coin).ToListAsync();
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
                            var tickerNetworks = await _context.Networks.Where(x => x.ExchangerId == binanceBase.Id && item.Symbol.ToUpper().Replace("USDT", "") == x.Coin).ToListAsync();
                            Ticker ticker = new Ticker()
                            {
                                Symbol = item.Symbol.ToUpper(),
                                BuyPrice = item.BestAskPrice,
                                SellPrice = item.BestBidPrice,
                                ExchangerId = binanceBase.Id,
                                Volume = item.Volume,
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
                Name = "Binance",
            };

            await _context.Exchangers.AddAsync(newEx);
            await _context.SaveChangesAsync();
        }
    }


}
