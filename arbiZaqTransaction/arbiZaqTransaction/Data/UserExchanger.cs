using arbiZaqTransaction.Data.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace arbiZaqTransaction.Data
{
    public class UserExchanger : Entity
    {
        public Guid UserId { get; set; }
        public User User { get; set; }
        
        public Guid ExchangerId { get; set; }
        public Exchanger Exchanger { get; set; }

        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public string? PassCode { get; set; }
        public bool IsEnabled { get; set; }
    }
}
