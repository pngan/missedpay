using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace missedpay.ApiService.Controllers;

/// <summary>
/// Admin controller for database management operations.
/// This controller is non-tenanted and can operate across all tenants or a specific tenant.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly MissedPayDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(MissedPayDbContext context, ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets the tenant ID from the required X-Tenant-Id header.
    /// Returns null if the header value is "ALL" (case insensitive), meaning operation applies to all tenants.
    /// Otherwise returns the specific tenant GUID.
    /// </summary>
    private Guid? GetTenantIdOrAll()
    {
        var tenantIdHeader = Request.Headers["X-Tenant-Id"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(tenantIdHeader))
        {
            throw new BadHttpRequestException("X-Tenant-Id header is required. Use a tenant GUID or 'ALL' for all tenants.");
        }

        // Check for "ALL" keyword (case insensitive)
        if (tenantIdHeader.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            return null; // null means ALL tenants
        }

        // Try to parse as GUID
        if (Guid.TryParse(tenantIdHeader, out var tenantId))
        {
            return tenantId;
        }

        throw new BadHttpRequestException($"Invalid X-Tenant-Id header value: '{tenantIdHeader}'. Must be a valid GUID or 'ALL'.");
    }

    /// <summary>
    /// Deletes all data from the database (Transactions and Accounts).
    /// X-Tenant-Id header is required: use a tenant GUID for specific tenant, or 'ALL' for all tenants.
    /// USE WITH CAUTION - This cannot be undone!
    /// </summary>
    [HttpDelete("clear-all-data")]
    public async Task<ActionResult<object>> ClearAllData()
    {
        try
        {
            var tenantId = GetTenantIdOrAll();
            
            if (tenantId.HasValue)
            {
                _context.SetTenantId(tenantId.Value);
                _logger.LogWarning("Clearing all data for tenant {TenantId}", tenantId.Value);
            }
            else
            {
                _logger.LogWarning("Clearing all data for ALL tenants");
            }
            
            // Use execution strategy to handle transactions with retry logic
            var strategy = _context.Database.CreateExecutionStrategy();
            
            var result = await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                // Delete in order to respect foreign key constraints
                // Transactions first (child), then Accounts (parent)
                // If no tenant specified, ignore query filters to operate on ALL tenants
                var transactionsDeleted = tenantId.HasValue 
                    ? await _context.Transactions.ExecuteDeleteAsync()
                    : await _context.Transactions.IgnoreQueryFilters().ExecuteDeleteAsync();
                    
                var accountsDeleted = tenantId.HasValue
                    ? await _context.Accounts.ExecuteDeleteAsync()
                    : await _context.Accounts.IgnoreQueryFilters().ExecuteDeleteAsync();

                await transaction.CommitAsync();
                
                return new { TransactionsDeleted = transactionsDeleted, AccountsDeleted = accountsDeleted };
            });

            var message = tenantId.HasValue 
                ? $"All data for tenant {tenantId.Value} successfully deleted"
                : "All data for ALL tenants successfully deleted";

            _logger.LogWarning(
                "{Message}: {TransactionsDeleted} transactions and {AccountsDeleted} accounts deleted",
                message, result.TransactionsDeleted, result.AccountsDeleted);

            return Ok(new
            {
                Message = message,
                TenantId = tenantId?.ToString() ?? "ALL",
                TransactionsDeleted = result.TransactionsDeleted,
                AccountsDeleted = result.AccountsDeleted,
                TotalDeleted = result.TransactionsDeleted + result.AccountsDeleted
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing database");
            return StatusCode(500, new { Message = "Error clearing database", Error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes all transactions from the database (keeps accounts).
    /// X-Tenant-Id header is required: use a tenant GUID for specific tenant, or 'ALL' for all tenants.
    /// </summary>
    [HttpDelete("clear-transactions")]
    public async Task<ActionResult<object>> ClearTransactions()
    {
        try
        {
            var tenantId = GetTenantIdOrAll();
            
            if (tenantId.HasValue)
            {
                _context.SetTenantId(tenantId.Value);
                _logger.LogWarning("Clearing transactions for tenant {TenantId}", tenantId.Value);
            }
            else
            {
                _logger.LogWarning("Clearing transactions for ALL tenants");
            }
            
            // If no tenant specified, ignore query filters to operate on ALL tenants
            var deletedCount = tenantId.HasValue
                ? await _context.Transactions.ExecuteDeleteAsync()
                : await _context.Transactions.IgnoreQueryFilters().ExecuteDeleteAsync();

            var message = tenantId.HasValue
                ? $"All transactions for tenant {tenantId.Value} successfully deleted"
                : "All transactions for ALL tenants successfully deleted";

            _logger.LogWarning("{Message}: {DeletedCount} transactions deleted", message, deletedCount);

            return Ok(new
            {
                Message = message,
                TenantId = tenantId?.ToString() ?? "ALL",
                TransactionsDeleted = deletedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing transactions");
            return StatusCode(500, new { Message = "Error clearing transactions", Error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes all accounts from the database (will cascade delete transactions if configured).
    /// X-Tenant-Id header is required: use a tenant GUID for specific tenant, or 'ALL' for all tenants.
    /// </summary>
    [HttpDelete("clear-accounts")]
    public async Task<ActionResult<object>> ClearAccounts()
    {
        try
        {
            var tenantId = GetTenantIdOrAll();
            
            if (tenantId.HasValue)
            {
                _context.SetTenantId(tenantId.Value);
                _logger.LogWarning("Clearing accounts for tenant {TenantId}", tenantId.Value);
            }
            else
            {
                _logger.LogWarning("Clearing accounts for ALL tenants");
            }
            
            // Use execution strategy to handle transactions with retry logic
            var strategy = _context.Database.CreateExecutionStrategy();
            
            var result = await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                // Delete transactions first to avoid foreign key issues
                // If no tenant specified, ignore query filters to operate on ALL tenants
                var transactionsDeleted = tenantId.HasValue
                    ? await _context.Transactions.ExecuteDeleteAsync()
                    : await _context.Transactions.IgnoreQueryFilters().ExecuteDeleteAsync();
                    
                var accountsDeleted = tenantId.HasValue
                    ? await _context.Accounts.ExecuteDeleteAsync()
                    : await _context.Accounts.IgnoreQueryFilters().ExecuteDeleteAsync();

                await transaction.CommitAsync();
                
                return new { TransactionsDeleted = transactionsDeleted, AccountsDeleted = accountsDeleted };
            });

            var message = tenantId.HasValue
                ? $"All accounts for tenant {tenantId.Value} successfully deleted"
                : "All accounts for ALL tenants successfully deleted";

            _logger.LogWarning(
                "{Message}: {AccountsDeleted} accounts and {TransactionsDeleted} related transactions deleted",
                message, result.AccountsDeleted, result.TransactionsDeleted);

            return Ok(new
            {
                Message = message,
                TenantId = tenantId?.ToString() ?? "ALL",
                AccountsDeleted = result.AccountsDeleted,
                TransactionsDeleted = result.TransactionsDeleted,
                TotalDeleted = result.AccountsDeleted + result.TransactionsDeleted
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing accounts");
            return StatusCode(500, new { Message = "Error clearing accounts", Error = ex.Message });
        }
    }

    /// <summary>
    /// Gets database statistics (record counts).
    /// X-Tenant-Id header is required: use a tenant GUID for specific tenant, or 'ALL' for all tenants.
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetDatabaseStats()
    {
        try
        {
            var tenantId = GetTenantIdOrAll();
            
            if (tenantId.HasValue)
            {
                _context.SetTenantId(tenantId.Value);
            }
            
            // If no tenant specified, ignore query filters to count ALL tenants
            var accountCount = tenantId.HasValue
                ? await _context.Accounts.CountAsync()
                : await _context.Accounts.IgnoreQueryFilters().CountAsync();
                
            var transactionCount = tenantId.HasValue
                ? await _context.Transactions.CountAsync()
                : await _context.Transactions.IgnoreQueryFilters().CountAsync();

            return Ok(new
            {
                TenantId = tenantId?.ToString() ?? "ALL",
                Accounts = accountCount,
                Transactions = transactionCount,
                Total = accountCount + transactionCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database stats");
            return StatusCode(500, new { Message = "Error getting database stats", Error = ex.Message });
        }
    }
}
