using arbiZaqRateGetter.Context;
using arbiZaqRateGetter.Data;
using Bybit.Net.Clients;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace arbiZaqRateGetter.Services.CryptoRequest.MEXC
{
    public class MexcService : IMexcService
    {
        private readonly ArbiZaqDbContext _context;
        private const string ApiKey = "mx0vglypmp3Tqr2sCp";
        private const string ApiSecret = "92170d08e4f344eb9905aa71916a5ecf";
        private const string RecvWindow = "5000";
        private static readonly string TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        public MexcService(ArbiZaqDbContext context)
        {
            _context = context;
        }

        public async Task GetTickersAsync()
        {
            var mexcBase = await _context.Exchangers.SingleOrDefaultAsync(x => x.Name == "MEXC");
            if (mexcBase != null)
            {
                var parameters = new Dictionary<string, object>
                    {
                        { "recvWindow", RecvWindow },
                        { "timestamp", TimeStamp }
                    };

                var baseAddress = "https://api.mexc.com";
                var endpoint = "/api/v3/ticker/24hr";

                var queryString = GenerateQueryString(parameters);

                using var client = new HttpClient();
                HttpRequestMessage request = new(HttpMethod.Get, $"{baseAddress}{endpoint}?{queryString}");

                request.Headers.Add("X-MEXC-APIKEY", ApiKey);
                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode) return;

                var result = response.Content.ReadAsStringAsync().Result;

                JArray tickers = JArray.Parse(result);

                foreach (var item in tickers)
                {
                    if (item["bidPrice"] == null || item["askPrice"] == null || !item["symbol"].ToString().ToUpper().EndsWith("USDT") || item["volume"] == null) continue;

                    var hasTickers = await _context.Tickers.Where(x => x.Symbol.ToUpper() == item["symbol"].ToString().ToUpper()).ToListAsync();

                    if (!hasTickers.Any()) continue;

                    var foundTicker = await _context.Tickers
                            .Include(x => x.Networks)
                            .SingleOrDefaultAsync(x => x.Symbol == item["symbol"].ToString().ToUpper()
                            && x.ExchangerId == mexcBase.Id);

                    if (foundTicker != null)
                    {
                        foundTicker.BuyPrice = (decimal)item["askPrice"];
                        foundTicker.SellPrice = (decimal)item["bidPrice"];
                        foundTicker.Volume = (decimal)item["volume"];
                        foundTicker.UpdateTime = DateTime.UtcNow;
                        var tickerNetworks = await _context.Networks.Where(x => x.ExchangerId == mexcBase.Id && item["symbol"].ToString().ToUpper().Replace("USDT", "") == x.Coin).ToListAsync();

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
                        var tickerNetworks = await _context.Networks.Where(x => x.ExchangerId == mexcBase.Id && item["symbol"].ToString().Replace("USDT", "") == x.Coin).ToListAsync();
                        Ticker ticker = new Ticker()
                        {
                            Symbol = item["symbol"].ToString().ToUpper(),
                            BuyPrice = (decimal)item["askPrice"],
                            SellPrice = (decimal)item["bidPrice"],
                            ExchangerId = mexcBase.Id,
                            Volume = (decimal)item["volume"],
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
            else
            {
                await AddExchangerAsync();
            }
        }

        private static string GenerateQueryString(Dictionary<string, object> parameters)
        {
            return string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));
        }

        private async Task AddExchangerAsync()
        {
            var newEx = new Exchanger
            {
                Name = "MEXC",
            };

            await _context.Exchangers.AddAsync(newEx);
            await _context.SaveChangesAsync();
        }
    }
}
