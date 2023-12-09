using ArbZaqqweeBot.Context;
using ArbZaqqweeBot.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using NuGet.Packaging.Signing;
using System.Security.Cryptography;
using System.Text;

namespace ArbZaqqweeBot.Services.CryptoRequest.MEXC
{
    public class MexcService : IMexcService
    {
        private readonly AppDbContext _context;
        private const string ApiKey = "mx0vglypmp3Tqr2sCp";
        private const string ApiSecret = "92170d08e4f344eb9905aa71916a5ecf";
        private const string RecvWindow = "5000";
        private static readonly string TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();


        public MexcService(AppDbContext context)
        {
            _context = context;
        }

        public async Task GetCoinDataAsync()
        {
            var mexcBase = await _context.Exchangers.SingleOrDefaultAsync(x => x.Name == "MEXC");

            if (mexcBase == null)
            {
                await AddExchangerAsync();
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                { "recvWindow", RecvWindow },
                { "timestamp", TimeStamp }
            };

            var baseAddress = "https://api.mexc.com";
            var endpoint = "/api/v3/capital/config/getall";

            var signature = GenerateGetSignature(parameters);
            var queryString = GenerateQueryString(parameters);

            using var client = new HttpClient();
            HttpRequestMessage request = new(HttpMethod.Get, $"{baseAddress}{endpoint}?{queryString}&signature={signature}");

            request.Headers.Add("X-MEXC-APIKEY", ApiKey);
            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                await Task.Delay(100);
                return;
            }

            var result = await response.Content.ReadAsStringAsync();
            JArray assets = JArray.Parse(result);
            foreach (var item in assets)
            {
                JArray networks = JArray.FromObject(item["networkList"]);
                foreach (var network in networks)
                {

                    var hasTicker = await _context.Tickers.Where(x => x.Symbol.ToUpper().Replace("USDT", "") == item["coin"].ToString().ToUpper()
                        && x.ExchangerId == mexcBase.Id).ToListAsync();

                    if (!hasTicker.Any() && item["coin"].ToString().ToUpper() != "USDT") continue;

                    var lb = network["network"].ToString().IndexOf("(");
                    var rb = network["network"].ToString().IndexOf(")");
                    var name = lb != -1 && rb != -1 ? network["network"].ToString().Substring(lb + 1, rb - lb - 1) : network["network"].ToString();

                    var foundNetwork = await _context.Networks.SingleOrDefaultAsync(x => x.Name == name.ToUpper() && x.ExchangerId == mexcBase.Id
                            && x.Coin == item["coin"].ToString().ToUpper());

                    if (foundNetwork == null)
                    {
                        if (!(bool)network["depositEnable"] && !(bool)network["withdrawEnable"]) continue;

                        var newNetwork = new Network
                        {
                            Name = name.ToUpper(),
                            ShortName = network["network"].ToString().ToUpper(),
                            Fee = (decimal)network["withdrawFee"],
                            Coin = item["coin"].ToString().ToUpper(),
                            ExchangerId = mexcBase.Id,
                            DepositEnable = (bool)network["depositEnable"],
                            WithdrawEnable = (bool)network["withdrawEnable"],
                            ChainId = network["network"].ToString(),
                        };

                        await _context.Networks.AddAsync(newNetwork);
                    }
                    else
                    {
                        foundNetwork.Fee = (decimal)network["withdrawFee"];
                        foundNetwork.DepositEnable = (bool)network["depositEnable"];
                        foundNetwork.WithdrawEnable = (bool)network["withdrawEnable"];
                        foundNetwork.ChainId = network["network"].ToString();
                    }

                    await _context.SaveChangesAsync();
                }
            }
        }

        private static string GenerateGetSignature(Dictionary<string, object> parameters)
        {
            string queryString = GenerateQueryString(parameters);
            string rawData = queryString;

            return ComputeSignature(rawData);
        }

        private static string ComputeSignature(string data)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ApiSecret));
            byte[] signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(signature).Replace("-", "").ToLower();
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
