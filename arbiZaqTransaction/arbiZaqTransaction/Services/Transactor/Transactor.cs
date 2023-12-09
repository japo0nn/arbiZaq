using arbiZaqTransaction.Context;
using arbiZaqTransaction.Data;
using arbiZaqTransaction.Services.TickerChecker;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Bybit.Net.Clients;
using Bybit.Net.Objects.Models;
using Bybit.Net.Objects.Models.V5;
using CryptoExchange.Net;
using CryptoExchange.Net.Authentication;
using Huobi.Net.Clients;
using Kucoin.Net.Clients;
using Kucoin.Net.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json.Linq;
using OKX.Net.Clients;
using OKX.Net.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace arbiZaqTransaction.Services.Transactor
{
    public class Transactor : ITransactor
    {
        private readonly ArbiZaqDbContext _dbContext;
        private readonly IChecker _checker;

        public Transactor(ArbiZaqDbContext dbContext, IChecker checker)
        {
            _dbContext = dbContext;
            _checker = checker;
        }
        public async Task StartProcess()
        {
            var user = await _dbContext.Users
                .Include(x => x.UserExchangers).ThenInclude(x => x.Exchanger)
                .FirstOrDefaultAsync(x => x.Id == Guid.Parse("320d6cbb-0a57-471b-9a71-9d6b645efe52"));

            var userEx = user.UserExchangers.Select(x => x.ExchangerId).ToList();

            /*var walletId = await GetWalletWithBalance(user);

            if (walletId == Guid.Empty) return;*/

            if (user != null)
            {
                var pair = await _dbContext.Pairs
                    .Include(x => x.BuyTicker).ThenInclude(x => x.Exchanger)
                    .Include(x => x.SellTicker).ThenInclude(x => x.Exchanger)
                    .Include(x => x.BuyNetwork)
                    .Include(x => x.SellNetwork)
                    /*.Include(x => x.Withdraw)
                    .Include(x => x.Deposit)*/
                    .OrderByDescending(x => x.Spread)
                    .FirstOrDefaultAsync(x => x.IsValid && userEx.Contains(x.SellTicker.ExchangerId) && x.Spread >= 0
                    && x.Id == Guid.Parse("5a4758c9-d4d1-4c66-8726-96435281370c"));

                if (pair != null)
                {
                    /*if (!await _checker.CheckPair(user, pair.Id)) return;*/

                    /*if (walletId != pair.BuyTicker.ExchangerId)
                    {
                        var withdrawUSDT = await WithdrawAsync(user, walletId, pair, false);
                        if (!withdrawUSDT) return;
                    }*/

                    var buyTicker = pair.BuyTicker;
                    var sellTicker = pair.SellTicker;

                    /*var PlaceOrderToBuy = await PlaceOrder(buyTicker, user, true);

                    if (!PlaceOrderToBuy) return;*/

                    var WithdrawCurrency = await WithdrawAsync(user, buyTicker.ExchangerId, pair, true);

                    if (!WithdrawCurrency) return;

                    var PlaceOrderToSell = await PlaceOrder(sellTicker, user, false);

                    if (!PlaceOrderToSell) return;
                }
            }
        }

        public async Task<bool> PlaceOrder(Ticker ticker, User user, bool ToBuy)
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

                        var side = ToBuy ? OrderSide.Buy : OrderSide.Sell;
                        var asset = ToBuy ? "USDT" : ticker.Symbol.Replace("USDT", "");
                        var balance = await GetBalance(user, ticker.ExchangerId, asset);

                        if (balance == decimal.Zero) return false;

                        var price = ToBuy ? ticker.BuyPrice : ticker.SellPrice;

                        var quantity = ToBuy ? (balance / price) : balance;

                        for (int i = 15; i >= 0; i--)
                        {
                            var placeOrder = await restClient.SpotApi.Trading.PlaceOrderAsync(ticker.Symbol.ToLower(), side, SpotOrderType.Limit, Math.Round(quantity, i), price: price, timeInForce: TimeInForce.GoodTillCanceled);
                            if (!placeOrder.Success || placeOrder.Data == null) continue;

                            while (true)
                            {
                                var order = await restClient.SpotApi.Trading.GetOrderAsync(ticker.Symbol, placeOrder.Data.Id);
                                if (order.Success && order.Data != null)
                                {
                                    if (order.Data.Status == OrderStatus.Filled)
                                    {
                                        return true;
                                    }
                                    else if (order.Data.Status != OrderStatus.New && order.Data.Status != OrderStatus.PartiallyFilled)
                                    {
                                        return false;
                                    }
                                }
                                await Task.Delay(100);
                            }
                        }

                        return false;
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

                        var side = ToBuy ? Kucoin.Net.Enums.OrderSide.Buy : Kucoin.Net.Enums.OrderSide.Sell;
                        var asset = ToBuy ? "USDT" : ticker.Symbol.Replace("USDT", "");

                        var accounts = await restClient.SpotApi.Account.GetAccountsAsync(asset, Kucoin.Net.Enums.AccountType.Main);

                        if (accounts.Success && accounts.Data != null)
                        {
                            foreach (var account in accounts.Data.ToList())
                            {
                                var transferToTradeAccount = await restClient.SpotApi.Account.InnerTransferAsync(asset, Kucoin.Net.Enums.AccountType.Main, Kucoin.Net.Enums.AccountType.Trade, account.Available);
                            }
                        }

                        var price = ToBuy ? ticker.BuyPrice : ticker.SellPrice;

                        var balance = await GetBalance(user, ticker.ExchangerId, asset);

                        if (balance == decimal.Zero) return false;

                        var quantity = ToBuy ? (balance / price) : balance;

                        for (int i = 15; i >= 0; i--)
                        {
                            var placeOrder = await restClient.SpotApi.Trading.PlaceOrderAsync(ticker.Symbol.Replace("USDT", "-USDT"), side, Kucoin.Net.Enums.NewOrderType.Limit, Math.Round(quantity, i), price, timeInForce: Kucoin.Net.Enums.TimeInForce.GoodTillCanceled);

                            if (!placeOrder.Success || placeOrder.Data == null) continue;

                            while (true)
                            {
                                var order = await restClient.SpotApi.Trading.GetOrderAsync(placeOrder.Data.Id);

                                if (order.Success && order.Data != null)
                                {
                                    if (order.Data.IsActive != null)
                                    {
                                        if (!(bool)order.Data.IsActive)
                                        {
                                            return true;
                                        }
                                    }
                                }
                                await Task.Delay(100);
                            }
                        }
                        return false;
                    }
                case "Huobi":
                    {
                        var ApiKey = user.UserExchangers.FirstOrDefault(x => x.Exchanger.Name == ticker.Exchanger.Name).ApiKey;
                        var ApiSecret = user.UserExchangers.FirstOrDefault(x => x.Exchanger.Name == ticker.Exchanger.Name).ApiSecret;

                        var restClient = new HuobiRestClient(options =>
                        {
                            options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
                        });

                        var side = ToBuy ? Huobi.Net.Enums.OrderSide.Buy : Huobi.Net.Enums.OrderSide.Sell;
                        var asset = ToBuy ? "USDT" : ticker.Symbol.Replace("USDT", "");
                        var balance = await GetBalance(user, ticker.ExchangerId, asset);

                        var price = ToBuy ? ticker.BuyPrice : ticker.SellPrice;

                        if (balance == decimal.Zero) return false;

                        var accounts = await restClient.SpotApi.Account.GetAccountsAsync();
                        long accountId = 1;

                        var quantity = ToBuy ? (balance / price) : balance;

                        if (accounts.Success && accounts.Data != null)
                        {
                            foreach (var account in accounts.Data.ToList())
                            {
                                if (account.Type == Huobi.Net.Enums.AccountType.Spot)
                                {
                                    accountId = account.Id;
                                    break;
                                }
                            }
                        }

                        for (int i = 15; i >= 0; i--)
                        {
                            var placeOrder = await restClient.SpotApi.Trading.PlaceOrderAsync(accountId, ticker.Symbol.ToLower(), side, Huobi.Net.Enums.OrderType.Limit, Math.Round(quantity, i), price);
                            if (!placeOrder.Success && placeOrder.Data == 0) continue;

                            while (true)
                            {
                                var order = await restClient.SpotApi.Trading.GetOrderAsync(placeOrder.Data);

                                if (order.Success && order.Data != null)
                                {
                                    if (order.Data.State == Huobi.Net.Enums.OrderState.Filled)
                                    {
                                        return true;
                                    }
                                    else if (order.Data.State == Huobi.Net.Enums.OrderState.Rejected || order.Data.State == Huobi.Net.Enums.OrderState.Rejected || order.Data.State == Huobi.Net.Enums.OrderState.PartiallyCanceled)
                                    {
                                        return false;
                                    }
                                }
                                await Task.Delay(100);
                            }
                        }
                        return false;
                    }
                case "OKX":
                    {
                        var ApiKey = user.UserExchangers.FirstOrDefault(x => x.Exchanger.Name == ticker.Exchanger.Name).ApiKey;
                        var ApiSecret = user.UserExchangers.FirstOrDefault(x => x.Exchanger.Name == ticker.Exchanger.Name).ApiSecret;
                        var PassCode = user.UserExchangers.FirstOrDefault(x => x.Exchanger.Name == ticker.Exchanger.Name).PassCode;

                        var restClient = new OKXRestClient(options =>
                        {
                            options.ApiCredentials = new OKXApiCredentials(ApiKey, ApiSecret, PassCode);
                        });

                        var side = ToBuy ? OKX.Net.Enums.OKXOrderSide.Buy : OKX.Net.Enums.OKXOrderSide.Sell;
                        var asset = ToBuy ? "USDT" : ticker.Symbol.Replace("USDT", "");

                        var accounts = await restClient.UnifiedApi.Account.GetFundingBalanceAsync(asset);

                        if (accounts.Success && accounts.Data != null)
                        {
                            foreach (var item in accounts.Data.ToList())
                            {
                                if (item.Asset.ToUpper() == asset.ToUpper())
                                {
                                    var transferToTrade = await restClient.UnifiedApi.Account.TransferAsync(asset, item.Available, OKX.Net.Enums.OKXTransferType.TransferWithinAccount,OKX.Net.Enums.OKXAccount.Funding, OKX.Net.Enums.OKXAccount.Trading);
                                }
                            }
                        }

                        var price = ToBuy ? ticker.BuyPrice : ticker.SellPrice;

                        var balance = await GetBalance(user, ticker.ExchangerId, asset);

                        if (balance == decimal.Zero) return false;

                        var quantity = ToBuy ? (balance / price) : balance;

                        for (int i = 15; i >= 0; i--)
                        {
                            var placeOrder = await restClient.UnifiedApi.Trading.PlaceOrderAsync(ticker.Symbol.Replace("USDT", "-USDT"), side, OKX.Net.Enums.OKXOrderType.LimitOrder, quantity, price);
                            if (placeOrder.Success && placeOrder.Data != null)
                            {
                                while (true)
                                {
                                    var order = await restClient.UnifiedApi.Trading.GetOrderDetailsAsync(ticker.Symbol.Replace("USDT", "-USDT"), placeOrder.Data.OrderId);

                                    if (order.Success && order.Data != null)
                                    {
                                        if (order.Data.OrderState == OKX.Net.Enums.OKXOrderState.Filled) return true;

                                        if (order.Data.OrderState == OKX.Net.Enums.OKXOrderState.Canceled) return false;
                                    }
                                    await Task.Delay(100);
                                }
                            }
                        }
                        return false;
                    }
                case "ByBit":
                    {
                        var ApiKey = user.UserExchangers.FirstOrDefault(x => x.Exchanger.Name == ticker.Exchanger.Name).ApiKey;
                        var ApiSecret = user.UserExchangers.FirstOrDefault(x => x.Exchanger.Name == ticker.Exchanger.Name).ApiSecret;

                        var restClient = new BybitRestClient(options =>
                        {
                            options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
                        });

                        var side = ToBuy ? Bybit.Net.Enums.OrderSide.Buy : Bybit.Net.Enums.OrderSide.Sell;
                        var asset = ToBuy ? "USDT" : ticker.Symbol.Replace("USDT", "");
                        var balance = await GetBalance(user, ticker.ExchangerId, asset);

                        if (balance == decimal.Zero) return false;

                        var price = ToBuy ? ticker.BuyPrice : ticker.SellPrice;

                        var quantity = ToBuy ? (balance / price) : balance;

                        for (int i = 15; i >= 0; i--)
                        {
                            var placeOrder = await restClient.V5Api.Trading.PlaceOrderAsync(Bybit.Net.Enums.Category.Spot, ticker.Symbol, side, Bybit.Net.Enums.NewOrderType.Limit, quantity, price, timeInForce: Bybit.Net.Enums.TimeInForce.GoodTillCanceled);
                            
                            if (!placeOrder.Success || placeOrder.Data == null) continue;

                            while (true)
                            {
                                var orders = await restClient.V5Api.Trading.GetOrdersAsync(Bybit.Net.Enums.Category.Spot, orderId: placeOrder.Data.OrderId);
                                if (orders.Success && orders.Data != null)
                                {
                                    foreach (var order in orders.Data.List)
                                    {
                                        if (order.Status == Bybit.Net.Enums.V5.OrderStatus.Filled) return true;
                                        if (order.Status == Bybit.Net.Enums.V5.OrderStatus.Cancelled
                                            || order.Status == Bybit.Net.Enums.V5.OrderStatus.Deactivated
                                            || order.Status == Bybit.Net.Enums.V5.OrderStatus.Cancelled
                                            || order.Status == Bybit.Net.Enums.V5.OrderStatus.PartiallyFilledCanceled
                                            || order.Status == Bybit.Net.Enums.V5.OrderStatus.Rejected) return false;

                                        break;
                                    }
                                }
                                await Task.Delay(100);
                            }
                        }

                        return false;
                    }
                case "MEXC":
                    {
                        var ApiKey = user.UserExchangers.FirstOrDefault(x => x.Exchanger.Name == ticker.Exchanger.Name).ApiKey;
                        var ApiSecret = user.UserExchangers.FirstOrDefault(x => x.Exchanger.Name == ticker.Exchanger.Name).ApiSecret;

                        var side = ToBuy ? "BUY" : "SELL";
                        var asset = ToBuy ? "USDT" : ticker.Symbol.Replace("USDT", "");
                        var balance = await GetBalance(user, ticker.ExchangerId, asset);

                        if (balance == decimal.Zero) return false;

                        var price = ToBuy ? ticker.BuyPrice : ticker.SellPrice;

                        var quantity = ToBuy ? (balance / price) : balance;

                        var baseAddress = "https://api.mexc.com";
                        var endpoint = "/api/v3/order";

                        for (int i = 15; i >= 0; i--)
                        {
                            var recvWindow = "5000";
                            var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                            var parameters = new Dictionary<string, object>
                            {
                                { "recvWindow", recvWindow },
                                { "timestamp", timeStamp },
                                { "symbol", ticker.Symbol },
                                { "side", side },
                                { "quantity", (Math.Round(quantity - (quantity % (decimal)Math.Pow(0.1, i)))).ToString() },
                                { "price", price.ToString() },
                                { "type", "LIMIT" }
                            };

                            var signature = GenerateSignature(parameters, ApiSecret);
                            var queryString = GenerateQueryString(parameters);

                            using var client = new HttpClient();
                            HttpRequestMessage request = new(HttpMethod.Post, $"{baseAddress}{endpoint}?{queryString}&signature={signature}");

                            request.Headers.Add("X-MEXC-APIKEY", ApiKey);
                            var response = await client.SendAsync(request);

                            if (!response.IsSuccessStatusCode) return false;

                            var result = await response.Content.ReadAsStringAsync();
                            JToken placeOrder = JToken.Parse(result);
                            var orderId = (string)placeOrder["orderId"];

                            while (true)
                            {
                                timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                                parameters = new Dictionary<string, object>
                                {
                                    { "recvWindow", recvWindow },
                                    { "timestamp", timeStamp },
                                    { "symbol", ticker.Symbol },
                                    { "orderId", orderId }
                                };

                                signature = GenerateSignature(parameters, ApiSecret);
                                queryString = GenerateQueryString(parameters);

                                request = new(HttpMethod.Get, $"{baseAddress}{endpoint}?{queryString}&signature={signature}");
                                response = await client.SendAsync(request);

                                if (response.IsSuccessStatusCode)
                                {
                                    result = await response.Content.ReadAsStringAsync();
                                    JToken order = JToken.Parse(result);

                                    if ((string)order["status"] == "FILLED") return true;
                                    if ((string)order["status"] == "CANCELED" || (string)order["status"] == "PARTIALLY_CANCELED") return false;
                                }
                                await Task.Delay(100);
                            }
                        }
                        break;
                    }

            }
            return false;
        }

        public async Task<Guid> GetWalletWithBalance(User user)
        {
            foreach (var exchanger in user.UserExchangers)
            {
                switch (exchanger.Exchanger.Name)
                {
                    case "Binance":
                        {
                            var ApiKey = exchanger.ApiKey;
                            var ApiSecret = exchanger.ApiSecret;

                            var restClient = new BinanceRestClient(options =>
                            {
                                options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
                            });

                            var accounts = await restClient.SpotApi.Account.GetBalancesAsync("USDT");

                            if (accounts.Success && accounts.Data != null)
                            {
                                foreach (var account in accounts.Data.ToList())
                                {
                                    if (account.Available >= 100)
                                    {
                                        return exchanger.Exchanger.Id;
                                    }
                                }
                            }

                            break;
                        }
                    case "Kucoin":
                        {
                            var ApiKey = exchanger.ApiKey;
                            var ApiSecret = exchanger.ApiSecret;
                            var PassCode = exchanger.PassCode;

                            var restClient = new KucoinRestClient(options =>
                            {
                                options.ApiCredentials = new KucoinApiCredentials(ApiKey, ApiSecret, PassCode);
                            });

                            var accounts = await restClient.SpotApi.Account.GetAccountsAsync("USDT");

                            if (accounts.Success && accounts.Data != null)
                            {
                                foreach (var account in accounts.Data.ToList())
                                {
                                    if (account.Available >= 100)
                                    {
                                        return exchanger.Exchanger.Id;
                                    }
                                }
                            }

                            break;
                        }
                    case "Huobi":
                        {
                            var ApiKey = exchanger.ApiKey;
                            var ApiSecret = exchanger.ApiSecret;

                            var restClient = new HuobiRestClient(options =>
                            {
                                options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
                            });

                            var accounts = await restClient.SpotApi.Account.GetAccountsAsync();
                            if (accounts.Success && accounts.Data != null)
                            {
                                foreach (var account in accounts.Data.ToList())
                                {
                                    if (account.Type == Huobi.Net.Enums.AccountType.Spot)
                                    {
                                        var balances = await restClient.SpotApi.Account.GetBalancesAsync(account.Id);
                                        if (balances.Success && balances.Data != null)
                                        {
                                            foreach (var balance in balances.Data.ToList())
                                            {
                                                if (balance.Asset.ToUpper() == "USDT" && balance.Balance >= 100)
                                                {
                                                    return exchanger.ExchangerId;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            break;
                        }
                    case "OKX":
                        {
                            var ApiKey = exchanger.ApiKey;
                            var ApiSecret = exchanger.ApiSecret;
                            var PassCode = exchanger.PassCode;

                            var restClient = new OKXRestClient(options =>
                            {
                                options.ApiCredentials = new OKXApiCredentials(ApiKey, ApiSecret, PassCode);
                            });

                            var accounts = await restClient.UnifiedApi.Account.GetAccountBalanceAsync("USDT");

                            if (accounts.Success && accounts.Data != null)
                            {
                                foreach (var item in accounts.Data.Details)
                                {
                                    if (item.Asset.ToUpper() == "USDT" && item.AvailableBalance >= 100)
                                    {
                                        return exchanger.ExchangerId;
                                    }
                                }
                            }

                            break;
                        }
                    case "ByBit":
                        {
                            var ApiKey = exchanger.ApiKey;
                            var ApiSecret = exchanger.ApiSecret;

                            var restClient = new BybitRestClient(options =>
                            {
                                options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
                            });

                            var accounts = await restClient.V5Api.Account.GetAssetBalanceAsync(Bybit.Net.Enums.AccountType.Spot, "USDT");

                            if (accounts.Success && accounts.Data != null)
                            {
                                if (accounts.Data.Balances.WalletBalance != null && accounts.Data.Balances.WalletBalance >= 100) return exchanger.ExchangerId;
                            }

                            break;
                        }
                    case "MEXC":
                        {
                            var ApiKey = exchanger.ApiKey;
                            var ApiSecret = exchanger.ApiSecret;

                            var recvWindow = "5000";
                            var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                            var parameters = new Dictionary<string, object>
                            {
                                { "recvWindow", recvWindow },
                                { "timestamp", timeStamp }
                            };

                            var baseAddress = "https://api.mexc.com";
                            var endpoint = "/api/v3/account";

                            var signature = GenerateSignature(parameters, ApiSecret);
                            var queryString = GenerateQueryString(parameters);

                            using var client = new HttpClient();
                            HttpRequestMessage request = new(HttpMethod.Get, $"{baseAddress}{endpoint}?{queryString}&signature={signature}");

                            request.Headers.Add("X-MEXC-APIKEY", ApiKey);
                            var response = await client.SendAsync(request);

                            if (response.IsSuccessStatusCode)
                            {

                                var result = await response.Content.ReadAsStringAsync();
                                JToken account = JToken.Parse(result);
                                JArray balances = JArray.Parse(account["balances"].ToString());
                                foreach (var balance in balances)
                                {
                                    if (balance["asset"].ToString().ToUpper() == "USDT" && (decimal)balance["free"] >= 100) return exchanger.ExchangerId;
                                }
                            }

                            break;
                        }
                }
            }
            return Guid.Empty;
        }

        public async Task<bool> WithdrawAsync(User user, Guid exchangerId, Pair pair, bool To)
        {
            var exchanger = user.UserExchangers.FirstOrDefault(x => x.ExchangerId == exchangerId);

            var buyTicker = pair.BuyTicker;
            var sellTicker = pair.SellTicker;

            switch (exchanger.Exchanger.Name)
            {
                case "Binance":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;

                        var restClient = new BinanceRestClient(options =>
                        {
                            options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
                        });

                        var withdrawNetwork = pair.BuyNetwork;
                        var depositNetwork = pair.SellNetwork;
                        var asset = To ? pair.BuyTicker.Symbol.Replace("USDT", "") : "USDT";

                        if (!To)
                        {
                            var networks = await GetUSDTNetworks(exchangerId, withdrawNetwork.ExchangerId);

                            if (networks == null) return false;

                            withdrawNetwork = networks["from"];
                            depositNetwork = networks["to"];
                        }

                        var depositAddress = await GetDepositAddress(depositNetwork.ExchangerId, user, depositNetwork);

                        if (depositAddress["address"] == string.Empty) return false;

                        var balance = await GetBalance(user, withdrawNetwork.ExchangerId, withdrawNetwork.Coin);
                        var startTime = DateTime.UtcNow;

                        var withdraw = await restClient.SpotApi.Account.WithdrawAsync(withdrawNetwork.Coin, depositAddress["address"], balance, network: withdrawNetwork.ChainId, addressTag: depositAddress["tag"] != "" ? depositAddress["tag"] : null);

                        var withdrawId = withdraw.Data.Id;
                        if (withdraw.Success && withdraw.Data != null)
                        {
                            bool isSent = false;
                            while (!isSent)
                            {
                                var withdrawals = await restClient.SpotApi.Account.GetWithdrawalHistoryAsync(asset, startTime: startTime);

                                if (withdrawals.Success && withdrawals.Data != null)
                                {
                                    foreach (var item in withdrawals.Data.ToList())
                                    {
                                        if (withdrawId == item.Id)
                                        {
                                            if (item.Status == WithdrawalStatus.Completed) return await CheckDeposit(user, depositNetwork.ExchangerId, asset, startTime);
                                            if (item.Status == WithdrawalStatus.Rejected || item.Status == WithdrawalStatus.Failure || item.Status == WithdrawalStatus.Canceled) return false;
                                        }
                                    }
                                }
                                await Task.Delay(100);
                            }
                        }

                        break;
                    }
                case "Kucoin":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;
                        var PassCode = exchanger.PassCode;

                        var restClient = new KucoinRestClient(options =>
                        {
                            options.ApiCredentials = new KucoinApiCredentials(ApiKey, ApiSecret, PassCode);
                        });

                        var withdrawNetwork = pair.BuyNetwork;
                        var depositNetwork = pair.SellNetwork;

                        if (!To)
                        {
                            var networks = await GetUSDTNetworks(exchangerId, withdrawNetwork.ExchangerId);

                            if (networks == null) return false;

                            withdrawNetwork = networks["from"];
                            depositNetwork = networks["to"];
                        }

                        var asset = To ? pair.BuyTicker.Symbol.Replace("USDT", "") : "USDT";
                        var depositAddress = await GetDepositAddress(depositNetwork.ExchangerId, user, depositNetwork);
                        if (depositAddress["address"] == string.Empty) return false;

                        var balance = await GetBalance(user, withdrawNetwork.ExchangerId, withdrawNetwork.Coin);
                        var startTime = DateTime.UtcNow;

                        var withdraw = await restClient.SpotApi.Account.WithdrawAsync(asset, depositAddress["address"], balance, chain: withdrawNetwork.ChainId, memo: depositAddress["tag"] != "" ? depositAddress["tag"] : null);

                        var withdrawId = withdraw.Data.WithdrawalId;
                        if (withdraw.Success && withdraw.Data != null)
                        {
                            bool isSent = false;
                            while (!isSent)
                            {
                                var withdrawals = await restClient.SpotApi.Account.GetWithdrawalsAsync(withdrawId);

                                if (withdrawals.Success && withdrawals.Data != null)
                                {
                                    foreach (var item in withdrawals.Data.Items)
                                    {
                                        if (withdrawId == item.Id)
                                        {
                                            if (item.Status == Kucoin.Net.Enums.WithdrawalStatus.Success) return await CheckDeposit(user, depositNetwork.ExchangerId, asset, startTime);
                                            if (item.Status == Kucoin.Net.Enums.WithdrawalStatus.Failure) return false;
                                        }
                                    }
                                }
                                await Task.Delay(100);
                            }
                        }

                        break;
                    }
                case "Huobi":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;
                        var PassCode = exchanger.PassCode;

                        var restClient = new HuobiRestClient(options =>
                        {
                            options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
                        });

                        var withdrawNetwork = pair.BuyNetwork;
                        var depositNetwork = pair.SellNetwork;

                        if (!To)
                        {
                            var networks = await GetUSDTNetworks(exchangerId, withdrawNetwork.ExchangerId);

                            if (networks == null) return false;

                            withdrawNetwork = networks["from"];
                            depositNetwork = networks["to"];
                        }

                        var asset = To ? pair.BuyTicker.Symbol.Replace("USDT", "") : "USDT";
                        var depositAddress = await GetDepositAddress(depositNetwork.ExchangerId, user, depositNetwork);
                        if (depositAddress["address"] == string.Empty) return false;
                        var balance = await GetBalance(user, withdrawNetwork.ExchangerId, withdrawNetwork.Coin);
                        var startTime = DateTime.UtcNow;

                        for (int i = 15; i >= 0; i--)
                        {
                            var withdraw = await restClient.SpotApi.Account.WithdrawAsync(depositAddress["address"], asset.ToLower(), Math.Round(balance - (balance % (decimal)Math.Pow(0.1, i)) - (decimal)withdrawNetwork.Fee, i), (decimal)withdrawNetwork.Fee, withdrawNetwork.ChainId, depositAddress["tag"] != "" ? depositAddress["tag"] : null);

                            if (withdraw.Success && withdraw.Data != null)
                            {
                                bool isSent = false;
                                while (!isSent)
                                {
                                    var withdrawals = await restClient.SpotApi.Account.GetWithdrawDepositAsync(Huobi.Net.Enums.WithdrawDepositType.Withdraw, asset.ToLower(), (int)withdraw.Data);

                                    if (withdrawals.Success && withdrawals.Data != null)
                                    {
                                        foreach (var item in withdrawals.Data.ToList())
                                        {
                                            if (item.Id == withdraw.Data)
                                            {
                                                if (item.State == Huobi.Net.Enums.WithdrawDepositState.WalletTransfer) return await CheckDeposit(user, depositNetwork.ExchangerId, asset, startTime);

                                                if (item.State == Huobi.Net.Enums.WithdrawDepositState.Failed
                                                    || item.State == Huobi.Net.Enums.WithdrawDepositState.Canceled
                                                    || item.State == Huobi.Net.Enums.WithdrawDepositState.Reject
                                                    || item.State == Huobi.Net.Enums.WithdrawDepositState.WalletReject
                                                    || item.State == Huobi.Net.Enums.WithdrawDepositState.ConfirmError
                                                    || item.State == Huobi.Net.Enums.WithdrawDepositState.Repealed) return false;

                                                break;
                                            }
                                        }
                                    }
                                    await Task.Delay(100);
                                }
                            }

                            await Task.Delay(100);
                        }

                        break;
                    }
                case "OKX":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;
                        var PassCode = exchanger.PassCode;

                        var restClient = new OKXRestClient(options =>
                        {
                            options.ApiCredentials = new OKXApiCredentials(ApiKey, ApiSecret, PassCode);
                        });

                        var withdrawNetwork = pair.BuyNetwork;
                        var depositNetwork = pair.SellNetwork;

                        if (!To)
                        {
                            var networks = await GetUSDTNetworks(exchangerId, withdrawNetwork.ExchangerId);

                            if (networks == null) return false;

                            withdrawNetwork = networks["from"];
                            depositNetwork = networks["to"];
                        }

                        var asset = To ? pair.BuyTicker.Symbol.Replace("USDT", "") : "USDT";
                        var depositAddress = await GetDepositAddress(depositNetwork.ExchangerId, user, depositNetwork);
                        if (depositAddress["address"] == string.Empty) return false;
                        var balance = await GetBalance(user, withdrawNetwork.ExchangerId, withdrawNetwork.Coin);
                        var startTime = DateTime.UtcNow;

                        var address = depositAddress["tag"] != "" ? depositAddress["address"] + ":" + depositAddress["tag"] : depositAddress["address"];

                        var withdraw = await restClient.UnifiedApi.Account.WithdrawAsync(asset, balance, OKX.Net.Enums.OKXWithdrawalDestination.DigitalCurrencyAddress, address, (decimal)withdrawNetwork.Fee, withdrawNetwork.ChainId);

                        if (withdraw.Success && withdraw.Data != null)
                        {
                            bool isSent = false;
                            while (!isSent)
                            {
                                var withdrawals = await restClient.UnifiedApi.Account.GetWithdrawalHistoryAsync(withdrawalId: withdraw.Data.WithdrawalId);
                                if (withdrawals.Success && withdrawals.Data != null)
                                {
                                    foreach (var item in withdrawals.Data.ToList())
                                    {
                                        if (item.WithdrawalId.ToString() == withdraw.Data.WithdrawalId)
                                        {
                                            if (item.State == OKX.Net.Enums.OKXWithdrawalState.Success) return await CheckDeposit(user, depositNetwork.ExchangerId, asset, startTime);

                                            if (item.State == OKX.Net.Enums.OKXWithdrawalState.Failed
                                                || item.State == OKX.Net.Enums.OKXWithdrawalState.Canceled
                                                || item.State == OKX.Net.Enums.OKXWithdrawalState.Canceling) return false;

                                            break;
                                        }
                                    }
                                }
                                await Task.Delay(100);
                            }
                        }

                        break;
                    }
                case "ByBit":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;

                        var restClient = new BybitRestClient(options =>
                        {
                            options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
                        });

                        var withdrawNetwork = pair.BuyNetwork;
                        var depositNetwork = pair.SellNetwork;

                        if (!To)
                        {
                            var networks = await GetUSDTNetworks(exchangerId, withdrawNetwork.ExchangerId);

                            if (networks == null) return false;

                            withdrawNetwork = networks["from"];
                            depositNetwork = networks["to"];
                        }

                        var asset = To ? pair.BuyTicker.Symbol.Replace("USDT", "") : "USDT";

                        var depositAddress = await GetDepositAddress(depositNetwork.ExchangerId, user, depositNetwork);
                        if (depositAddress["address"] == string.Empty) return false;
                        var balance = await GetBalance(user, withdrawNetwork.ExchangerId, withdrawNetwork.Coin);
                        var startTime = DateTime.UtcNow;

                        // Bot must send Message to User, then User have to add Deposit Address to Address Book on Bybit

                        var withdraw = await restClient.V5Api.Account.WithdrawAsync(asset, withdrawNetwork.ChainId, depositAddress["address"], balance, depositAddress["tag"] != "" ? depositAddress["tag"] : null);

                        if (withdraw.Success && withdraw.Data != null)
                        {
                            while (true)
                            {
                                var withdrawals = await restClient.V5Api.Account.GetWithdrawalsAsync(withdraw.Data.Id);
                                if (withdrawals.Success && withdrawals.Data != null)
                                {
                                    foreach(var item in withdrawals.Data.List)
                                    {
                                        if (item.Id == withdraw.Data.Id)
                                        {
                                            if (item.Status == Bybit.Net.Enums.V5.WithdrawalStatus.Success) return await CheckDeposit(user, depositNetwork.ExchangerId, asset, startTime);
                                            if (item.Status == Bybit.Net.Enums.V5.WithdrawalStatus.Failed
                                                || item.Status == Bybit.Net.Enums.V5.WithdrawalStatus.Rejected
                                                || item.Status == Bybit.Net.Enums.V5.WithdrawalStatus.CanceledByUser) return false;
                                            break;
                                        }
                                    }
                                }
                                await Task.Delay(100);
                            }
                        }

                        break;
                    }
                case "MEXC":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;

                        var withdrawNetwork = pair.BuyNetwork;
                        var depositNetwork = pair.SellNetwork;

                        if (!To)
                        {
                            var networks = await GetUSDTNetworks(exchangerId, withdrawNetwork.ExchangerId);

                            if (networks == null) return false;

                            withdrawNetwork = networks["from"];
                            depositNetwork = networks["to"];
                        }

                        var asset = To ? pair.BuyTicker.Symbol.Replace("USDT", "") : "USDT";

                        var depositAddress = await GetDepositAddress(depositNetwork.ExchangerId, user, depositNetwork);
                        if (depositAddress["address"] == string.Empty) return false;
                        var balance = await GetBalance(user, withdrawNetwork.ExchangerId, withdrawNetwork.Coin);
                        var startTime = DateTime.UtcNow;

                        var recvWindow = "5000";
                        var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                        var parameters = new Dictionary<string, object>
                        {
                            { "recvWindow", recvWindow },
                            { "timestamp", timeStamp },
                            { "coin",  asset},
                            { "network", withdrawNetwork.ChainId },
                            { "address", depositAddress },
                            { "amount", balance.ToString() }
                        };

                        if (depositAddress["tag"] != "")
                        {
                            parameters.Add("memo", depositAddress["tag"]);
                        }

                        var baseAddress = "https://api.mexc.com";
                        var endpoint = "/api/v3/capital/withdraw/apply";

                        var signature = GenerateSignature(parameters, ApiSecret);
                        var queryString = GenerateQueryString(parameters);

                        using var client = new HttpClient();
                        HttpRequestMessage request = new(HttpMethod.Post, $"{baseAddress}{endpoint}?{queryString}&signature={signature}");

                        request.Headers.Add("X-MEXC-APIKEY", ApiKey);
                        var response = await client.SendAsync(request);

                        if (!response.IsSuccessStatusCode) return false;

                        var result = await response.Content.ReadAsStringAsync();
                        JArray withdraw = JArray.Parse(result);

                        foreach(var item in withdraw)
                        {
                            var withdrawId = (string)item["id"];
                            while (true)
                            {
                                timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                                parameters = new Dictionary<string, object>
                                {
                                    { "recvWindow", recvWindow },
                                    { "timestamp", timeStamp },
                                    { "coin",  asset},
                                    { "startTime", startTime.Ticks.ToString() },
                                };
                                endpoint = "/api/v3/capital/withdraw/history";
                                signature = GenerateSignature(parameters, ApiSecret);
                                queryString = GenerateQueryString(parameters);
                                request = new(HttpMethod.Post, $"{baseAddress}{endpoint}?{queryString}&signature={signature}");
                                var responseWithdrawals = await client.SendAsync(request);

                                if (responseWithdrawals.IsSuccessStatusCode)
                                {
                                    var resultWithdrawals = await responseWithdrawals.Content.ReadAsStringAsync();
                                    JArray withdrawals = JArray.Parse(resultWithdrawals);
                                    foreach (var withdrawal in withdrawals)
                                    {
                                        if ((string)withdrawal["id"] == withdrawId)
                                        {
                                            if ((int)withdrawal["status"] == 7) return await CheckDeposit(user, depositNetwork.ExchangerId, asset, startTime);
                                            if ((int)withdrawal["status"] == 8) return false;
                                        }
                                    }
                                }
                                await Task.Delay(100);
                            }
                        }
                        break;
                    }

            }

            return false;
        }

        public async Task<Dictionary<string, string>> GetDepositAddress(Guid exchangerId, User user, Network network)
        {
            var exchanger = await _dbContext.UserExchangers.FirstOrDefaultAsync(x => x.UserId == user.Id && x.ExchangerId == exchangerId);

            var deposit = new Dictionary<string, string>
            {
                { "address", "" },
                { "tag", "" }
            };

            switch (exchanger.Exchanger.Name)
            {
                case "Binance":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;

                        var restClient = new BinanceRestClient(options =>
                        {
                            options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
                        });

                        var depositAddress = await restClient.SpotApi.Account.GetDepositAddressAsync(network.Coin, network.ChainId);

                        if (depositAddress.Success && depositAddress.Data != null)
                        {
                            deposit["address"] = depositAddress.Data.Address;
                            deposit["tag"] = depositAddress.Data.Tag;
                            return deposit;
                        }

                        break;
                    }
                case "Kucoin":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;
                        var PassCode = exchanger.PassCode;

                        var restClient = new KucoinRestClient(options =>
                        {
                            options.ApiCredentials = new KucoinApiCredentials(ApiKey, ApiSecret, PassCode);
                        });

                        var depositAddress = await restClient.SpotApi.Account.GetDepositAddressAsync(network.Coin, network.ChainId);
                        if (!depositAddress.Success)
                        {
                            depositAddress = await restClient.SpotApi.Account.CreateDepositAddressAsync(network.Coin, network.ChainId);
                        }

                        if (depositAddress.Success && depositAddress.Data != null)
                        {
                            deposit["address"] = depositAddress.Data.Address;
                            deposit["tag"] = depositAddress.Data.Memo;
                            return deposit;
                        }

                        break;
                    }
                case "Huobi":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;

                        var restClient = new HuobiRestClient(options =>
                        {
                            options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
                        });

                        var depositAddress = await restClient.SpotApi.Account.GetDepositAddressesAsync(network.Coin.ToLower());

                        if (depositAddress.Success && depositAddress.Data != null)
                        {
                            foreach (var item in depositAddress.Data.ToList())
                            {
                                if (item.Network == network.ChainId)
                                {
                                    deposit["address"] = item.Address;
                                    deposit["tag"] = item.AddressTag;
                                    return deposit;
                                }
                            }
                        }

                        break;
                    }
                case "OKX":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;
                        var PassCode = exchanger.PassCode;

                        var restClient = new OKXRestClient(options =>
                        {
                            options.ApiCredentials = new OKXApiCredentials(ApiKey, ApiSecret, PassCode);
                        });

                        var depositAddresses = await restClient.UnifiedApi.Account.GetDepositAddressAsync(network.Coin);

                        if (depositAddresses.Success && depositAddresses.Data != null)
                        {
                            foreach (var item in depositAddresses.Data.ToList())
                            {
                                if (item.Network == network.ChainId)
                                {
                                    deposit["address"] = item.Address;
                                    deposit["tag"] = item.Memo != null ? item.Memo : "";
                                    return deposit;
                                }
                            }
                        }

                        break;
                    }
                case "ByBit":
                    {
                        string ApiKey = "QtWP6agVXbga9G4lRy";
                        string ApiSecret = "8DBE9uGgb3FPh0JLv3Cu22IQfCxFZmDMPbju";

                        var restClient = new BybitRestClient(options =>
                        {
                            options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
                        });

                        var depositAddress = await restClient.V5Api.Account.GetDepositAddressAsync(network.Coin, network.ShortName);

                        if (depositAddress.Success && depositAddress.Data != null)
                        {
                            foreach (var item in depositAddress.Data.Networks)
                            {
                                if (item.NetworkType == network.ChainId)
                                {
                                    deposit["address"] = item.DepositAddress;
                                    deposit["tag"] = item.DepositTag;
                                    return deposit;
                                }
                            }
                        }

                        break;
                    }
                case "MEXC":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;

                        var recvWindow = "5000";
                        var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                        var parameters = new Dictionary<string, object>
                        {
                            { "coin",  network.Coin },
                            { "network", network.ChainId },
                            { "recvWindow", recvWindow },
                            { "timestamp", timeStamp },
                        };

                        var baseAddress = "https://api.mexc.com";
                        var endpoint = "/api/v3/capital/deposit/address";

                        var signature = GenerateSignature(parameters, ApiSecret);
                        var queryString = GenerateQueryString(parameters);

                        using var client = new HttpClient();
                        HttpRequestMessage request = new(HttpMethod.Get, $"{baseAddress}{endpoint}?{queryString}&signature={signature}");

                        request.Headers.Add("X-MEXC-APIKEY", ApiKey);
                        var response = await client.SendAsync(request);

                        string result = "";

                        if (!response.IsSuccessStatusCode || response.Content.ReadAsStringAsync().Result == "")
                        {
                            timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                            parameters = new Dictionary<string, object>
                            {
                                { "coin",  network.Coin },
                                { "network", network.ChainId },
                                { "recvWindow", recvWindow },
                                { "timestamp", timeStamp },
                            };

                            signature = GenerateSignature(parameters, ApiSecret);
                            queryString = GenerateQueryString(parameters);
                            HttpRequestMessage getRequest = new(HttpMethod.Post, $"{baseAddress}{endpoint}?{queryString}&signature={signature}");
                            getRequest.Headers.Add("X-MEXC-APIKEY", ApiKey);

                            var getResponse = await client.SendAsync(getRequest);
                            result = await getResponse.Content.ReadAsStringAsync();
                        }
                        else
                        {
                            result = await response.Content.ReadAsStringAsync();
                        }
                        JArray addresses = JArray.Parse(result);

                        foreach (var address in addresses)
                        {
                            if ((string)address["coin"] == network.Coin && (string)address["network"] == network.ChainId)
                            {
                                deposit["address"] = (string)address["address"];
                                deposit["tag"] = address["memo"] != null ? (string)address["memo"] : (address["tag"] != null ? (string)address["tag"] : "");
                                return deposit;
                            }
                        }

                        break;
                    }
            }

            return deposit;
        }

        public async Task<decimal> GetBalance(User user, Guid exchangerId, string Asset)
        {
            var exchanger = user.UserExchangers.FirstOrDefault(x => x.ExchangerId == exchangerId);

            switch (exchanger.Exchanger.Name)
            {
                case "Binance":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;

                        var restClient = new BinanceRestClient(options =>
                        {
                            options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
                        });

                        var accounts = await restClient.SpotApi.Account.GetBalancesAsync(Asset);

                        if (accounts.Success && accounts.Data != null)
                        {
                            foreach (var account in accounts.Data.ToList())
                            {
                                return account.Available;
                            }
                        }
                        break;
                    }
                case "Kucoin":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;
                        var PassCode = exchanger.PassCode;

                        var restClient = new KucoinRestClient(options =>
                        {
                            options.ApiCredentials = new KucoinApiCredentials(ApiKey, ApiSecret, PassCode);
                        });

                        var accounts = await restClient.SpotApi.Account.GetAccountsAsync(Asset, Kucoin.Net.Enums.AccountType.Trade);

                        if (accounts.Success && accounts.Data != null)
                        {
                            foreach (var account in accounts.Data.ToList())
                            {
                                return account.Available;
                            }
                        }
                        break;
                    }
                case "Huobi":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;

                        var restClient = new HuobiRestClient(options =>
                        {
                            options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
                        });

                        var accounts = await restClient.SpotApi.Account.GetAccountsAsync();
                        if (accounts.Success && accounts.Data != null)
                        {
                            foreach (var account in accounts.Data.ToList())
                            {
                                if (account.Type == Huobi.Net.Enums.AccountType.Spot)
                                {
                                    var balances = await restClient.SpotApi.Account.GetBalancesAsync(account.Id);
                                    if (balances.Success && balances.Data != null)
                                    {
                                        foreach (var balance in balances.Data.ToList())
                                        {
                                            if (balance.Asset.ToUpper() == Asset.ToUpper())
                                            {
                                                return balance.Balance;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    }
                case "OKX":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;
                        var PassCode = exchanger.PassCode;

                        var restClient = new OKXRestClient(options =>
                        {
                            options.ApiCredentials = new OKXApiCredentials(ApiKey, ApiSecret, PassCode);
                        });

                        var accounts = await restClient.UnifiedApi.Account.GetAccountBalanceAsync(Asset);

                        if (accounts.Success && accounts.Data != null)
                        {
                            foreach(var item in accounts.Data.Details)
                            {
                                if (item.Asset.ToUpper() == Asset.ToUpper())
                                {
                                    return (decimal)item.AvailableBalance;
                                }
                            }
                        }

                        break;
                    }
                case "ByBit":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;

                        var restClient = new BybitRestClient(options =>
                        {
                            options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
                        });

                        var accounts = await restClient.V5Api.Account.GetAssetBalanceAsync(Bybit.Net.Enums.AccountType.Spot, Asset);

                        if (accounts.Success && accounts.Data != null)
                        {
                            if (accounts.Data.Balances.WalletBalance != null) return (decimal)accounts.Data.Balances.WalletBalance;
                        }

                        break;
                    }
                case "MEXC":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;

                        var recvWindow = "5000";
                        var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                        var parameters = new Dictionary<string, object>
                        {
                            { "recvWindow", recvWindow },
                            { "timestamp", timeStamp }
                        };

                        var baseAddress = "https://api.mexc.com";
                        var endpoint = "/api/v3/account";

                        var signature = GenerateSignature(parameters, ApiSecret);
                        var queryString = GenerateQueryString(parameters);

                        using var client = new HttpClient();
                        HttpRequestMessage request = new(HttpMethod.Get, $"{baseAddress}{endpoint}?{queryString}&signature={signature}");

                        request.Headers.Add("X-MEXC-APIKEY", ApiKey);
                        var response = await client.SendAsync(request);

                        if (!response.IsSuccessStatusCode) return decimal.Zero;

                        var result = await response.Content.ReadAsStringAsync();
                        JToken account = JToken.Parse(result);
                        JArray balances = JArray.Parse(account["balances"].ToString());

                        foreach (var balance in balances)
                        {
                            if (balance["asset"].ToString().ToUpper() == Asset.ToUpper()) return (decimal)balance["free"]; 
                        }

                        break;
                    }
            }

            return decimal.Zero;
        }

        public async Task<bool> CheckDeposit(User user, Guid exchangerId, string Asset, DateTime startTime)
        {
            var exchanger = user.UserExchangers.FirstOrDefault(x => x.ExchangerId == exchangerId);
            switch (exchanger.Exchanger.Name)
            {
                case "Binance":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;

                        var restClient = new BinanceRestClient(options =>
                        {
                            options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
                        });

                        while (true)
                        {

                            var deposits = await restClient.SpotApi.Account.GetDepositHistoryAsync(Asset, startTime: startTime);

                            if (deposits.Success && deposits.Data != null)
                            {
                                foreach (var deposit in deposits.Data.ToList())
                                {
                                    if (deposit.Asset.ToUpper() == Asset.ToUpper())
                                    {
                                        if (deposit.Status == DepositStatus.Completed) return true;
                                    }
                                }
                            }
                            await Task.Delay(100);
                        }
                    }
                case "Kucoin":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;
                        var PassCode = exchanger.PassCode;

                        var restClient = new KucoinRestClient(options =>
                        {
                            options.ApiCredentials = new KucoinApiCredentials(ApiKey, ApiSecret, PassCode);
                        });

                        while (true)
                        {
                            var deposits = await restClient.SpotApi.Account.GetDepositsAsync(Asset, startTime);

                            if (deposits.Success && deposits.Data != null)
                            {
                                foreach (var deposit in deposits.Data.Items)
                                {
                                    if (deposit.Asset.ToUpper() == Asset.ToUpper())
                                    {
                                        if (deposit.Status == Kucoin.Net.Enums.DepositStatus.Success) return true;
                                        if (deposit.Status == Kucoin.Net.Enums.DepositStatus.Failure) return false;
                                    }
                                }
                            }
                            await Task.Delay(100);
                        }
                    }
                case "Huobi":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;

                        var restClient = new HuobiRestClient(options =>
                        {
                            options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
                        });

                        while (true)
                        {
                            var deposits = await restClient.SpotApi.Account.GetWithdrawDepositAsync(Huobi.Net.Enums.WithdrawDepositType.Deposit, Asset.ToLower(), direction: Huobi.Net.Enums.FilterDirection.Next);
                            if (deposits.Success && deposits.Data != null)
                            {
                                foreach (var deposit in deposits.Data.ToList())
                                {
                                    if (deposit.Asset.ToUpper() == Asset.ToUpper())
                                    {
                                        if (deposit.State == Huobi.Net.Enums.WithdrawDepositState.Safe) return true;
                                    }
                                }
                            }
                            await Task.Delay(100);
                        }
                    }
                case "OKX":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;
                        var PassCode = exchanger.PassCode;

                        var restClient = new OKXRestClient(options =>
                        {
                            options.ApiCredentials = new OKXApiCredentials(ApiKey, ApiSecret, PassCode);
                        });

                        while (true)
                        {
                            var deposits = await restClient.UnifiedApi.Account.GetDepositHistoryAsync(Asset, startTime: startTime, type: OKX.Net.Enums.OKXDepositType.NetworkDeposit);

                            if (deposits.Success && deposits.Data != null)
                            {
                                foreach (var item in deposits.Data.ToList())
                                {
                                    if (item.Asset.ToUpper() == Asset.ToUpper())
                                    {
                                        if (item.State == OKX.Net.Enums.OKXDepositState.Successful) return true;
                                    }
                                }
                            }
                            await Task.Delay(100);
                        }
                    }
                case "ByBit":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;

                        var restClient = new BybitRestClient(options =>
                        {
                            options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
                        });
                        while (true)
                        {
                            var deposits = await restClient.V5Api.Account.GetDepositsAsync(Asset, startTime);

                            if (deposits.Success && deposits.Data != null)
                            {
                                foreach (var deposit in deposits.Data.Deposits)
                                {
                                    if (deposit.Asset.ToUpper() == Asset.ToUpper())
                                    {
                                        if (deposit.Status == Bybit.Net.Enums.DepositStatus.Success) return true;
                                        if (deposit.Status == Bybit.Net.Enums.DepositStatus.DepositFailed) return false;
                                    }
                                }
                            }
                            await Task.Delay(100);
                        }
                    }
                case "MEXC":
                    {
                        var ApiKey = exchanger.ApiKey;
                        var ApiSecret = exchanger.ApiSecret;

                        while (true)
                        {

                            var recvWindow = "5000";
                            var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                            var parameters = new Dictionary<string, object>
                            {
                                { "recvWindow", recvWindow },
                                { "timestamp", timeStamp },
                                { "coin", Asset },
                                { "startTime",  startTime.Ticks.ToString() }
                            };

                            var baseAddress = "https://api.mexc.com";
                            var endpoint = "/api/v3/capital/deposit/hisrec";

                            var signature = GenerateSignature(parameters, ApiSecret);
                            var queryString = GenerateQueryString(parameters);

                            using var client = new HttpClient();
                            HttpRequestMessage request = new(HttpMethod.Get, $"{baseAddress}{endpoint}?{queryString}&signature={signature}");

                            request.Headers.Add("X-MEXC-APIKEY", ApiKey);
                            var response = await client.SendAsync(request);

                            if (response.IsSuccessStatusCode)
                            {
                                var result = await response.Content.ReadAsStringAsync();
                                JArray deposits = JArray.Parse(result);

                                foreach (var deposit in deposits)
                                {
                                    if (deposit["coin"].ToString().ToUpper() == Asset.ToUpper())
                                    {
                                        if ((int)deposit["status"] == 5) return true;
                                        if ((int)deposit["status"] == 7) return false;
                                    }
                                }
                            }

                            await Task.Delay(100);
                        }

                        break;
                    }
            }

            return false;
        }

        public async Task<Dictionary<string, Network>> GetUSDTNetworks(Guid From, Guid To)
        {
            var trcFromNetwork = await _dbContext.Networks
                .Where(x => x.ExchangerId == From
                    && x.Coin == "USDT" && x.WithdrawEnable && (x.ShortName == "TRC20" || x.Name == "TRC20"))
                .FirstOrDefaultAsync();

            if (trcFromNetwork != null)
            {
                var trcToNetwork = await _dbContext.Networks
                    .Where(x => x.ExchangerId == To
                        && x.Coin == "USDT" && x.DepositEnable && (x.ShortName == "TRC20" || x.Name == "TRC20"))
                    .FirstOrDefaultAsync();

                if (trcToNetwork != null)
                {
                    var networks = new Dictionary<string, Network>
                    {
                        { "from", trcFromNetwork },
                        { "to", trcToNetwork }
                    };

                    return networks;
                }
            }

            var fromNetworks = await _dbContext.Networks
                .OrderBy(x => x.Fee)
                .Where(x => x.ExchangerId == From
                    && x.Coin == "USDT" && x.Fee != null && x.Fee != 0 && x.WithdrawEnable)
                .ToListAsync();

            foreach (var network in fromNetworks)
            {
                var toNetwork = await _dbContext.Networks.SingleOrDefaultAsync(x =>
                                    (x.Name.ToUpper() == network.Name.ToUpper()
                                    || network.ShortName.ToUpper().Replace(" ", "") == x.ShortName.ToUpper().Replace(" ", "")
                                    || network.Name.ToUpper() == x.ShortName.ToUpper().Replace(" ", "")
                                    || network.ShortName.ToUpper().Replace(" ", "") == x.Name.ToUpper()

                                    || network.Name.ToUpper().Contains(x.Name.ToUpper())
                                    || network.Name.ToUpper().Contains(x.ShortName.ToUpper().Replace(" ", ""))
                                    || x.Name.ToUpper().Contains(network.Name.ToUpper())
                                    || x.Name.ToUpper().Contains(network.ShortName.ToUpper().Replace(" ", ""))

                                    || network.ShortName.ToUpper().Replace(" ", "").Contains(x.Name.ToUpper())
                                    || network.ShortName.ToUpper().Replace(" ", "").Contains(x.ShortName.ToUpper().Replace(" ", ""))
                                    || x.ShortName.ToUpper().Replace(" ", "").Contains(network.Name.ToUpper())
                                    || x.ShortName.ToUpper().Replace(" ", "").Contains(network.ShortName.ToUpper().Replace(" ", "")))
                                    && x.ExchangerId == To
                                    && x.Coin == "USDT" && x.DepositEnable == true);
                if (toNetwork != null)
                {
                    var networks = new Dictionary<string, Network>
                    {
                        { "from", network },
                        { "to", toNetwork }
                    };

                    return networks;
                }
            }

            return null;
        }



        private static string GenerateSignature(Dictionary<string, object> parameters, string apiSecret)
        {
            string queryString = GenerateQueryString(parameters);
            string rawData = queryString;

            return ComputeSignature(rawData, apiSecret);
        }

        private static string ComputeSignature(string data, string apiSecret)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(apiSecret);
            using (HMACSHA256 hmacsha256 = new HMACSHA256(keyBytes))
            {
                byte[] sourceBytes = Encoding.UTF8.GetBytes(data);

                byte[] hash = hmacsha256.ComputeHash(sourceBytes);

                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        private static string GenerateQueryString(Dictionary<string, object> parameters)
        {
            return string.Join("&", parameters.Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value?.ToString())).Select(kvp => string.Format("{0}={1}", kvp.Key, Uri.EscapeDataString(kvp.Value.ToString()))));
        }

    }
}
