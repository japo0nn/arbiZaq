using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArbZaqqweeBot.Services.Analyzer
{
    internal interface IAnalyzerService
    {
        Task AnalyzeTickersAsync();
    }
}
