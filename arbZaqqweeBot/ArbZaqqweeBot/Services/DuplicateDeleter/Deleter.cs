using ArbZaqqweeBot.Context;
using Microsoft.EntityFrameworkCore;

namespace ArbZaqqweeBot.Services.DuplicateDeleter
{
    public class Deleter : IDeleter
    {
        public readonly AppDbContext _context;

        public Deleter(AppDbContext context)
        {
            _context = context;
        }

        public async Task DeleteDuplicates()
        {
            var pairs = await _context.Pairs.ToListAsync();

            foreach (var pair in pairs)
            {
                foreach (var duplicate in pairs)
                {
                    if (pair.BuyTickerId == duplicate.BuyTickerId && pair.SellTickerId == duplicate.SellTickerId && pair.Id != duplicate.Id)
                    {
                        _context.Pairs.Remove(duplicate);
                    }
                }
                await _context.SaveChangesAsync();
            }

        }
    }
}
