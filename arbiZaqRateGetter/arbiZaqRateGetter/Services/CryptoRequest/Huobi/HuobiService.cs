using arbiZaqRateGetter.Context;
using arbiZaqRateGetter.Data;
using Binance.Net.Clients;
using CryptoExchange.Net.Authentication;
using Huobi.Net.Clients;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace arbiZaqRateGetter.Services.CryptoRequest.Huobi
{
    public class HuobiService : IHuobiService
    {
        private readonly ArbiZaqDbContext _context;
        private const string ApiKey = "9366188c-bgrdawsdsd-8c49d3b1-e4da5";
        private const string ApiSecret = "0083e357-e6d171b6-f4828925-f40bd";

        public HuobiService(ArbiZaqDbContext context)
        {
            _context = context;
        }

        public async Task GetTickersAsync()
        {
            var huobiBase = await _context.Exchangers.SingleOrDefaultAsync(x => x.Name == "Huobi");
            if (huobiBase != null)
            {
                var huobiRestClient = new HuobiRestClient(options =>
                {
                    options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
                });

                var response = await huobiRestClient.SpotApi.ExchangeData.GetTickersAsync();

                if (response.Success)
                {
                    foreach (var item in response.Data.Ticks)
                    {
                        if (item.BestAskPrice == 0 || item.BestBidPrice == 0 || item.Volume == null || !item.Symbol.ToUpper().EndsWith("USDT")) continue;

                        var foundTicker = await _context.Tickers
                            .Include(x => x.Networks)
                            .SingleOrDefaultAsync(x => x.Symbol == item.Symbol.ToUpper()
                            && x.ExchangerId == huobiBase.Id);

                        if (foundTicker != null)
                        {
                            foundTicker.BuyPrice = item.BestAskPrice;
                            foundTicker.SellPrice = item.BestBidPrice;
                            foundTicker.Volume = (decimal)item.Volume;
                            foundTicker.UpdateTime = DateTime.UtcNow;

                            var tickerNetworks = await _context.Networks.Where(x => x.ExchangerId == huobiBase.Id && item.Symbol.ToUpper().Replace("USDT", "") == x.Coin).ToListAsync();
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
                            var tickerNetworks = await _context.Networks.Where(x => x.ExchangerId == huobiBase.Id && item.Symbol.ToUpper().Replace("USDT", "") == x.Coin).ToListAsync();
                            Ticker ticker = new Ticker()
                            {
                                Symbol = item.Symbol.ToUpper(),
                                BuyPrice = item.BestAskPrice,
                                SellPrice = item.BestBidPrice,
                                ExchangerId = huobiBase.Id,
                                Volume = (decimal)item.Volume,
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
                Name = "Huobi",
            };

            await _context.Exchangers.AddAsync(newEx);
            await _context.SaveChangesAsync();
        }
    }
}
