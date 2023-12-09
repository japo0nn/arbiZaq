using ArbZaqqweeBot.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;

namespace ArbZaqqweeBot.Context
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
            /*Database.EnsureCreated();*/
        }

        public DbSet<Exchanger> Exchangers { get; set; }
        public DbSet<Ticker> Tickers { get; set; }
        public DbSet<Pair> Pairs { get; set; }
        public DbSet<Network> Networks { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserExchanger> UserExchangers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Ticker>()
                    .HasMany(c => c.Networks)
                    .WithMany(s => s.Tickers)
                    .UsingEntity<TickerNet>(
                        j => j
                            .HasOne(p => p.Network)
                            .WithMany(q => q.TickerNets)
                            .HasForeignKey(p => p.NetworkId),
                        j => j
                            .HasOne(p => p.Ticker)
                            .WithMany(q => q.TickerNets)
                            .HasForeignKey(p => p.TickerId),
                        j =>
                        {
                            j.HasKey(p => new { p.TickerId, p.NetworkId});
                            j.ToTable("TickerNets");
                        });

            base.OnModelCreating(modelBuilder);
        }
    }
}
