using Microsoft.EntityFrameworkCore;
using missedpay.ApiService.Models;

public class MissedPayDbContext : DbContext
{
    private Guid _tenantId;

    public MissedPayDbContext(DbContextOptions<MissedPayDbContext> options) 
        : base(options)
    {
        // Tenant will be set via SetTenantId() after construction
    }

    public DbSet<Account> Accounts { get; set; } = default!;
    public DbSet<Transaction> Transactions { get; set; } = default!;

    /// <summary>
    /// Sets the tenant ID for this DbContext instance.
    /// Must be called before any queries are executed.
    /// </summary>
    public void SetTenantId(Guid tenantId)
    {
        _tenantId = tenantId;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Account
        modelBuilder.Entity<Account>(entity =>
        {
            // Add index on TenantId for efficient filtering
            entity.HasIndex(a => a.TenantId);
            
            // Add composite index on TenantId and Id for lookups
            entity.HasIndex(a => new { a.TenantId, a.Id });
            
            // Global query filter for multi-tenancy
            entity.HasQueryFilter(a => a.TenantId == _tenantId);
            
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
            // Add index on TenantId for efficient filtering
            entity.HasIndex(t => t.TenantId);
            
            // Add composite index on TenantId and Id for lookups
            entity.HasIndex(t => new { t.TenantId, t.Id });
            
            // Add composite index on TenantId and AccountId for account queries
            entity.HasIndex(t => new { t.TenantId, t.AccountId });
            
            // Global query filter for multi-tenancy
            entity.HasQueryFilter(t => t.TenantId == _tenantId);
            
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

    public override int SaveChanges()
    {
        SetTenantIdOnEntities();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTenantIdOnEntities();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void SetTenantIdOnEntities()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = _tenantId;
            }
        }
    }
}
