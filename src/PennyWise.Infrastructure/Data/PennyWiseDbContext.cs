using Microsoft.EntityFrameworkCore;
using PennyWise.Domain.Entities;

namespace PennyWise.Infrastructure.Data;

/// <summary>
/// Entity Framework Core DbContext for PennyWise.
/// Configures entity mappings and seed data.
/// </summary>
public class PennyWiseDbContext : DbContext
{
    public PennyWiseDbContext(DbContextOptions<PennyWiseDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<Holding> Holdings => Set<Holding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ─────────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50).HasDefaultValue("User");
        });

        // ── Transaction ──────────────────────────────────────────
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Transactions)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Budget ───────────────────────────────────────────────
        modelBuilder.Entity<Budget>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LimitAmount).HasPrecision(18, 2);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Budgets)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Portfolio ────────────────────────────────────────────
        modelBuilder.Entity<Portfolio>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Portfolios)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Holding ──────────────────────────────────────────────
        modelBuilder.Entity<Holding>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TickerSymbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.InstrumentName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Quantity).HasPrecision(18, 6);
            entity.Property(e => e.PurchasePrice).HasPrecision(18, 4);
            entity.Property(e => e.CurrentPrice).HasPrecision(18, 4);
            entity.HasOne(e => e.Portfolio)
                  .WithMany(p => p.Holdings)
                  .HasForeignKey(e => e.PortfolioId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
