using arbiZaqRateGetter.Context;
using arbiZaqRateGetter.Data;
using Binance.Net.Clients;
using Bybit.Net.Clients;
using CryptoExchange.Net.Authentication;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace arbiZaqRateGetter.Services.CryptoRequest.Bybit
{
    public class BybitService : IBybitService
    {
        private readonly ArbiZaqDbContext _context;
        private const string ApiKey = "OJpnRubQQusrwV04sQ";
        private const string ApiSecret = "EpU58fUJYVbiX5IhFHBOWffZuoPAOS9bXLec";

        public BybitService(ArbiZaqDbContext context)
        {
            _context = context;
        }

        public async Task GetTickersAsync()
        {
            var bybitBase = await _context.Exchangers.SingleOrDefaultAsync(x => x.Name == "ByBit");
            if (bybitBase != null)
            {
                var bybitRestClient = new BybitRestClient(options =>
                {
                    options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
                });

                var response = await bybitRestClient.V5Api.ExchangeData.GetSpotTickersAsync();

                if (response.Success)
                {
                    foreach (var item in response.Data.List)
                    {
                        if (item.BestBidPrice == 0 || item.BestAskPrice == 0 || !item.Symbol.ToUpper().EndsWith("USDT")) continue;

                        var foundTicker = await _context.Tickers
                            .Include(x => x.Networks)
                            .SingleOrDefaultAsync(x => x.Symbol == item.Symbol.ToUpper()
                            && x.ExchangerId == bybitBase.Id);

                        if (foundTicker != null)
                        {
                            foundTicker.BuyPrice = (decimal)item.BestAskPrice;
                            foundTicker.SellPrice = (decimal)item.BestBidPrice;
                            foundTicker.Volume = item.Volume24h;
                            foundTicker.UpdateTime = DateTime.UtcNow;
                            var tickerNetworks = await _context.Networks.Where(x => x.ExchangerId == bybitBase.Id && item.Symbol.ToUpper().Replace("USDT", "") == x.Coin).ToListAsync();
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
                            var tickerNetworks = await _context.Networks.Where(x => x.ExchangerId == bybitBase.Id && item.Symbol.ToUpper().Replace("USDT", "") == x.Coin).ToListAsync();
                            Ticker ticker = new Ticker()
                            {
                                Symbol = item.Symbol.ToUpper(),
                                BuyPrice = (decimal)item.BestAskPrice,
                                SellPrice = (decimal)item.BestBidPrice,
                                ExchangerId = bybitBase.Id,
                                Volume = item.Volume24h,
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
                Name = "ByBit",
            };

            await _context.Exchangers.AddAsync(newEx);
            await _context.SaveChangesAsync();
        }
    }
}
