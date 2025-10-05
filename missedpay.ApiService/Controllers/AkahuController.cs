using Microsoft.AspNetCore.Mvc;
using missedpay.ApiService.Models;
using missedpay.ApiService.Services;

namespace missedpay.ApiService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AkahuController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ITransactionService _transactionService;
    private readonly ITenantProvider _tenantProvider;
    private readonly AkahuService _akahuService;
    private readonly ILogger<AkahuController> _logger;

    public AkahuController(
        IAccountService accountService,
        ITransactionService transactionService,
        ITenantProvider tenantProvider,
        AkahuService akahuService,
        ILogger<AkahuController> logger)
    {
        _accountService = accountService;
        _transactionService = transactionService;
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

            // Use service to upsert accounts
            var (created, updated) = await _accountService.UpsertAccountsAsync(akahuAccounts, tenantId);

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

            // Use service to upsert transactions
            var (created, updated, skipped) = await _transactionService.UpsertTransactionsAsync(akahuTransactions, tenantId);

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
