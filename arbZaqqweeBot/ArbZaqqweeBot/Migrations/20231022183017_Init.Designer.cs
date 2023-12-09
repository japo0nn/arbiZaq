﻿// <auto-generated />
using System;
using ArbZaqqweeBot.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArbZaqqweeBot.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20231022183017_Init")]
    partial class Init
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("ArbZaqqweeBot.Data.Exchanger", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("BaseUrl")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("TickersEndpoint")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Exchangers");
                });

            modelBuilder.Entity("ArbZaqqweeBot.Data.Network", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Coin")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("DepositEnable")
                        .HasColumnType("boolean");

                    b.Property<Guid>("ExchangerId")
                        .HasColumnType("uuid");

                    b.Property<decimal?>("Fee")
                        .HasColumnType("numeric");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("WithdrawEnable")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.HasIndex("ExchangerId");

                    b.ToTable("Networks");
                });

            modelBuilder.Entity("ArbZaqqweeBot.Data.Pair", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("BuyTickerId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("SellTickerId")
                        .HasColumnType("uuid");

                    b.Property<decimal>("Spread")
                        .HasColumnType("numeric");

                    b.Property<bool>("isSend")
                        .HasColumnType("boolean");

                    b.Property<bool>("isValid")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.HasIndex("BuyTickerId");

                    b.HasIndex("SellTickerId");

                    b.ToTable("Pairs");
                });

            modelBuilder.Entity("ArbZaqqweeBot.Data.Ticker", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal>("BuyPrice")
                        .HasColumnType("numeric");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("ExchangerId")
                        .HasColumnType("uuid");

                    b.Property<decimal>("SellPrice")
                        .HasColumnType("numeric");

                    b.Property<string>("Symbol")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime?>("UpdateTime")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("ExchangerId");

                    b.ToTable("Tickers");
                });

            modelBuilder.Entity("ArbZaqqweeBot.Data.TickerNet", b =>
                {
                    b.Property<Guid?>("TickerId")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("NetworkId")
                        .HasColumnType("uuid");

                    b.HasKey("TickerId", "NetworkId");

                    b.HasIndex("NetworkId");

                    b.ToTable("TickerNets", (string)null);
                });

            modelBuilder.Entity("ArbZaqqweeBot.Data.Network", b =>
                {
                    b.HasOne("ArbZaqqweeBot.Data.Exchanger", "Exchanger")
                        .WithMany()
                        .HasForeignKey("ExchangerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Exchanger");
                });

            modelBuilder.Entity("ArbZaqqweeBot.Data.Pair", b =>
                {
                    b.HasOne("ArbZaqqweeBot.Data.Ticker", "BuyTicker")
                        .WithMany()
                        .HasForeignKey("BuyTickerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ArbZaqqweeBot.Data.Ticker", "SellTicker")
                        .WithMany()
                        .HasForeignKey("SellTickerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("BuyTicker");

                    b.Navigation("SellTicker");
                });

            modelBuilder.Entity("ArbZaqqweeBot.Data.Ticker", b =>
                {
                    b.HasOne("ArbZaqqweeBot.Data.Exchanger", "Exchanger")
                        .WithMany("TickerList")
                        .HasForeignKey("ExchangerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Exchanger");
                });

            modelBuilder.Entity("ArbZaqqweeBot.Data.TickerNet", b =>
                {
                    b.HasOne("ArbZaqqweeBot.Data.Network", "Network")
                        .WithMany("TickerNets")
                        .HasForeignKey("NetworkId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ArbZaqqweeBot.Data.Ticker", "Ticker")
                        .WithMany("TickerNets")
                        .HasForeignKey("TickerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Network");

                    b.Navigation("Ticker");
                });

            modelBuilder.Entity("ArbZaqqweeBot.Data.Exchanger", b =>
                {
                    b.Navigation("TickerList");
                });

            modelBuilder.Entity("ArbZaqqweeBot.Data.Network", b =>
                {
                    b.Navigation("TickerNets");
                });

            modelBuilder.Entity("ArbZaqqweeBot.Data.Ticker", b =>
                {
                    b.Navigation("TickerNets");
                });
#pragma warning restore 612, 618
        }
    }
}
