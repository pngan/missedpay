using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace missedpay.ApiService.Controllers;

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
    /// Deletes all data from the database (Transactions and Accounts).
    /// USE WITH CAUTION - This cannot be undone!
    /// </summary>
    /// <returns>Count of deleted records</returns>
    [HttpDelete("clear-all-data")]
    public async Task<ActionResult<object>> ClearAllData()
    {
        try
        {
            // Use execution strategy to handle transactions with retry logic
            var strategy = _context.Database.CreateExecutionStrategy();
            
            var result = await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                // Delete in order to respect foreign key constraints
                // Transactions first (child), then Accounts (parent)
                var transactionsDeleted = await _context.Transactions.ExecuteDeleteAsync();
                var accountsDeleted = await _context.Accounts.ExecuteDeleteAsync();

                await transaction.CommitAsync();
                
                return new { TransactionsDeleted = transactionsDeleted, AccountsDeleted = accountsDeleted };
            });

            _logger.LogWarning(
                "Database cleared: {TransactionsDeleted} transactions and {AccountsDeleted} accounts deleted",
                result.TransactionsDeleted, result.AccountsDeleted);

            return Ok(new
            {
                Message = "All data successfully deleted",
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
    /// </summary>
    /// <returns>Count of deleted transactions</returns>
    [HttpDelete("clear-transactions")]
    public async Task<ActionResult<object>> ClearTransactions()
    {
        try
        {
            var deletedCount = await _context.Transactions.ExecuteDeleteAsync();

            _logger.LogWarning("{DeletedCount} transactions deleted", deletedCount);

            return Ok(new
            {
                Message = "All transactions successfully deleted",
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
    /// </summary>
    /// <returns>Count of deleted accounts</returns>
    [HttpDelete("clear-accounts")]
    public async Task<ActionResult<object>> ClearAccounts()
    {
        try
        {
            // Use execution strategy to handle transactions with retry logic
            var strategy = _context.Database.CreateExecutionStrategy();
            
            var result = await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                // Delete transactions first to avoid foreign key issues
                var transactionsDeleted = await _context.Transactions.ExecuteDeleteAsync();
                var accountsDeleted = await _context.Accounts.ExecuteDeleteAsync();

                await transaction.CommitAsync();
                
                return new { TransactionsDeleted = transactionsDeleted, AccountsDeleted = accountsDeleted };
            });

            _logger.LogWarning(
                "{AccountsDeleted} accounts and {TransactionsDeleted} related transactions deleted",
                result.AccountsDeleted, result.TransactionsDeleted);

            return Ok(new
            {
                Message = "All accounts and related transactions successfully deleted",
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
    /// </summary>
    /// <returns>Database statistics</returns>
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetDatabaseStats()
    {
        try
        {
            var accountCount = await _context.Accounts.CountAsync();
            var transactionCount = await _context.Transactions.CountAsync();

            return Ok(new
            {
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
