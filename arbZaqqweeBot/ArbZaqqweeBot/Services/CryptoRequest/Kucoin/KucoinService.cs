using ArbZaqqweeBot.Context;
using ArbZaqqweeBot.Data;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.Xml;
using System.Timers;
using Bybit.Net.Clients;
using Kucoin.Net.Clients;
using Kucoin.Net.Objects;

namespace ArbZaqqweeBot.Services.CryptoRequest.Kucoin
{
    public class KucoinService : IKucoinService
    {
        private readonly AppDbContext _context;
        private const string ApiKey = "6524c0c0efe922000182fdce";
        private const string ApiSecret = "9aa9a9c5-45b1-4174-8053-6d9ed1fb51ab";
        private const string PassCode = "Kalibri";

        public KucoinService(AppDbContext context)
        {
            _context = context;
        }

        public async Task GetCoinDataAsync()
        {
            var kucoinBase = await _context.Exchangers.SingleOrDefaultAsync(x => x.Name == "Kucoin");

            if (kucoinBase == null)
            {
                await AddExchangerAsync();
                return;
            }

            var kucoinRestClient = new KucoinRestClient(options =>
            {
                options.ApiCredentials = new KucoinApiCredentials(ApiKey, ApiSecret, PassCode);
            });

            var assetResponse = await kucoinRestClient.SpotApi.ExchangeData.GetAssetsAsync();
            if (assetResponse.Success)
            {
                foreach (var item in assetResponse.Data.ToList())
                {
                    var response = await kucoinRestClient.SpotApi.ExchangeData.GetAssetAsync(item.Asset);
                    if (response.Success)
                    {
                        if (response.Data == null || response.Data.Networks == null) continue;

                        foreach (var network in response.Data.Networks)
                        {
                            var foundNetwork = await _context.Networks.SingleOrDefaultAsync(x => x.Name == network.NetworkName.ToUpper() && x.ExchangerId == kucoinBase.Id
                                && x.Coin == item.Asset.ToUpper());

                            if (foundNetwork == null)
                            {
                                if (!network.IsWithdrawEnabled && !network.IsDepositEnabled) continue;

                                var newNetwork = new Network
                                {
                                    Name = network.NetworkName.ToUpper(),
                                    ShortName = network.Network.ToUpper(),
                                    Fee = network.WithdrawalMinFee,
                                    Coin = item.Asset.ToUpper(),
                                    ExchangerId = kucoinBase.Id,
                                    DepositEnable = network.IsDepositEnabled,
                                    WithdrawEnable = network.IsWithdrawEnabled,
                                    ChainId = network.Network,
                                };

                                await _context.Networks.AddAsync(newNetwork);
                            }
                            else
                            {
                                foundNetwork.Fee = network.WithdrawalMinFee;
                                foundNetwork.DepositEnable = network.IsDepositEnabled;
                                foundNetwork.WithdrawEnable = network.IsWithdrawEnabled;
                                foundNetwork.ChainId = network.Network;
                            }
                            await _context.SaveChangesAsync();
                        }

                    }
                }
            }
        }

        private async Task AddExchangerAsync()
        {
            if (await _context.Exchangers.SingleOrDefaultAsync(x => x.Name == "Kucoin") == null)
            {
                var newEx = new Exchanger
                {
                    Name = "Kucoin",
                };

                await _context.Exchangers.AddAsync(newEx);
                await _context.SaveChangesAsync();
            }
        }
    }
}
