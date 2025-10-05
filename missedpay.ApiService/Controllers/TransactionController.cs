using Microsoft.AspNetCore.Mvc;
using missedpay.ApiService.Models;
using missedpay.ApiService.Services;

[Route("api/[controller]")]
[ApiController]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ITenantProvider _tenantProvider;
    
    public TransactionController(ITransactionService transactionService, ITenantProvider tenantProvider)
    {
        _transactionService = transactionService;
        _tenantProvider = tenantProvider;
    }

    // GET: api/Transaction
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetTransaction()
    {
        var tenantId = _tenantProvider.GetTenantId();
        var transactions = await _transactionService.GetAllTransactionsAsync(tenantId);
        return Ok(transactions);
    }

    // GET: api/Transaction/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Transaction>> GetTransaction(string id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var transaction = await _transactionService.GetTransactionByIdAsync(id, tenantId);

        if (transaction == null)
        {
            return NotFound();
        }

        return transaction;
    }

    // PUT: api/Transaction/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTransaction(string id, Transaction transaction)
    {
        if (id != transaction.Id)
        {
            return BadRequest();
        }

        var tenantId = _tenantProvider.GetTenantId();
        var updatedTransaction = await _transactionService.UpdateTransactionAsync(id, transaction, tenantId);

        if (updatedTransaction == null)
        {
            return NotFound();
        }

        return NoContent();
    }

    // POST: api/Transaction
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Transaction>> PostTransaction(Transaction transaction)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var createdTransaction = await _transactionService.CreateTransactionAsync(transaction, tenantId);

        return CreatedAtAction("GetTransaction", new { id = createdTransaction.Id }, createdTransaction);
    }

    // DELETE: api/Transaction/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(string id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var deleted = await _transactionService.DeleteTransactionAsync(id, tenantId);
        
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
