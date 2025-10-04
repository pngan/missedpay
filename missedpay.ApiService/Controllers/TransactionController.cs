using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using missedpay.ApiService.Models;

[Route("api/[controller]")]
[ApiController]
public class TransactionController : ControllerBase
{
    private readonly MissedPayDbContext _context;
    public TransactionController(MissedPayDbContext context)
    {
        _context = context;
    }

    // GET: api/Transaction
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetTransaction()
    {
        return await _context.Transaction.ToListAsync();
    }

    // GET: api/Transaction/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Transaction>> GetTransaction(string id)
    {
        var transaction = await _context.Transaction.FindAsync(id);

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
        _context.Transaction.Add(transaction);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetTransaction", new { id = transaction.Id }, transaction);
    }

    // DELETE: api/Transaction/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(string? id)
    {
        var transaction = await _context.Transaction.FindAsync(id);
        if (transaction == null)
        {
            return NotFound();
        }

        _context.Transaction.Remove(transaction);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool TransactionExists(string? id)
    {
        return _context.Transaction.Any(e => e.Id == id);
    }
}
