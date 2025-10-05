using Microsoft.EntityFrameworkCore;
using missedpay.ApiService.Models;

namespace missedpay.ApiService.Services;

public interface ITransactionService
{
    Task<List<Transaction>> GetAllTransactionsAsync(Guid tenantId);
    Task<Transaction?> GetTransactionByIdAsync(string id, Guid tenantId);
    Task<Transaction> CreateTransactionAsync(Transaction transaction, Guid tenantId);
    Task<Transaction?> UpdateTransactionAsync(string id, Transaction transaction, Guid tenantId);
    Task<bool> DeleteTransactionAsync(string id, Guid tenantId);
    Task<bool> TransactionExistsAsync(string id, Guid tenantId);
    Task<(int created, int updated, int skipped)> UpsertTransactionsAsync(List<Transaction> transactions, Guid tenantId);
}

public class TransactionService : ITransactionService
{
    private readonly MissedPayDbContext _context;
    private readonly IAccountService _accountService;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        MissedPayDbContext context, 
        IAccountService accountService,
        ILogger<TransactionService> logger)
    {
        _context = context;
        _accountService = accountService;
        _logger = logger;
    }

    public async Task<List<Transaction>> GetAllTransactionsAsync(Guid tenantId)
    {
        _context.SetTenantId(tenantId);
        return await _context.Transactions.ToListAsync();
    }

    public async Task<Transaction?> GetTransactionByIdAsync(string id, Guid tenantId)
    {
        _context.SetTenantId(tenantId);
        return await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Transaction> CreateTransactionAsync(Transaction transaction, Guid tenantId)
    {
        transaction.TenantId = tenantId;
        _context.SetTenantId(tenantId);
        
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        
        return transaction;
    }

    public async Task<Transaction?> UpdateTransactionAsync(string id, Transaction transaction, Guid tenantId)
    {
        if (id != transaction.Id)
        {
            return null;
        }

        transaction.TenantId = tenantId;
        _context.SetTenantId(tenantId);

        try
        {
            _context.Entry(transaction).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return transaction;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await TransactionExistsAsync(id, tenantId))
            {
                return null;
            }
            throw;
        }
    }

    public async Task<bool> DeleteTransactionAsync(string id, Guid tenantId)
    {
        _context.SetTenantId(tenantId);
        
        var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id);
        if (transaction == null)
        {
            return false;
        }

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync();
        
        return true;
    }

    public async Task<bool> TransactionExistsAsync(string id, Guid tenantId)
    {
        _context.SetTenantId(tenantId);
        return await _context.Transactions.AnyAsync(e => e.Id == id);
    }

    /// <summary>
    /// Upsert (Create or Update) multiple transactions from external sources like Akahu
    /// </summary>
    public async Task<(int created, int updated, int skipped)> UpsertTransactionsAsync(List<Transaction> transactions, Guid tenantId)
    {
        _context.SetTenantId(tenantId);
        
        int created = 0;
        int updated = 0;
        int skipped = 0;

        foreach (var transaction in transactions)
        {
            transaction.TenantId = tenantId;

            // Verify the account exists for this tenant
            var accountExists = await _accountService.AccountExistsAsync(transaction.AccountId, tenantId);

            if (!accountExists)
            {
                _logger.LogWarning($"Skipping transaction {transaction.Id} - Account {transaction.AccountId} not found");
                skipped++;
                continue;
            }

            // Check if transaction already exists (use AsNoTracking to avoid tracking conflicts)
            var existingTransaction = await _context.Transactions
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == transaction.Id);

            if (existingTransaction == null)
            {
                // Create new transaction - clear change tracker to avoid conflicts
                _context.ChangeTracker.Clear();
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
                created++;
                _logger.LogInformation($"Created transaction: {transaction.Id}");
            }
            else
            {
                // Update existing transaction
                _context.ChangeTracker.Clear();
                
                existingTransaction.Description = transaction.Description;
                existingTransaction.Amount = transaction.Amount;
                existingTransaction.Balance = transaction.Balance;
                existingTransaction.Type = transaction.Type;
                existingTransaction.Category = transaction.Category;
                existingTransaction.Merchant = transaction.Merchant;
                existingTransaction.Meta = transaction.Meta;
                existingTransaction.Date = transaction.Date;
                existingTransaction.CreatedAt = transaction.CreatedAt;
                existingTransaction.TenantId = tenantId; // Ensure tenant doesn't change
                
                _context.Transactions.Update(existingTransaction);
                await _context.SaveChangesAsync();
                updated++;
                _logger.LogInformation($"Updated transaction: {transaction.Id}");
            }
        }

        return (created, updated, skipped);
    }
}
