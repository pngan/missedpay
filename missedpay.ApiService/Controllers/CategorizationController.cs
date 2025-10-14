using Microsoft.AspNetCore.Mvc;
using missedpay.ApiService.Services;

namespace missedpay.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategorizationController : ControllerBase
{
    private readonly IMerchantCategorizationService _categorizationService;
    private readonly ILogger<CategorizationController> _logger;

    public CategorizationController(
        IMerchantCategorizationService categorizationService,
        ILogger<CategorizationController> logger)
    {
        _categorizationService = categorizationService;
        _logger = logger;
    }

    /// <summary>
    /// Categorize a merchant using AI
    /// </summary>
    [HttpPost("categorize")]
    public async Task<ActionResult<CategorizationResult>> CategorizeAi([FromBody] CategorizeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MerchantName))
        {
            return BadRequest("Merchant name is required");
        }

        var result = await _categorizationService.CategorizeAsync(
            request.MerchantName,
            request.Description,
            request.Amount,
            CategorizationMethod.AI);

        if (result == null)
        {
            return Problem(
                title: "Categorization failed",
                detail: "Unable to categorize the merchant. Please try again or use manual selection.",
                statusCode: 500);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get category suggestions for a merchant (for hybrid/manual selection)
    /// </summary>
    [HttpPost("suggestions")]
    public async Task<ActionResult<List<CategorySuggestion>>> GetSuggestions([FromBody] SuggestionsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MerchantName))
        {
            return BadRequest("Merchant name is required");
        }

        var suggestions = await _categorizationService.GetCategorySuggestionsAsync(
            request.MerchantName,
            request.Description,
            request.Amount,
            request.MaxSuggestions);

        return Ok(suggestions);
    }

    /// <summary>
    /// Confirm a category selection (after user picks from suggestions or manual list)
    /// </summary>
    [HttpPost("confirm")]
    public async Task<ActionResult> ConfirmCategorization([FromBody] ConfirmCategorizationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MerchantName))
        {
            return BadRequest("Merchant name is required");
        }

        if (string.IsNullOrWhiteSpace(request.CategoryId))
        {
            return BadRequest("Category ID is required");
        }

        try
        {
            await _categorizationService.ConfirmCategorizationAsync(
                request.MerchantName,
                request.CategoryId,
                request.Method);

            return Ok(new { message = "Category confirmed successfully" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get all available categories grouped by personal finance group
    /// </summary>
    [HttpGet("categories")]
    public ActionResult<Dictionary<string, List<CategoryMapping>>> GetAllCategories()
    {
        var categories = _categorizationService.GetAllCategoriesGrouped();
        return Ok(categories);
    }

    /// <summary>
    /// Get categories for a specific group
    /// </summary>
    [HttpGet("categories/{groupName}")]
    public ActionResult<List<CategoryMapping>> GetCategoriesByGroup(string groupName)
    {
        var allCategories = _categorizationService.GetAllCategoriesGrouped();
        
        if (!allCategories.TryGetValue(groupName, out var categories))
        {
            return NotFound($"Group '{groupName}' not found");
        }

        return Ok(categories);
    }
}

// Request/Response models
public record CategorizeRequest(
    string MerchantName,
    string? Description = null,
    decimal? Amount = null);

public record SuggestionsRequest(
    string MerchantName,
    string? Description = null,
    decimal? Amount = null,
    int MaxSuggestions = 6);

public record ConfirmCategorizationRequest(
    string MerchantName,
    string CategoryId,
    CategorizationMethod Method = CategorizationMethod.Manual);
