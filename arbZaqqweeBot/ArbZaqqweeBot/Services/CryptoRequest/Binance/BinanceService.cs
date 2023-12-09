using ArbZaqqweeBot.Context;
using ArbZaqqweeBot.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using Binance.Net.Clients;
using CryptoExchange.Net.Authentication;

namespace ArbZaqqweeBot.Services.CryptoRequest.Binance
{
    public class BinanceService : IBinanceService
    {
        private readonly AppDbContext _context;
        private const string ApiKey = "O0cWWglWefO7gIpVaUjt7xuTlrlTMUDgeoFnat5dwxtZsTnYC0GpOWSN4bKium9X";
        private const string ApiSecret = "5FkHRuhhaOH4VvtMyzAYJArDu7Up250fZgtdigGpNwF85FsUy7p2S3b5NlkBQfWw";

        public BinanceService(AppDbContext context)
        {
            _context = context;
        }

        public async Task GetCoinDataAsync()
        {
            var binanceBase = await _context.Exchangers.SingleOrDefaultAsync(x => x.Name == "Binance");

            if (binanceBase == null) { 
                await AddExchangerAsync();
                return;
            }

            var binanceRestClient = new BinanceRestClient(options =>
            {
                options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
            });

            var response = await binanceRestClient.SpotApi.Account.GetUserAssetsAsync();

            if (response.Success)
            {
                foreach (var item in response.Data.ToList())
                {
                    foreach (var network in item.NetworkList)
                    {
                        var lb = network.Name.IndexOf("(");
                        var rb = network.Name.IndexOf(")");
                        var name = lb != -1 && rb != -1 ? network.Name.Substring(lb + 1, rb - lb - 1) : network.Name;

                        var foundNetwork = await _context.Networks.SingleOrDefaultAsync(x => x.Name == name.ToUpper() && x.ExchangerId == binanceBase.Id
                            && x.Coin == item.Asset.ToUpper());

                        if (foundNetwork == null)
                        {
                            if (!network.WithdrawEnabled && !network.DepositEnabled) continue;

                            var newNetwork = new Network
                            {
                                Name = name.ToUpper(),
                                ShortName = network.Network.ToUpper(),
                                Fee = network.WithdrawFee,
                                Coin = item.Asset.ToUpper(),
                                ExchangerId = binanceBase.Id,
                                DepositEnable = network.DepositEnabled,
                                WithdrawEnable = network.WithdrawEnabled,
                                ChainId = network.Network,
                            };

                            await _context.Networks.AddAsync(newNetwork);
                        }
                        else
                        {
                            foundNetwork.Fee = network.WithdrawFee;
                            foundNetwork.DepositEnable = network.DepositEnabled;
                            foundNetwork.WithdrawEnable = network.WithdrawEnabled;
                            foundNetwork.ChainId = network.Network;
                        }

                        await _context.SaveChangesAsync();
                    }
                }
            }
        }

        private async Task AddExchangerAsync()
        {
            if (await _context.Exchangers.SingleOrDefaultAsync(x => x.Name == "Binance") == null)
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
}
