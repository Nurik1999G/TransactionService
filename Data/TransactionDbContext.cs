using Microsoft.EntityFrameworkCore;
using TransactionService.Models;

namespace TransactionService.Data;

public class TransactionDbContext : DbContext
{
    public TransactionDbContext(DbContextOptions<TransactionDbContext> options) : base(options)
    {
    }

    public DbSet<CreditTransaction> CreditTransactions { get; set; }
    public DbSet<DebitTransaction> DebitTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Настройка CreditTransaction
        modelBuilder.Entity<CreditTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Id).IsUnique();
            entity.HasIndex(e => e.ClientId);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.DateTime).HasColumnType("timestamp with time zone");
            entity.Property(e => e.InsertDateTime).HasColumnType("timestamp with time zone");
            entity.Property(e => e.RevertDateTime).HasColumnType("timestamp with time zone");
        });

        // Настройка DebitTransaction
        modelBuilder.Entity<DebitTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Id).IsUnique();
            entity.HasIndex(e => e.ClientId);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.DateTime).HasColumnType("timestamp with time zone");
            entity.Property(e => e.InsertDateTime).HasColumnType("timestamp with time zone");
            entity.Property(e => e.RevertDateTime).HasColumnType("timestamp with time zone");
        });
    }
}