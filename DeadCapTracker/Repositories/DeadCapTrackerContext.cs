using System;
using System.Collections.Generic;
using System.Linq;
using DeadCapTracker.Models;
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

        // public void GetTransactionsJoinedWithFranchises()
        // {
        //     var transactions =  Transactions.ToList();
        //     var franchises = Franchises.ToList();
        //     var allTransactions = (
        //             from t in transactions
        //             join f in franchises on t.Franchiseid equals f.Franchiseid into penalties
        //             from p in penalties.DefaultIfEmpty()
        //             select new
        //             {
        //                 FranchiseId = t.Franchiseid,
        //                 TeamName = p.Teamname,
        //                 DeadAmount = t.Amount,
        //                 PlayerName = t.Playername,
        //                 TransactionYear = t.Yearoftransaction,
        //                 NumOfYears = t.Years
        //             })
        //         .GroupBy(t => t.FranchiseId)
        //         .ToList();
        // }
        //
        
        
        
        
        
        
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


    }
}
