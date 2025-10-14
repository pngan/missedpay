using Microsoft.AspNetCore.Mvc;
using missedpay.ApiService.Models;
using missedpay.ApiService.Services;

[Route("api/[controller]")]
[ApiController]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IMerchantCategorizationService _categorizationService;
    
    public TransactionController(
        ITransactionService transactionService, 
        ITenantProvider tenantProvider,
        IMerchantCategorizationService categorizationService)
    {
        _transactionService = transactionService;
        _tenantProvider = tenantProvider;
        _categorizationService = categorizationService;
    }

    // GET: api/Transaction
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetTransaction()
    {
        var tenantId = _tenantProvider.GetTenantId();
        var transactions = await _transactionService.GetAllTransactionsAsync(tenantId);
        
        // Apply cached merchant categorizations to transactions
        transactions = ApplyCachedCategorizations(transactions);
        
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

    private List<Transaction> ApplyCachedCategorizations(List<Transaction> transactions)
    {
        foreach (var transaction in transactions)
        {
            // Try to get merchant name from transaction
            var merchantName = transaction.Merchant?.Name ?? transaction.Description;
            
            if (string.IsNullOrWhiteSpace(merchantName))
                continue;

            // Check if we have a cached categorization for this merchant
            var cached = _categorizationService.GetCachedCategorization(merchantName);
            
            if (cached != null)
            {
                // Apply the cached categorization to the transaction
                if (transaction.Category == null)
                {
                    transaction.Category = new TransactionCategory();
                }
                
                transaction.Category.Id = cached.CategoryId;
                transaction.Category.Name = cached.CategoryName;
                
                if (transaction.Category.Groups == null)
                {
                    transaction.Category.Groups = new Dictionary<string, CategoryGroup>();
                }
                
                transaction.Category.Groups["personal_finance"] = new CategoryGroup
                {
                    Id = cached.GroupId,
                    Name = cached.GroupName
                };
            }
        }
        
        return transactions;
    }
}
