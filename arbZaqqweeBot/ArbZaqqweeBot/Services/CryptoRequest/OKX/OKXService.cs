using ArbZaqqweeBot.Context;
using ArbZaqqweeBot.Data;
using Kucoin.Net.Clients;
using Microsoft.EntityFrameworkCore;
using OKX.Net.Clients;
using OKX.Net.Objects;

namespace ArbZaqqweeBot.Services.CryptoRequest.OKX
{
    public class OKXService : IOKXService
    {
        private readonly AppDbContext _context;
        private const string ApiKey = "ab2e3dd4-5178-4a28-8bea-536e21535217";
        private const string ApiSecret = "AA1CBE79BBC85A9CF854BD7B81E1782E";
        private const string PassCode = "Kalibri3663*";

        public OKXService(AppDbContext context)
        {
            _context = context;
        }

        public async Task GetCoinDataAsync()
        {
            var okxBase = await _context.Exchangers.SingleOrDefaultAsync(x => x.Name == "OKX");

            if (okxBase == null)
            {
                await AddExchangerAsync();
                return;
            }

            var okxRestClient = new OKXRestClient(options =>
            {
                options.ApiCredentials = new OKXApiCredentials(ApiKey, ApiSecret, PassCode);
            });

            var assetResponse = await okxRestClient.UnifiedApi.Account.GetAssetsAsync();
            if (assetResponse.Success)
            {
                foreach (var item in assetResponse.Data.ToList())
                {
                    var name = item.Network.Replace(item.Asset + "-", "");

                    var foundNetwork = await _context.Networks.SingleOrDefaultAsync(x => x.Name == name && x.ExchangerId == okxBase.Id
                        && x.Coin == item.Asset.ToUpper());

                    if (foundNetwork == null)
                    {
                        if (!item.AllowWithdrawal && !item.AllowDeposit) continue;

                        var newNetwork = new Network
                        {
                            Name = name,
                            ShortName = name,
                            Fee = item.MinimumWithdrawalFee,
                            Coin = item.Asset.ToUpper(),
                            ExchangerId = okxBase.Id,
                            DepositEnable = item.AllowDeposit,
                            WithdrawEnable = item.AllowWithdrawal,
                            ChainId = item.Network,
                        };

                        await _context.Networks.AddAsync(newNetwork);
                    }
                    else
                    {
                        foundNetwork.Fee = item.MinimumWithdrawalFee;
                        foundNetwork.DepositEnable = item.AllowDeposit;
                        foundNetwork.WithdrawEnable = item.AllowDeposit;
                        foundNetwork.ChainId = item.Network;
                    }
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task AddExchangerAsync()
        {
            if (await _context.Exchangers.SingleOrDefaultAsync(x => x.Name == "OKX") == null)
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
}
