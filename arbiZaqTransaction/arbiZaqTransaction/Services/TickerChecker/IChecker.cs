using arbiZaqTransaction.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace arbiZaqTransaction.Services.TickerChecker
{
    public interface IChecker
    {
        Task<bool> CheckPair(User user, Guid pairId);
    }
}
