using ArbZaqqweeBot.Context;
using ArbZaqqweeBot.Data;
using Bybit.Net.Clients;
using CryptoExchange.Net.Authentication;
using Kucoin.Net.Clients;
using Microsoft.EntityFrameworkCore;

namespace ArbZaqqweeBot.Services.CryptoRequest.ByBit
{
    public class BybitService : IBybitService
    {
        private readonly AppDbContext _context;
        private const string ApiKey = "OJpnRubQQusrwV04sQ";
        private const string ApiSecret = "EpU58fUJYVbiX5IhFHBOWffZuoPAOS9bXLec";

        public BybitService(AppDbContext context)
        {
            _context = context;
        }

        public async Task GetCoinDataAsync()
        {
            var bybitBase = await _context.Exchangers.SingleOrDefaultAsync(x => x.Name == "ByBit");

            if (bybitBase == null)
            {
                await AddExchangerAsync();
                return;
            }

            var bybitRestClient = new BybitRestClient(options =>
            {
                options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
            });

            var assetResponse = await bybitRestClient.V5Api.Account.GetAssetInfoAsync();
            if (assetResponse.Success)
            {
                foreach (var item in assetResponse.Data.Assets)
                {
                    foreach (var network in item.Networks)
                    {
                        var foundNetwork = await _context.Networks.SingleOrDefaultAsync(x => x.Name == network.NetworkType.ToUpper() && x.ExchangerId == bybitBase.Id
                            && x.Coin == item.Asset.ToUpper());

                        if (foundNetwork == null)
                        {
                            if (network.NetworkDeposit == null || network.NetworkWithdraw == null) continue;

                            if (!(bool)network.NetworkDeposit && !(bool)network.NetworkWithdraw) continue;

                            var newNetwork = new Network
                            {
                                Name = network.NetworkType.ToUpper(),
                                ShortName = network.Network.ToUpper(),
                                Fee = network.WithdrawFee,
                                Coin = item.Asset.ToUpper(),
                                ExchangerId = bybitBase.Id,
                                DepositEnable = (bool)network.NetworkDeposit,
                                WithdrawEnable = (bool)network.NetworkWithdraw,
                                ChainId = network.NetworkType,
                            };

                            await _context.Networks.AddAsync(newNetwork);
                        }
                        else
                        {
                            foundNetwork.Fee = network.WithdrawFee;
                            foundNetwork.DepositEnable = (bool)network.NetworkDeposit;
                            foundNetwork.WithdrawEnable = (bool)network.NetworkWithdraw;
                            foundNetwork.ChainId = network.NetworkType;
                        }
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }

        private async Task AddExchangerAsync()
        {
            if (await _context.Exchangers.SingleOrDefaultAsync(x => x.Name == "ByBit") == null)
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
}
