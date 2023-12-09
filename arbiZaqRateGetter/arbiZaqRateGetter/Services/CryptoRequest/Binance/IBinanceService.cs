using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace arbiZaqRateGetter.Services.CryptoRequest.Binance
{
    public interface IBinanceService
    {
        Task GetTickersAsync();
    }
}
