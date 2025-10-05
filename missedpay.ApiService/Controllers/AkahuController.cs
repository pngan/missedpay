using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using missedpay.ApiService.Models;
using missedpay.ApiService.Services;

namespace missedpay.ApiService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AkahuController : ControllerBase
{
    private readonly MissedPayDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly AkahuService _akahuService;
    private readonly ILogger<AkahuController> _logger;

    public AkahuController(
        MissedPayDbContext context, 
        ITenantProvider tenantProvider,
        AkahuService akahuService,
        ILogger<AkahuController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _akahuService = akahuService;
        _logger = logger;
    }

    /// <summary>
    /// Refresh accounts from Akahu API
    /// </summary>
    /// <returns>Number of accounts created/updated</returns>
    [HttpPost("refresh-accounts")]
    public async Task<ActionResult<RefreshAccountsResponse>> RefreshAccounts()
    {
        try
        {
            var tenantId = _tenantProvider.GetTenantId();
            _context.SetTenantId(tenantId);

            _logger.LogInformation($"Refreshing accounts for tenant {tenantId}");

            // Get accounts from Akahu
            var akahuAccounts = await _akahuService.GetAccountsAsync();

            if (akahuAccounts.Count == 0)
            {
                return Ok(new RefreshAccountsResponse
                {
                    Success = true,
                    AccountsCreated = 0,
                    AccountsUpdated = 0,
                    Message = "No accounts found in Akahu"
                });
            }

            int created = 0;
            int updated = 0;

            foreach (var akahuAccount in akahuAccounts)
            {
                // Set tenant ID
                akahuAccount.TenantId = tenantId;

                // Check if account already exists
                var existingAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.Id == akahuAccount.Id);

                if (existingAccount == null)
                {
                    // Create new account
                    _context.Accounts.Add(akahuAccount);
                    created++;
                    _logger.LogInformation($"Creating new account: {akahuAccount.Id} - {akahuAccount.Name}");
                }
                else
                {
                    // Update existing account
                    existingAccount.Name = akahuAccount.Name;
                    existingAccount.Status = akahuAccount.Status;
                    existingAccount.FormattedAccount = akahuAccount.FormattedAccount;
                    existingAccount.Balance = akahuAccount.Balance;
                    existingAccount.Type = akahuAccount.Type;
                    existingAccount.Attributes = akahuAccount.Attributes;
                    existingAccount.Connection = akahuAccount.Connection;
                    existingAccount.TenantId = tenantId; // Ensure tenant doesn't change
                    
                    _context.Entry(existingAccount).State = EntityState.Modified;
                    updated++;
                    _logger.LogInformation($"Updating existing account: {akahuAccount.Id} - {akahuAccount.Name}");
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new RefreshAccountsResponse
            {
                Success = true,
                AccountsCreated = created,
                AccountsUpdated = updated,
                TotalAccounts = akahuAccounts.Count,
                Message = $"Successfully refreshed {akahuAccounts.Count} accounts"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing accounts from Akahu");
            return StatusCode(500, new RefreshAccountsResponse
            {
                Success = false,
                Message = $"Error refreshing accounts: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Refresh transactions from Akahu API
    /// </summary>
    /// <returns>Number of transactions created/updated</returns>
    [HttpPost("refresh-transactions")]
    public async Task<ActionResult<RefreshTransactionsResponse>> RefreshTransactions()
    {
        try
        {
            var tenantId = _tenantProvider.GetTenantId();
            _context.SetTenantId(tenantId);

            _logger.LogInformation($"Refreshing transactions for tenant {tenantId}");

            // Get transactions from Akahu
            var akahuTransactions = await _akahuService.GetTransactionsAsync();

            if (akahuTransactions.Count == 0)
            {
                return Ok(new RefreshTransactionsResponse
                {
                    Success = true,
                    TransactionsCreated = 0,
                    TransactionsUpdated = 0,
                    Message = "No transactions found in Akahu"
                });
            }

            int created = 0;
            int updated = 0;
            int skipped = 0;

            foreach (var akahuTransaction in akahuTransactions)
            {
                // Set tenant ID
                akahuTransaction.TenantId = tenantId;

                // Verify the account exists for this tenant
                var accountExists = await _context.Accounts
                    .AnyAsync(a => a.Id == akahuTransaction.Account);

                if (!accountExists)
                {
                    _logger.LogWarning($"Skipping transaction {akahuTransaction.Id} - Account {akahuTransaction.Account} not found");
                    skipped++;
                    continue;
                }

                // Check if transaction already exists
                var existingTransaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.Id == akahuTransaction.Id);

                if (existingTransaction == null)
                {
                    // Create new transaction
                    _context.Transactions.Add(akahuTransaction);
                    created++;
                }
                else
                {
                    // Update existing transaction
                    existingTransaction.Description = akahuTransaction.Description;
                    existingTransaction.Amount = akahuTransaction.Amount;
                    existingTransaction.Balance = akahuTransaction.Balance;
                    existingTransaction.Type = akahuTransaction.Type;
                    existingTransaction.Category = akahuTransaction.Category;
                    existingTransaction.Merchant = akahuTransaction.Merchant;
                    existingTransaction.Meta = akahuTransaction.Meta;
                    existingTransaction.TenantId = tenantId; // Ensure tenant doesn't change
                    
                    _context.Entry(existingTransaction).State = EntityState.Modified;
                    updated++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new RefreshTransactionsResponse
            {
                Success = true,
                TransactionsCreated = created,
                TransactionsUpdated = updated,
                TransactionsSkipped = skipped,
                TotalTransactions = akahuTransactions.Count,
                Message = $"Successfully refreshed {created + updated} transactions ({skipped} skipped)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing transactions from Akahu");
            return StatusCode(500, new RefreshTransactionsResponse
            {
                Success = false,
                Message = $"Error refreshing transactions: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Refresh both accounts and transactions from Akahu API
    /// </summary>
    /// <returns>Summary of refresh operation</returns>
    [HttpPost("refresh-all")]
    public async Task<ActionResult<RefreshAllResponse>> RefreshAll()
    {
        try
        {
            var tenantId = _tenantProvider.GetTenantId();
            _logger.LogInformation($"Refreshing all data for tenant {tenantId}");

            // First refresh accounts
            var accountsResult = await RefreshAccounts();
            var accountsResponse = (accountsResult.Result as OkObjectResult)?.Value as RefreshAccountsResponse;

            // Then refresh transactions
            var transactionsResult = await RefreshTransactions();
            var transactionsResponse = (transactionsResult.Result as OkObjectResult)?.Value as RefreshTransactionsResponse;

            return Ok(new RefreshAllResponse
            {
                Success = accountsResponse?.Success == true && transactionsResponse?.Success == true,
                AccountsCreated = accountsResponse?.AccountsCreated ?? 0,
                AccountsUpdated = accountsResponse?.AccountsUpdated ?? 0,
                TransactionsCreated = transactionsResponse?.TransactionsCreated ?? 0,
                TransactionsUpdated = transactionsResponse?.TransactionsUpdated ?? 0,
                TransactionsSkipped = transactionsResponse?.TransactionsSkipped ?? 0,
                Message = "Refresh completed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing all data from Akahu");
            return StatusCode(500, new RefreshAllResponse
            {
                Success = false,
                Message = $"Error refreshing data: {ex.Message}"
            });
        }
    }
}

// Response models
public class RefreshAccountsResponse
{
    public bool Success { get; set; }
    public int AccountsCreated { get; set; }
    public int AccountsUpdated { get; set; }
    public int TotalAccounts { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class RefreshTransactionsResponse
{
    public bool Success { get; set; }
    public int TransactionsCreated { get; set; }
    public int TransactionsUpdated { get; set; }
    public int TransactionsSkipped { get; set; }
    public int TotalTransactions { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class RefreshAllResponse
{
    public bool Success { get; set; }
    public int AccountsCreated { get; set; }
    public int AccountsUpdated { get; set; }
    public int TransactionsCreated { get; set; }
    public int TransactionsUpdated { get; set; }
    public int TransactionsSkipped { get; set; }
    public string Message { get; set; } = string.Empty;
}
