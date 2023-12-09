using arbiZaqTransaction.Context;
using arbiZaqTransaction.Data;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Bybit.Net.Clients;
using CryptoExchange.Net.Authentication;
using Huobi.Net.Clients;
using Kucoin.Net.Clients;
using Kucoin.Net.Objects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using OKX.Net.Clients;
using OKX.Net.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace arbiZaqTransaction.Services.TickerChecker
{
    public class Checker : IChecker
    {
        private readonly ArbiZaqDbContext _dbContext;

        public Checker(ArbiZaqDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<bool> CheckPair(User user, Guid pairId)
        {
            var pair = await _dbContext.Pairs
                .Include(x => x.BuyNetwork)
                /*.Include(x => x.Withdraw)*/
                .Include(x => x.BuyTicker)
                .Include(x => x.SellTicker)
                .SingleOrDefaultAsync(x => x.Id == pairId);

            var buyPrice = await GetTicker(user, pair.BuyTicker);
            var sellPrice = await GetTicker(user, pair.SellTicker);

            if (buyPrice == decimal.Zero && sellPrice == decimal.Zero) return false;

            var sellPair = 100 / buyPrice * sellPrice;
            var spread = (sellPair / (100 + (pair.BuyNetwork.Fee / buyPrice) /*+ pair.Withdraw.Fee*/)) - 1;

            if (spread == null) return false;

            if ((spread > pair.Spread) || (spread < pair.Spread && spread > (decimal)0.01))
            {
                pair.Spread = (decimal)spread;
                pair.BuyTicker.BuyPrice = buyPrice;
                pair.BuyTicker.UpdateTime = DateTime.UtcNow;
                pair.SellTicker.SellPrice = sellPrice;
                pair.SellTicker.UpdateTime = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                return true;
            }
            else if (spread < (decimal)0.01)
            {
                pair.Spread = (decimal)spread;
                pair.BuyTicker.BuyPrice = buyPrice;
                pair.BuyTicker.UpdateTime = DateTime.UtcNow;
                pair.SellTicker.SellPrice = sellPrice;
                pair.SellTicker.UpdateTime = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }

            return false;
        }

        public async Task<decimal> GetTicker(User user, Ticker ticker)
        {
            switch (ticker.Exchanger.Name)
            {
                case "Binance":
                    {
                        var ApiKey = user.UserExchangers.FirstOrDefault(x => x.Exchanger.Name == ticker.Exchanger.Name).ApiKey;
                        var ApiSecret = user.UserExchangers.FirstOrDefault(x => x.Exchanger.Name == ticker.Exchanger.Name).ApiSecret;

                        var restClient = new BinanceRestClient(options =>
                        {
                            options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
                        });

                        var response = await restClient.SpotApi.ExchangeData.GetTickerAsync(ticker.Symbol);
                        return response.Data.LastPrice;
                    }
                case "Kucoin":
                    {
                        var ApiKey = user.UserExchangers.FirstOrDefault(x => x.Exchanger.Name == ticker.Exchanger.Name).ApiKey;
                        var ApiSecret = user.UserExchangers.FirstOrDefault(x => x.Exchanger.Name == ticker.Exchanger.Name).ApiSecret;
                        var PassCode = user.UserExchangers.FirstOrDefault(x => x.Exchanger.Name == ticker.Exchanger.Name).PassCode; 

                        var restClient = new KucoinRestClient(options =>
                        {
                            options.ApiCredentials = new KucoinApiCredentials(ApiKey, ApiSecret, PassCode);
                        });

                        var response = await restClient.SpotApi.ExchangeData.GetTickerAsync(ticker.Symbol.Replace("USDT", "-USDT"));
                        if (response.Data.LastPrice == null) return decimal.Zero;
                        return (decimal)response.Data.LastPrice;
                    }
            }

            return decimal.Zero;
        }


        public async Task FindUSDTNetworks(Guid From, Guid To)
        {

        }
    }
}
