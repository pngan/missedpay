using Microsoft.EntityFrameworkCore;
using missedpay.ApiService.Models;

public class MissedPayDbContext(DbContextOptions<MissedPayDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts { get; set; } = default!;
    public DbSet<Transaction> Transactions { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Account
        modelBuilder.Entity<Account>(entity =>
        {
            entity.OwnsOne(a => a.Connection);
            entity.OwnsOne(a => a.Balance);
            entity.OwnsOne(a => a.Refreshed);
            
            // Store Meta as JSON column
            entity.Navigation(a => a.Meta).IsRequired(false);
            entity.Property(a => a.Meta)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<AccountMeta>(v, (System.Text.Json.JsonSerializerOptions?)null)!);
        });

        // Configure Transaction
        modelBuilder.Entity<Transaction>(entity =>
        {
            // Store complex types as JSON columns
            entity.Property(t => t.Category)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<TransactionCategory>(v, (System.Text.Json.JsonSerializerOptions?)null));
            
            entity.Property(t => t.Merchant)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Merchant>(v, (System.Text.Json.JsonSerializerOptions?)null));
            
            entity.Property(t => t.Meta)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<TransactionMeta>(v, (System.Text.Json.JsonSerializerOptions?)null));
        });
    }
}
