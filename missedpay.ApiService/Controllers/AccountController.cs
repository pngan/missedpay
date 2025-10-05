using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using missedpay.ApiService.Models;
using missedpay.ApiService.Services;

[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly MissedPayDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    
    public AccountController(MissedPayDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    // GET: api/Account
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Account>>> GetAccount()
    {
        _context.SetTenantId(_tenantProvider.GetTenantId());
        return await _context.Accounts.ToListAsync();
    }

    // GET: api/Account/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Account>> GetAccount(string id)
    {
        _context.SetTenantId(_tenantProvider.GetTenantId());
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id);

        if (account == null)
        {
            return NotFound();
        }

        return account;
    }

    // PUT: api/Account/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutAccount(string? id, Account account)
    {
        if (id != account.Id)
        {
            return BadRequest();
        }

        var tenantId = _tenantProvider.GetTenantId();
        _context.SetTenantId(tenantId);
        
        // Ensure the account maintains the correct tenant ID (prevent tenant switching)
        account.TenantId = tenantId;
        
        _context.Entry(account).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!AccountExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/Account
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Account>> PostAccount(Account account)
    {
        var tenantId = _tenantProvider.GetTenantId();
        _context.SetTenantId(tenantId);
        
        // Ensure the account has the correct tenant ID
        account.TenantId = tenantId;
        
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetAccount", new { id = account.Id }, account);
    }

    // DELETE: api/Account/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount(string? id)
    {
        _context.SetTenantId(_tenantProvider.GetTenantId());
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id);
        if (account == null)
        {
            return NotFound();
        }

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool AccountExists(string? id)
    {
        _context.SetTenantId(_tenantProvider.GetTenantId());
        return _context.Accounts.Any(e => e.Id == id);
    }
}
