using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using missedpay.ApiService.Models;
using missedpay.ApiService.Services;

[Route("api/[controller]")]
[ApiController]
public class TransactionController : ControllerBase
{
    private readonly MissedPayDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    
    public TransactionController(MissedPayDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    // GET: api/Transaction
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetTransaction()
    {
        _context.SetTenantId(_tenantProvider.GetTenantId());
        return await _context.Transactions.ToListAsync();
    }

    // GET: api/Transaction/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Transaction>> GetTransaction(string id)
    {
        _context.SetTenantId(_tenantProvider.GetTenantId());
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null)
        {
            return NotFound();
        }

        return transaction;
    }

    // PUT: api/Transaction/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTransaction(string? id, Transaction transaction)
    {
        if (id != transaction.Id)
        {
            return BadRequest();
        }

        var tenantId = _tenantProvider.GetTenantId();
        _context.SetTenantId(tenantId);
        
        // Ensure the transaction maintains the correct tenant ID (prevent tenant switching)
        transaction.TenantId = tenantId;
        
        _context.Entry(transaction).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TransactionExists(id))
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

    // POST: api/Transaction
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Transaction>> PostTransaction(Transaction transaction)
    {
        var tenantId = _tenantProvider.GetTenantId();
        _context.SetTenantId(tenantId);
        
        // Ensure the transaction has the correct tenant ID
        transaction.TenantId = tenantId;
        
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetTransaction", new { id = transaction.Id }, transaction);
    }

    // DELETE: api/Transaction/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(string? id)
    {
        _context.SetTenantId(_tenantProvider.GetTenantId());
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id);
        if (transaction == null)
        {
            return NotFound();
        }

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool TransactionExists(string? id)
    {
        _context.SetTenantId(_tenantProvider.GetTenantId());
        return _context.Transactions.Any(e => e.Id == id);
    }
}
