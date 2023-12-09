using ArbZaqqweeBot.Data;
using ArbZaqqweeBot.Migrations;
using System.Security.Claims;

namespace ArbZaqqweeBot.Helpers
{
    public static class ClaimsExtensions
    {
        public static UserInfo ToUserInfo(this ClaimsPrincipal claimsPrincipal)
        {
            return new UserInfo
            {
                Username = claimsPrincipal.Identity.Name
            };
        }
    }
}
