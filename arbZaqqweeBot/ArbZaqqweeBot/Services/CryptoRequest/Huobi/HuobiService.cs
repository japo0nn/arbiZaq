using ArbZaqqweeBot.Context;
using ArbZaqqweeBot.Data;
using CryptoExchange.Net.Authentication;
using Huobi.Net.Clients;
using Huobi.Net.Enums;
using Microsoft.EntityFrameworkCore;

namespace ArbZaqqweeBot.Services.CryptoRequest.Huobi
{
    public class HuobiService : IHuobiService
    {
        private readonly AppDbContext _context;
        private const string ApiKey = "9366188c-bgrdawsdsd-8c49d3b1-e4da5";
        private const string ApiSecret = "0083e357-e6d171b6-f4828925-f40bd";

        public HuobiService(AppDbContext context)
        {
            _context = context;
        }

        public async Task GetCoinDataAsync()
        {
            var huobiBase = await _context.Exchangers.SingleOrDefaultAsync(x => x.Name == "Huobi");

            if (huobiBase == null)
            {
                await AddExchangerAsync();
                return;
            }

            var binanceRestClient = new HuobiRestClient(options =>
            {
                options.ApiCredentials = new ApiCredentials(ApiKey, ApiSecret);
            });

            var response = await binanceRestClient.SpotApi.ExchangeData.GetAssetDetailsAsync();
            if (response.Success)
            {
                foreach (var item in response.Data.ToList())
                {
                    foreach (var network in item.Networks)
                    {
                        var foundNetwork = await _context.Networks.SingleOrDefaultAsync(x => x.Name == network.DisplayName.ToUpper() && x.ExchangerId == huobiBase.Id
                            && x.Coin == item.Asset.ToUpper());

                        if (foundNetwork == null)
                        {
                            if (network.WithdrawStatus != CurrencyStatus.Allowed && network.DepositStatus != CurrencyStatus.Allowed) continue;
                            var newNetwork = new Network
                            {
                                Name = network.DisplayName != "" ? network.DisplayName.ToUpper() : item.Asset.ToUpper(),
                                ShortName = network.BaseChain.ToUpper() != "" ? network.BaseChain.ToUpper() : network.DisplayName.ToUpper(),
                                Fee = network.TransactFeeWithdraw,
                                Coin = item.Asset.ToUpper(),
                                ExchangerId = huobiBase.Id,
                                DepositEnable = network.DepositStatus == CurrencyStatus.Allowed ? true : false,
                                WithdrawEnable = network.WithdrawStatus == CurrencyStatus.Allowed ? true : false,
                                ChainId = network.Chain,
                            };

                            await _context.Networks.AddAsync(newNetwork);
                        }
                        else
                        {
                            foundNetwork.Fee = network.TransactFeeWithdraw;
                            foundNetwork.DepositEnable = network.DepositStatus == CurrencyStatus.Allowed ? true : false;
                            foundNetwork.WithdrawEnable = network.WithdrawStatus == CurrencyStatus.Allowed ? true : false;
                            foundNetwork.ChainId = network.Chain;
                        }

                        await _context.SaveChangesAsync();
                    }
                }
            }
        }

        private async Task AddExchangerAsync()
        {
            if (await _context.Exchangers.SingleOrDefaultAsync(x => x.Name == "Huobi") == null)
            {
                var newEx = new Exchanger
                {
                    Name = "Huobi",
                };

                await _context.Exchangers.AddAsync(newEx);
                await _context.SaveChangesAsync();
            }
        }
    }
}
