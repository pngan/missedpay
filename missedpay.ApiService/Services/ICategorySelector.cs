namespace missedpay.ApiService.Services;

/// <summary>
/// Strategy interface for different categorization methods
/// </summary>
public interface ICategorySelector
{
    /// <summary>
    /// The categorization method this selector implements
    /// </summary>
    CategorizationMethod Method { get; }

    /// <summary>
    /// Select a category for the given merchant
    /// </summary>
    Task<CategorizationResult?> SelectCategoryAsync(
        string merchantName,
        string? description,
        decimal? amount,
        List<CategoryMapping> availableCategories);

    /// <summary>
    /// Get multiple category suggestions (used by hybrid approach)
    /// </summary>
    Task<List<CategorySuggestion>> GetSuggestionsAsync(
        string merchantName,
        string? description,
        decimal? amount,
        List<CategoryMapping> availableCategories,
        int maxSuggestions = 6);
}
