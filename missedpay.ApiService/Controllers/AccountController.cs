using Microsoft.AspNetCore.Mvc;
using missedpay.ApiService.Models;
using missedpay.ApiService.Services;

[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ITenantProvider _tenantProvider;
    
    public AccountController(IAccountService accountService, ITenantProvider tenantProvider)
    {
        _accountService = accountService;
        _tenantProvider = tenantProvider;
    }

    // GET: api/Account
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Account>>> GetAccount()
    {
        var tenantId = _tenantProvider.GetTenantId();
        var accounts = await _accountService.GetAllAccountsAsync(tenantId);
        return Ok(accounts);
    }

    // GET: api/Account/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Account>> GetAccount(string id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var account = await _accountService.GetAccountByIdAsync(id, tenantId);

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
        var updatedAccount = await _accountService.UpdateAccountAsync(id, account, tenantId);

        if (updatedAccount == null)
        {
            return NotFound();
        }

        return NoContent();
    }

    // POST: api/Account
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Account>> PostAccount(Account account)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var createdAccount = await _accountService.CreateAccountAsync(account, tenantId);

        return CreatedAtAction("GetAccount", new { id = createdAccount.Id }, createdAccount);
    }

    // DELETE: api/Account/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount(string id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var deleted = await _accountService.DeleteAccountAsync(id, tenantId);
        
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
