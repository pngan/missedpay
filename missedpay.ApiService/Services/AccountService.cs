using Microsoft.EntityFrameworkCore;
using missedpay.ApiService.Models;

namespace missedpay.ApiService.Services;

public interface IAccountService
{
    Task<List<Account>> GetAllAccountsAsync(Guid tenantId);
    Task<Account?> GetAccountByIdAsync(string id, Guid tenantId);
    Task<Account> CreateAccountAsync(Account account, Guid tenantId);
    Task<Account?> UpdateAccountAsync(string id, Account account, Guid tenantId);
    Task<bool> DeleteAccountAsync(string id, Guid tenantId);
    Task<bool> AccountExistsAsync(string id, Guid tenantId);
    Task<(int created, int updated)> UpsertAccountsAsync(List<Account> accounts, Guid tenantId);
}

public class AccountService : IAccountService
{
    private readonly MissedPayDbContext _context;
    private readonly ILogger<AccountService> _logger;

    public AccountService(MissedPayDbContext context, ILogger<AccountService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Account>> GetAllAccountsAsync(Guid tenantId)
    {
        _context.SetTenantId(tenantId);
        return await _context.Accounts.ToListAsync();
    }

    public async Task<Account?> GetAccountByIdAsync(string id, Guid tenantId)
    {
        _context.SetTenantId(tenantId);
        return await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Account> CreateAccountAsync(Account account, Guid tenantId)
    {
        account.TenantId = tenantId;
        _context.SetTenantId(tenantId);
        
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        
        return account;
    }

    public async Task<Account?> UpdateAccountAsync(string id, Account account, Guid tenantId)
    {
        if (id != account.Id)
        {
            return null;
        }

        account.TenantId = tenantId;
        _context.SetTenantId(tenantId);

        try
        {
            _context.Entry(account).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return account;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await AccountExistsAsync(id, tenantId))
            {
                return null;
            }
            throw;
        }
    }

    public async Task<bool> DeleteAccountAsync(string id, Guid tenantId)
    {
        _context.SetTenantId(tenantId);
        
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id);
        if (account == null)
        {
            return false;
        }

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();
        
        return true;
    }

    public async Task<bool> AccountExistsAsync(string id, Guid tenantId)
    {
        _context.SetTenantId(tenantId);
        return await _context.Accounts.AnyAsync(e => e.Id == id);
    }

    /// <summary>
    /// Upsert (Create or Update) multiple accounts from external sources like Akahu
    /// </summary>
    public async Task<(int created, int updated)> UpsertAccountsAsync(List<Account> accounts, Guid tenantId)
    {
        _context.SetTenantId(tenantId);
        
        int created = 0;
        int updated = 0;

        foreach (var account in accounts)
        {
            account.TenantId = tenantId;

            // Check if account already exists (use AsNoTracking to avoid tracking conflicts)
            var existingAccount = await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == account.Id);

            if (existingAccount == null)
            {
                // Create new account - clear change tracker to avoid conflicts
                _context.ChangeTracker.Clear();
                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();
                created++;
                _logger.LogInformation($"Created account: {account.Id} - {account.Name}");
            }
            else
            {
                // Update existing account
                _context.ChangeTracker.Clear();
                
                existingAccount.Name = account.Name;
                existingAccount.Status = account.Status;
                existingAccount.FormattedAccount = account.FormattedAccount;
                existingAccount.Balance = account.Balance;
                existingAccount.Type = account.Type;
                existingAccount.Attributes = account.Attributes;
                existingAccount.Connection = account.Connection;
                existingAccount.Authorisation = account.Authorisation;
                existingAccount.Meta = account.Meta;
                existingAccount.Refreshed = account.Refreshed;
                existingAccount.TenantId = tenantId; // Ensure tenant doesn't change
                
                _context.Accounts.Update(existingAccount);
                await _context.SaveChangesAsync();
                updated++;
                _logger.LogInformation($"Updated account: {account.Id} - {account.Name}");
            }
        }

        return (created, updated);
    }
}
