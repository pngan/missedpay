namespace missedpay.ApiService.Services;

/// <summary>
/// Extension methods for registering tenant providers
/// </summary>
public static class TenantProviderExtensions
{
    /// <summary>
    /// Registers the appropriate tenant provider based on configuration
    /// </summary>
    public static IServiceCollection AddTenantProvider(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Register HttpContextAccessor (required for all providers)
        services.AddHttpContextAccessor();

        // Get tenant provider type from configuration (defaults to "Header")
        var providerType = configuration.GetValue<string>("TenantProvider:Type") ?? "Header";

        switch (providerType.ToLowerInvariant())
        {
            case "jwt":
                services.AddScoped<ITenantProvider, JwtTenantProvider>();
                break;
            
            case "header":
            default:
                services.AddScoped<ITenantProvider, HeaderTenantProvider>();
                break;
        }

        return services;
    }
}
