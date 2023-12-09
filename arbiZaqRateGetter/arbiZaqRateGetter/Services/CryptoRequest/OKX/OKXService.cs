using arbiZaqRateGetter.Context;
using arbiZaqRateGetter.Data;
using Microsoft.EntityFrameworkCore;
using OKX.Net.Clients;
using OKX.Net.Objects;
using OKX.Net.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace arbiZaqRateGetter.Services.CryptoRequest.OKX
{
    public class OKXService : IOKXService
    {
        private readonly ArbiZaqDbContext _context;
        private const string ApiKey = "ab2e3dd4-5178-4a28-8bea-536e21535217";
        private const string ApiSecret = "AA1CBE79BBC85A9CF854BD7B81E1782E";
        private const string PassCode = "Kalibri3663*";

        public OKXService(ArbiZaqDbContext context)
        {
            _context = context;
        }

        public async Task GetTickersAsync()
        {
            var okxBase = await _context.Exchangers.SingleOrDefaultAsync(x => x.Name == "OKX");
            if (okxBase != null)
            {
                var okxRestClient = new OKXRestClient(options =>
                {
                    options.ApiCredentials = new OKXApiCredentials(ApiKey, ApiSecret, PassCode);
                });

                var response = await okxRestClient.UnifiedApi.ExchangeData.GetTickersAsync(OKXInstrumentType.Spot);

                if (response.Success)
                {
                    foreach (var item in response.Data.ToList())
                    {
                        if (!item.Symbol.ToUpper().EndsWith("USDT")) continue;
                        if (item.BestAskPrice == null || item.BestAskPrice == null) continue;
                        var foundTicker = await _context.Tickers
                            .Include(x => x.Networks)
                            .SingleOrDefaultAsync(x => x.Symbol == item.Symbol.ToUpper().Replace("-", "")
                            && x.ExchangerId == okxBase.Id);

                        if (foundTicker != null)
                        {
                            foundTicker.SellPrice = (decimal)item.BestBidPrice;
                            foundTicker.BuyPrice = (decimal)item.BestAskPrice;
                            foundTicker.Volume = item.Volume;
                            foundTicker.UpdateTime = DateTime.UtcNow;
                            var tickerNetworks = await _context.Networks.Where(x => x.ExchangerId == okxBase.Id && item.Symbol.ToUpper().Replace("USDT", "") == x.Coin).ToListAsync();
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
                            var tickerNetworks = await _context.Networks.Where(x => x.ExchangerId == okxBase.Id && item.Symbol.ToUpper().Replace("USDT", "") == x.Coin).ToListAsync();
                            Ticker ticker = new Ticker()
                            {
                                Symbol = item.Symbol.ToUpper().Replace("-", ""),
                                BuyPrice = (decimal)item.BestAskPrice,
                                SellPrice = (decimal)item.BestBidPrice,
                                ExchangerId = okxBase.Id,
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
                Name = "OKX",
            };

            await _context.Exchangers.AddAsync(newEx);
            await _context.SaveChangesAsync();
        }
    }
}
