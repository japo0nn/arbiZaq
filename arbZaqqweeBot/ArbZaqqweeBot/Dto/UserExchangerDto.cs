using ArbZaqqweeBot.Dto.Abstract;

namespace ArbZaqqweeBot.Dto
{
    public class UserExchangerDto : MainDto
    {
        public Guid UserId { get; set; }
        public UserDto? User { get; set; }

        public Guid ExchangerId { get; set; }
        public ExchangerDto? Exchanger { get; set; }

        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public string? PassCode { get; set; }
        public bool IsEnabled { get; set; }
    }
}
