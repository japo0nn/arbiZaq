using ArbZaqqweeBot.Data;
using ArbZaqqweeBot.Dto;
using AutoMapper;

namespace ArbZaqqweeBot.Helpers
{
    public class MapperInitalizer
    {
        private static readonly object _locker = new object();
        public static void Initialize()
        {
            lock (_locker)
            {
                Mapper.Reset();
                Mapper.Initialize(cfg =>
                {
                    cfg.CreateMap<Exchanger, ExchangerDto>();
                    cfg.CreateMap<Ticker, TickerDto>();
                    cfg.CreateMap<Pair, PairDto>().ForMember(
                            dest => dest.Spread,
                            opt => opt.MapFrom(x => Math.Round(x.Spread * 100, 3)))
                        .ForMember(
                            dest => dest.Symbol,
                            opt => opt.MapFrom(x => x.BuyTicker.Symbol));
                    cfg.CreateMap<User, UserDto>().ForMember(
                        dest => dest.UserName,
                        opt => opt.MapFrom(x => x.IdentityUser.UserName));

                    cfg.CreateMap<UserExchanger, UserExchangerDto>();
                });
            }
        }
    }
}
