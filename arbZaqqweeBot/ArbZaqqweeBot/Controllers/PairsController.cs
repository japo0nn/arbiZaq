using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArbZaqqweeBot.Context;
using ArbZaqqweeBot.Data;
using ArbZaqqweeBot.Dto;
using AutoMapper;
using Binance.Net.Clients;
using CryptoExchange.Net.Authentication;
using Binance.Net;
using Bybit.Net.Clients;
using Kucoin.Net.Clients;
using Kucoin.Net.Objects;
using CryptoExchange.Net.CommonObjects;
using Telegram.Bot.Types;
using System.Formats.Asn1;
using NuGet.ContentModel;
using System.Security.Policy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

namespace ArbZaqqweeBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PairsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private const string ApiKeyBinance = "O0cWWglWefO7gIpVaUjt7xuTlrlTMUDgeoFnat5dwxtZsTnYC0GpOWSN4bKium9X";
        private const string ApiSecretBinance = "5FkHRuhhaOH4VvtMyzAYJArDu7Up250fZgtdigGpNwF85FsUy7p2S3b5NlkBQfWw";
        private const string ApiKeyByBit = "dcdH4KGKB4TCjykqJf";
        private const string ApiSecretByBit = "sHF92RoPEcpzxqbyD9LToufBxopOUZhFxFlY";

        public PairsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Pairs
        [HttpGet("getPairs")]
        public async Task<List<PairDto>> GetPairs()
        {
          if (_context.Pairs == null)
          {
                return new List<PairDto>();
          }
          var pairs = await _context.Pairs
                .OrderByDescending(x => x.Spread)
                .Include(x => x.BuyTicker).ThenInclude(x => x.Exchanger)
                .Include(x => x.SellTicker).ThenInclude(x => x.Exchanger)
                .Take(10)
                .ToListAsync();

            return Mapper.Map<List<Pair>, List<PairDto>>(pairs);
        }

        [HttpPut("changeValid")]
        public async Task ChangeValid([FromBody] List<Guid> pairsId)
        {
            var pairs = await _context.Pairs.Where(x => pairsId.Contains(x.Id)).ToListAsync();

            foreach (var pair in pairs)
            {
                pair.IsValid = false;
            }

            await _context.SaveChangesAsync();
        }

        [HttpGet("placeOrder")]
        public async Task<bool> PlaceOrder()
        {

            var restClient = new BybitRestClient(options =>
            {
                options.ApiCredentials = new ApiCredentials(ApiKeyByBit, ApiSecretByBit);
                options.Environment = Bybit.Net.BybitEnvironment.Testnet;
            });

            var side = Bybit.Net.Enums.OrderSide.Buy;
            var symbol = "BTCUSDT";
            decimal balance = 100;
            if (balance == decimal.Zero) return false;
            var quantity = Math.Round(balance / 33357, 3);

            var placeOrder = await restClient.V5Api.Trading.PlaceOrderAsync(Bybit.Net.Enums.Category.Spot, symbol, side, Bybit.Net.Enums.NewOrderType.Limit, quantity, (decimal)33234.25, timeInForce: Bybit.Net.Enums.TimeInForce.GoodTillCanceled);

            if (!placeOrder.Success || placeOrder.Data == null) return false;

            while (true)
            {
                var order = await restClient.V5Api.Trading.GetOrdersAsync(Bybit.Net.Enums.Category.Spot, orderId: placeOrder.Data.OrderId);

                foreach (var item in order.Data.List)
                {
                    if (item.Status == Bybit.Net.Enums.V5.OrderStatus.Filled)
                    {
                        return true;
                    }
                    else if (item.Status != Bybit.Net.Enums.V5.OrderStatus.New && item.Status != Bybit.Net.Enums.V5.OrderStatus.Created && item.Status != Bybit.Net.Enums.V5.OrderStatus.PartiallyFilled)
                    {
                        return false;
                    }
                }
                await Task.Delay(100);
            }
        }

        public static async Task<decimal> GetBalance()
        {
            var restClient = new BybitRestClient(options =>
            {
                options.ApiCredentials = new ApiCredentials(ApiKeyByBit, ApiSecretByBit);
                options.Environment = Bybit.Net.BybitEnvironment.Testnet; 
            });

            var accounts = await restClient.V5Api.Account.GetBalancesAsync(Bybit.Net.Enums.AccountType.Unified, "USDT");

            if (accounts.Success && accounts.Data != null)
            {
                foreach (var account in accounts.Data.List)
                {
                    foreach (var coin in account.Assets)
                    {
                        return coin.WalletBalance;
                    }
                }
            }
            return decimal.Zero;
        }

        [HttpGet("getAddress")]
        public async Task<string> GetAddress()
        {
            var restClient = new BinanceRestClient(options =>
            {
                options.ApiCredentials = new ApiCredentials(ApiKeyBinance, ApiSecretBinance);
            });
            var depositAddress = await restClient.SpotApi.Account.GetDepositAddressAsync("BTC", "BNB");

            if (depositAddress.Success && depositAddress.Data != null)
            {
                return depositAddress.Data.Address;
            }

            return string.Empty;
        }
    }
}
