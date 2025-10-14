using System.Text;
using System.Text.Json;
using OllamaSharp;

namespace missedpay.ApiService.Services;

public interface IAiCategorizationService
{
    Task<(string CategoryId, string CategoryName, string GroupName, double Confidence)?> CategorizeTransactionAsync(
        string merchantName, 
        string description, 
        decimal amount);
}

public class AiCategorizationService : IAiCategorizationService
{
    private readonly AiCategorySelector _aiSelector;

    public AiCategorizationService(AiCategorySelector aiSelector)
    {
        _aiSelector = aiSelector;
    }

    public async Task<(string CategoryId, string CategoryName, string GroupName, double Confidence)?> CategorizeTransactionAsync(
        string merchantName, 
        string description, 
        decimal amount)
    {
        var categories = MerchantCategorizationService.LoadCategories();
        var result = await _aiSelector.SelectCategoryAsync(merchantName, description, amount, categories);
        
        if (result == null)
        {
            return null;
        }

        return (result.CategoryId, result.CategoryName, result.GroupName, result.Confidence);
    }
}
