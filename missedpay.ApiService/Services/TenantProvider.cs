namespace missedpay.ApiService.Services;

/// <summary>
/// Service to provide the current tenant context
/// </summary>
public interface ITenantProvider
{
    Guid GetTenantId();
}

/// <summary>
/// Production implementation that extracts tenant ID from JWT claims
/// Expects a "tenant_id" claim in the authenticated user's token
/// </summary>
public class JwtTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<JwtTenantProvider> _logger;

    public JwtTenantProvider(
        IHttpContextAccessor httpContextAccessor,
        ILogger<JwtTenantProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Guid GetTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        if (httpContext == null)
        {
            _logger.LogError("HttpContext is null - cannot determine tenant");
            throw new InvalidOperationException("HttpContext is not available");
        }

        // First, check if user is authenticated
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("Unauthenticated request attempted to access tenant data");
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        // Try to get tenant_id from claims
        var tenantIdClaim = httpContext.User.FindFirst("tenant_id")?.Value
                           ?? httpContext.User.FindFirst("tid")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim))
        {
            _logger.LogError("Tenant ID claim not found in JWT for user {UserId}", 
                httpContext.User.Identity?.Name ?? "Unknown");
            throw new UnauthorizedAccessException("Tenant ID not found in authentication token");
        }

        if (!Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            _logger.LogError("Invalid tenant ID format in JWT: {TenantId}", tenantIdClaim);
            throw new UnauthorizedAccessException("Invalid tenant ID format in authentication token");
        }

        _logger.LogDebug("Resolved tenant ID {TenantId} from JWT claims", tenantId);
        return tenantId;
    }
}

/// <summary>
/// Implementation that extracts tenant ID from HTTP header (X-Tenant-Id)
/// Useful for API keys, service-to-service communication, or development testing
/// WARNING: Should be combined with proper authentication in production!
/// </summary>
public class HeaderTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<HeaderTenantProvider> _logger;

    public HeaderTenantProvider(
        IHttpContextAccessor httpContextAccessor,
        ILogger<HeaderTenantProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Guid GetTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        if (httpContext == null)
        {
            _logger.LogError("HttpContext is null - cannot determine tenant");
            throw new InvalidOperationException("HttpContext is not available");
        }

        // Try to get tenant ID from header
        var tenantIdHeader = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();

        if (string.IsNullOrEmpty(tenantIdHeader))
        {
            _logger.LogWarning("X-Tenant-Id header is missing from request");
            throw new UnauthorizedAccessException("X-Tenant-Id header is required");
        }

        if (!Guid.TryParse(tenantIdHeader, out var tenantId))
        {
            _logger.LogError("Invalid tenant ID format in header: {TenantId}", tenantIdHeader);
            throw new UnauthorizedAccessException("Invalid tenant ID format in X-Tenant-Id header");
        }

        _logger.LogDebug("Resolved tenant ID {TenantId} from X-Tenant-Id header", tenantId);
        return tenantId;
    }
}
