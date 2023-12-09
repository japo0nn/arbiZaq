using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ArbZaqqweeBot.Helpers
{
    public class AuthOptions
    {
        public const string ISSUER = "https://localhost:5001";
        public const string AUDIENCE = "http://localhost:4200";
        const string KEY = "sdfkldsdsksldfds";
        public const int LIFETIME = 200;
        public static SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
        }
    }
}
