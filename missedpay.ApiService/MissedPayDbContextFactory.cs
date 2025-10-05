using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using missedpay.ApiService.Services;

namespace missedpay.ApiService;

/// <summary>
/// Design-time factory for MissedPayDbContext to support EF migrations
/// </summary>
public class MissedPayDbContextFactory : IDesignTimeDbContextFactory<MissedPayDbContext>
{
    public MissedPayDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MissedPayDbContext>();
        
        // Use a dummy connection string for design-time operations
        optionsBuilder.UseNpgsql("Host=localhost;Database=missedpay;Username=postgres;Password=postgres");
        
        return new MissedPayDbContext(optionsBuilder.Options);
    }
}

/// <summary>
/// Tenant provider for design-time operations (migrations, etc.)
/// </summary>
public class DesignTimeTenantProvider : ITenantProvider
{
    public Guid GetTenantId()
    {
        // Return empty GUID for design-time - query filters will be disabled
        return Guid.Empty;
    }
}
