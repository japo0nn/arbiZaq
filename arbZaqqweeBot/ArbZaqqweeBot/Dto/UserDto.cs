using ArbZaqqweeBot.Dto.Abstract;

namespace ArbZaqqweeBot.Dto
{
    public class UserDto : MainDto
    {
        public string UserName { get; set; }
        public List<UserExchangerDto> UserExchangers { get; set; }
    }
}
