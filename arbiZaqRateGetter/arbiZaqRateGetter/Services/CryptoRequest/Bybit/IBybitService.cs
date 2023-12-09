using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace arbiZaqRateGetter.Services.CryptoRequest.Bybit
{
    public interface IBybitService
    {
        Task GetTickersAsync();
    }
}
