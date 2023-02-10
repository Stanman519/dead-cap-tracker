﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Principal;
using DeadCapTracker.Models;
using DeadCapTracker.Models.BotModels;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace DeadCapTracker.Repositories
{
    public partial class DeadCapTrackerContext : DbContext
    {
        public DeadCapTrackerContext()
        {
            
        }

        public DeadCapTrackerContext(DbContextOptions<DeadCapTrackerContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Franchise> Franchises { get; set; }
        public virtual DbSet<Transaction> Transactions { get; set; }
        public DbSet<PlayerEntity> Players { get; set; }
        public DbSet<BidEntity> Bids { get; set; }
        public DbSet<OwnerEntity> Owners { get; set; }
        public DbSet<LotEntity> Lots { get; set; }
        public DbSet<SuggestionEntity> Suggestions { get; set; }
        public DbSet<LeagueOwnerEntity> LeagueOwners { get; set; }
        public DbSet<LeagueEntity> Leagues { get; set; }
        public DbSet<ContractEntity> Contracts { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                //optionsBuilder.UseNpgsql(Configuration);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BidEntity>(entity =>
            {
                entity.HasKey(e => e.Bidid).HasName("PK_bidledger");

                entity.ToTable("bid");

                entity.Property(e => e.Bidid).HasColumnName("bidid");
                entity.Property(e => e.Bidlength).HasColumnName("bidlength");
                entity.Property(e => e.Bidsalary).HasColumnName("bidsalary");
                entity.Property(e => e.Expires)
                    .HasColumnType("datetime")
                    .HasColumnName("expires");
                entity.Property(e => e.Leagueid).HasColumnName("leagueid");
                entity.Property(e => e.Mflid).HasColumnName("mflid");
                entity.Property(e => e.Ownerid).HasColumnName("ownerid");

                entity.HasOne(d => d.League).WithMany(p => p.Bids)
                    .HasForeignKey(d => d.Leagueid)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_bid_League");

                entity.HasOne(d => d.Player).WithMany(p => p.Bids)
                    .HasForeignKey(d => d.Mflid)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_bid_player");

                entity.HasOne(d => d.LeagueOwner).WithMany(p => p.Bids)
                    .HasForeignKey(d => d.Ownerid)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_bid_leagueowner");
            });

            modelBuilder.Entity<ContractEntity>(entity =>
            {
                entity.ToTable("contract");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Bidid).HasColumnName("bidid");
                entity.Property(e => e.Contractvalue).HasColumnName("contractvalue");
                entity.Property(e => e.Leagueid).HasColumnName("leagueid");
                entity.Property(e => e.Length).HasColumnName("length");
                entity.Property(e => e.Mflid).HasColumnName("mflid");
                entity.Property(e => e.Ownerid).HasColumnName("ownerid");
                entity.Property(e => e.Salary).HasColumnName("salary");

                entity.HasOne(d => d.Bid).WithMany(p => p.Contracts)
                    .HasForeignKey(d => d.Bidid)
                    .HasConstraintName("FK_contract_bid");

                entity.HasOne(d => d.League).WithMany(p => p.Contracts)
                    .HasForeignKey(d => d.Leagueid)
                    .HasConstraintName("FK_contract_League");

                entity.HasOne(d => d.Player).WithMany(p => p.Contracts)
                    .HasForeignKey(d => d.Mflid)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_contract_player");

                entity.HasOne(d => d.Owner).WithMany(p => p.Contracts)
                    .HasForeignKey(d => d.Ownerid)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_contract_leagueowner");
            });


            modelBuilder.Entity<LeagueEntity>(entity =>
            {
                entity.HasKey(e => e.Mflid);

                entity.ToTable("League");

                entity.Property(e => e.Mflid)
                    .ValueGeneratedNever()
                    .HasColumnName("mflid");
                entity.Property(e => e.Commishcookie).HasColumnName("commishcookie");
                entity.Property(e => e.Isauctioning).HasColumnName("isauctioning");
                entity.Property(e => e.Mflhash).HasColumnName("mflhash");
                entity.Property(e => e.Name)
                    .HasMaxLength(80)
                    .HasColumnName("name");
            });

            modelBuilder.Entity<LeagueOwnerEntity>(entity =>
            {
                entity.HasKey(e => e.Leagueownerid).HasName("PK_leagueowner");
                entity.ToTable("leagueowner");

                entity.Property(e => e.Leagueownerid).HasColumnName("leagueownerid");
                entity.Property(e => e.Caproom).HasColumnName("caproom");
                entity.Property(e => e.Leagueid).HasColumnName("leagueid");
                entity.Property(e => e.Mflfranchiseid).HasColumnName("mflfranchiseid");
                entity.Property(e => e.Ownerid).HasColumnName("ownerid");
                entity.Property(e => e.Yearsleft).HasColumnName("yearsleft");

                entity.HasOne(d => d.LeagueEntity).WithMany(p => p.Leagueowners)
                    .HasForeignKey(d => d.Leagueid)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_leagueowner_League");

                entity.HasOne(d => d.OwnerEntity).WithMany(p => p.Leagueowners)
                    .HasForeignKey(d => d.Ownerid)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_leagueowner_owner");
            });

            modelBuilder.Entity<LotEntity>(entity =>
            {
                entity.ToTable("lot");

                entity.Property(e => e.Lotid)
                    .ValueGeneratedNever()
                    .HasColumnName("lotid");
                entity.Property(e => e.Bidid).HasColumnName("bidid");
                entity.Property(e => e.Leagueid).HasColumnName("leagueid");

                entity.HasOne(d => d.Bid).WithMany(p => p.Lots)
                    .HasForeignKey(d => d.Bidid)
                    .HasConstraintName("FK_bidid_fkey");

                entity.HasOne(d => d.League).WithMany(p => p.Lots)
                    .HasForeignKey(d => d.Leagueid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_lot_League");
            });

            modelBuilder.Entity<OwnerEntity>(entity =>
            {
                entity.ToTable("owner");

                entity.Property(e => e.Ownerid)
                    .ValueGeneratedNever()
                    .HasColumnName("ownerid");
                entity.Property(e => e.Displayname)
                    .HasMaxLength(50)
                    .HasColumnName("displayname");
                entity.Property(e => e.Ownername)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("ownername");
                entity.Property(e => e.PasswordHash)
                    .IsUnicode(false)
                    .HasColumnName("password_hash");
                entity.Property(e => e.Premium).HasColumnName("premium");
            });

            modelBuilder.Entity<PlayerEntity>(entity =>
            {
                entity.HasKey(e => e.Mflid);

                entity.ToTable("player");

                entity.Property(e => e.Mflid)
                    .ValueGeneratedNever()
                    .HasColumnName("mflid");
                entity.Property(e => e.Actionshot).HasColumnName("actionshot");
                entity.Property(e => e.Age).HasColumnName("age");
                entity.Property(e => e.Cbsid)
                    .HasMaxLength(50)
                    .HasColumnName("cbsid");
                entity.Property(e => e.College).HasColumnName("college");
                entity.Property(e => e.Draftpick).HasColumnName("draftpick");
                entity.Property(e => e.Draftround).HasColumnName("draftround");
                entity.Property(e => e.Draftteam)
                    .HasMaxLength(50)
                    .HasColumnName("draftteam");
                entity.Property(e => e.Draftyear).HasColumnName("draftyear");
                entity.Property(e => e.Espnid)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("espnid");
                entity.Property(e => e.Firstname)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("firstname");
                entity.Property(e => e.Fullname)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("fullname");
                entity.Property(e => e.Headshot).HasColumnName("headshot");
                entity.Property(e => e.Height).HasColumnName("height");
                entity.Property(e => e.IsActive).HasColumnName("isActive");
                entity.Property(e => e.Jersey).HasColumnName("jersey");
                entity.Property(e => e.Lastname)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("lastname");
                entity.Property(e => e.Lastseasonpts)
                    .HasColumnType("numeric(4, 1)")
                    .HasColumnName("lastseasonpts");
                entity.Property(e => e.Position)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("position");
                entity.Property(e => e.Rotowireid)
                    .HasMaxLength(50)
                    .HasColumnName("rotowireid");
                entity.Property(e => e.Rotoworldid)
                    .HasMaxLength(50)
                    .HasColumnName("rotoworldid");
                entity.Property(e => e.Team)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("team");
                entity.Property(e => e.Weight).HasColumnName("weight");
            });

            modelBuilder.Entity<SuggestionEntity>(entity =>
            {
                entity.ToTable("suggestions");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Mflid)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("mflid");
                entity.Property(e => e.Ownerid).HasColumnName("ownerid");
                entity.Property(e => e.Suggestion).HasColumnName("suggestion");
                entity.Property(e => e.YearMax).HasColumnName("yearMax");
                entity.Property(e => e.YearMin).HasColumnName("yearMin");
            });
            modelBuilder.Entity<Franchise>(entity =>
            {
                entity.ToTable("franchise");

                entity.Property(e => e.Franchiseid)
                    .ValueGeneratedNever()
                    .HasColumnName("franchiseid");

                entity.Property(e => e.Abbrev)
                    .HasMaxLength(10)
                    .HasColumnName("abbrev");

                entity.Property(e => e.Bbidavailablebalance).HasColumnName("bbidavailablebalance");

                entity.Property(e => e.Icon)
                    .HasMaxLength(1000)
                    .HasColumnName("icon");

                entity.Property(e => e.Ownername)
                    .HasMaxLength(40)
                    .HasColumnName("ownername");

                entity.Property(e => e.Teamname)
                    .IsRequired()
                    .HasMaxLength(140)
                    .HasColumnName("teamname");
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.ToTable("transaction");

                entity.Property(e => e.Transactionid)
                    .ValueGeneratedNever()
                    .HasColumnName("transactionid");

                entity.Property(e => e.Amount).HasColumnName("amount");

                entity.Property(e => e.Franchiseid).HasColumnName("franchiseid");

                entity.Property(e => e.Playername)
                    .IsRequired()
                    .HasMaxLength(75)
                    .HasColumnName("playername");

                entity.Property(e => e.Position)
                    .HasMaxLength(8)
                    .HasColumnName("position");

                entity.Property(e => e.Salary).HasColumnName("salary");

                entity.Property(e => e.Team)
                    .HasMaxLength(5)
                    .HasColumnName("team");

                entity.Property(e => e.Timestamp)
                    .HasColumnType("date")
                    .HasColumnName("timestamp");

                entity.Property(e => e.Yearoftransaction).HasColumnName("yearoftransaction");

                entity.Property(e => e.Years).HasColumnName("years");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

        public partial class PlayerEntity
        {
            [Key]
            public int Mflid { get; set; }
            public string? Firstname { get; set; }
            public string? Lastname { get; set; }
            public string? Position { get; set; }
            public string? Fullname { get; set; }
            public string? Team { get; set; }
            public int? Age { get; set; }
            public int? Height { get; set; }
            public int? Weight { get; set; }
            public string? Headshot { get; set; }
            public string? Actionshot { get; set; }
            public string? Espnid { get; set; }
            public string? College { get; set; }
            public string? Rotowireid { get; set; }
            public int? Draftround { get; set; }
            public int? Draftpick { get; set; }
            public int? Draftyear { get; set; }
            public int? Jersey { get; set; }
            public string? Draftteam { get; set; }
            public string? Cbsid { get; set; }
            public string? Rotoworldid { get; set; }
            public decimal? Lastseasonpts { get; set; }
            public bool? IsActive { get; set; }
            public virtual ICollection<BidEntity> Bids { get; } = new List<BidEntity>();
            public virtual ICollection<ContractEntity> Contracts { get; } = new List<ContractEntity>();

        }
        public partial class BidEntity
        {
            [Key]
            public int Bidid { get; set; }
            public int Bidlength { get; set; }
            public int Bidsalary { get; set; }
            public DateTime Expires { get; set; }
            public int Mflid { get; set; }
            public int Ownerid { get; set; }
            public int Leagueid { get; set; }
            public virtual ICollection<ContractEntity> Contracts { get; } = new List<ContractEntity>();
            public virtual LeagueEntity League { get; set; } = null!;
            public virtual ICollection<LotEntity> Lots { get; } = new List<LotEntity>();
            public virtual PlayerEntity Player { get; set; } = null!;
            public virtual LeagueOwnerEntity LeagueOwner { get; set; } = null!;
        }
        public partial class LeagueOwnerEntity
        {
            [Key]
            public int Leagueownerid { get; set; }
            public int Leagueid { get; set; }
            public int Ownerid { get; set; }
            public int Mflfranchiseid { get; set; }
            public int? Caproom { get; set; }
            public int? Yearsleft { get; set; }
            public virtual ICollection<BidEntity> Bids { get; } = new List<BidEntity>();
            public virtual ICollection<ContractEntity> Contracts { get; } = new List<ContractEntity>();
            public virtual LeagueEntity LeagueEntity { get; set; } = null!;
            public virtual OwnerEntity OwnerEntity { get; set; } = null!;
        }

        public partial class SuggestionEntity
        {
            public int Ownerid { get; set; }
            public int Suggestion { get; set; }
            public string Mflid { get; set; } = null!;
            public int? YearMin { get; set; }
            public int? YearMax { get; set; }
            [Key]
            public int Id { get; set; }
            public SuggestionEntity()
            {

            }

            public SuggestionEntity(int owner, string mfl, int salary)
            {
                Ownerid = owner;
                Mflid = mfl;
                Suggestion = salary;
            }
        }

        public partial class LotEntity
        {
            [Key]
            public int Lotid { get; set; }
            public int? Bidid { get; set; }
            public int Leagueid { get; set; }
            public virtual BidEntity? Bid { get; set; }
            public virtual LeagueEntity League { get; set; } = null!;
        }

        public partial class OwnerEntity
        {
            [Key]
            public int Ownerid { get; set; }
            public string? Ownername { get; set; }
            public string? PasswordHash { get; set; }
            public string? Displayname { get; set; }
            public bool? Premium { get; set; }
            public virtual ICollection<LeagueOwnerEntity> Leagueowners { get; } = new List<LeagueOwnerEntity>();
        }

        public partial class LeagueEntity
        {
            [Key]
            public int Mflid { get; set; }
            public string Name { get; set; } = null!;
            public string? Mflhash { get; set; }
            public string? Commishcookie { get; set; }
            public bool Isauctioning { get; set; }
            public virtual ICollection<BidEntity> Bids { get; } = new List<BidEntity>();
            public virtual ICollection<ContractEntity> Contracts { get; } = new List<ContractEntity>();
            public virtual ICollection<LeagueOwnerEntity> Leagueowners { get; } = new List<LeagueOwnerEntity>();
            public virtual ICollection<LotEntity> Lots { get; } = new List<LotEntity>();
        }

        public partial class ContractEntity
        {
            [Key]
            public int Id { get; set; }
            public int Mflid { get; set; }
            public int Length { get; set; }
            public int Salary { get; set; }
            public int Contractvalue { get; set; }
            public int Ownerid { get; set; }
            public int? Leagueid { get; set; }
            public int? Bidid { get; set; }
            public virtual BidEntity? Bid { get; set; }
            public virtual LeagueEntity? League { get; set; }
            public virtual PlayerEntity Player { get; set; } = null!;
            public virtual LeagueOwnerEntity Owner { get; set; } = null!;
        }
    }
}
