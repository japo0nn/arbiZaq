using arbiZaqRateGetter.Data.Abstract;
using Microsoft.AspNetCore.Identity;

namespace arbiZaqRateGetter.Data
{
    public class User : Entity
    {
        public IdentityUser IdentityUser { get; set; }
        public List<UserExchanger> UserExchangers { get; set; }
    }
}
