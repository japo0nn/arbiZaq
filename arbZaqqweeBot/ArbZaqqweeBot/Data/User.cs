using ArbZaqqweeBot.Data.Abstract;
using Microsoft.AspNetCore.Identity;

namespace ArbZaqqweeBot.Data
{
    public class User : Entity
    {
        public IdentityUser IdentityUser { get; set; }
        public List<UserExchanger> UserExchangers { get; set; }
    }
}
