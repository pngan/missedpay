namespace missedpay.ApiService.Services;

/// <summary>
/// Hybrid category selector that uses AI to generate suggestions for user selection
/// </summary>
public class HybridCategorySelector : ICategorySelector
{
    private readonly AiCategorySelector _aiSelector;
    private readonly ILogger<HybridCategorySelector> _logger;

    public CategorizationMethod Method => CategorizationMethod.Hybrid;

    public HybridCategorySelector(
        AiCategorySelector aiSelector,
        ILogger<HybridCategorySelector> logger)
    {
        _aiSelector = aiSelector;
        _logger = logger;
    }

    public async Task<CategorizationResult?> SelectCategoryAsync(
        string merchantName,
        string? description,
        decimal? amount,
        List<CategoryMapping> availableCategories)
    {
        // Hybrid doesn't auto-select, it requires user interaction
        // This method returns the top suggestion but marked as hybrid
        var suggestions = await GetSuggestionsAsync(merchantName, description, amount, availableCategories, 1);
        
        if (suggestions.Count == 0)
        {
            return null;
        }

        var topSuggestion = suggestions[0];
        return new CategorizationResult
        {
            CategoryId = topSuggestion.CategoryId,
            CategoryName = topSuggestion.CategoryName,
            GroupId = topSuggestion.GroupId,
            GroupName = topSuggestion.GroupName,
            Confidence = topSuggestion.Score,
            Method = CategorizationMethod.Hybrid
        };
    }

    public async Task<List<CategorySuggestion>> GetSuggestionsAsync(
        string merchantName,
        string? description,
        decimal? amount,
        List<CategoryMapping> availableCategories,
        int maxSuggestions = 6)
    {
        // Delegate to AI selector for generating suggestions
        return await _aiSelector.GetSuggestionsAsync(
            merchantName, 
            description, 
            amount, 
            availableCategories, 
            maxSuggestions);
    }
}
