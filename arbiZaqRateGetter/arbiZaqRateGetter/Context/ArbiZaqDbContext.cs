using System;
using System.Collections.Generic;
using arbiZaqRateGetter.Data;
using Microsoft.EntityFrameworkCore;

namespace arbiZaqRateGetter.Context;

public partial class ArbiZaqDbContext : DbContext
{
    public ArbiZaqDbContext()
    {
    }

    public ArbiZaqDbContext(DbContextOptions<ArbiZaqDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Exchanger> Exchangers { get; set; }

    public virtual DbSet<Network> Networks { get; set; }

    public virtual DbSet<Pair> Pairs { get; set; }

    public virtual DbSet<Ticker> Tickers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserExchanger> UserExchangers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=arbiZaq_db;Username=postgres;Password=AezakMi1488*");

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
                        j.HasKey(p => new { p.TickerId, p.NetworkId });
                        j.ToTable("TickerNets");
                    });

        base.OnModelCreating(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
