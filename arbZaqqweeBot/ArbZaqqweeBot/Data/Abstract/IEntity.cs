namespace ArbZaqqweeBot.Data.Abstract
{
    public interface IEntity
    {
        Guid Id { get; set; }
        DateTime DateCreated { get; set; }
    }
}
