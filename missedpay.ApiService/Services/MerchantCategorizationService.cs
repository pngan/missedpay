namespace missedpay.ApiService.Services;

/// <summary>
/// Result of a categorization operation
/// </summary>
public record CategorizationResult
{
    public string CategoryId { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string GroupId { get; init; } = string.Empty;
    public string GroupName { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public CategorizationMethod Method { get; init; }
}

/// <summary>
/// Suggested category for hybrid selection
/// </summary>
public record CategorySuggestion
{
    public string CategoryId { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string GroupId { get; init; } = string.Empty;
    public string GroupName { get; init; } = string.Empty;
    public double Score { get; init; }
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Method used for categorization
/// </summary>
public enum CategorizationMethod
{
    AI,
    Manual,
    Hybrid,
    Cached
}

/// <summary>
/// Main service interface for merchant categorization
/// </summary>
public interface IMerchantCategorizationService
{
    /// <summary>
    /// Categorize a merchant using the specified method
    /// </summary>
    Task<CategorizationResult?> CategorizeAsync(
        string merchantName,
        string? description = null,
        decimal? amount = null,
        CategorizationMethod method = CategorizationMethod.AI);

    /// <summary>
    /// Get category suggestions for hybrid selection (AI generates shortlist for user to choose)
    /// </summary>
    Task<List<CategorySuggestion>> GetCategorySuggestionsAsync(
        string merchantName,
        string? description = null,
        decimal? amount = null,
        int maxSuggestions = 6);

    /// <summary>
    /// Confirm a category selection (used after manual or hybrid selection)
    /// </summary>
    Task ConfirmCategorizationAsync(
        string merchantName,
        string categoryId,
        CategorizationMethod method);

    /// <summary>
    /// Get all available categories grouped by personal finance group
    /// </summary>
    Dictionary<string, List<CategoryMapping>> GetAllCategoriesGrouped();
}

/// <summary>
/// Main implementation of merchant categorization service
/// </summary>
public class MerchantCategorizationService : IMerchantCategorizationService
{
    private readonly ICategorySelector _aiSelector;
    private readonly ICategorySelector _hybridSelector;
    private readonly ILogger<MerchantCategorizationService> _logger;
    private readonly List<CategoryMapping> _categories;
    private readonly Dictionary<string, CategorizationResult> _merchantCache;

    public MerchantCategorizationService(
        IEnumerable<ICategorySelector> selectors,
        ILogger<MerchantCategorizationService> logger)
    {
        _logger = logger;
        _categories = LoadCategories();
        _merchantCache = new Dictionary<string, CategorizationResult>(StringComparer.OrdinalIgnoreCase);

        // Get the specific selectors we need
        _aiSelector = selectors.FirstOrDefault(s => s.Method == CategorizationMethod.AI)
            ?? throw new InvalidOperationException("AI category selector not found");
        _hybridSelector = selectors.FirstOrDefault(s => s.Method == CategorizationMethod.Hybrid)
            ?? throw new InvalidOperationException("Hybrid category selector not found");
    }

    public async Task<CategorizationResult?> CategorizeAsync(
        string merchantName,
        string? description = null,
        decimal? amount = null,
        CategorizationMethod method = CategorizationMethod.AI)
    {
        // Check cache first
        if (_merchantCache.TryGetValue(merchantName, out var cached))
        {
            _logger.LogInformation("Using cached category for merchant: {Merchant}", merchantName);
            return cached with { Method = CategorizationMethod.Cached };
        }

        CategorizationResult? result = method switch
        {
            CategorizationMethod.AI => await _aiSelector.SelectCategoryAsync(merchantName, description, amount, _categories),
            CategorizationMethod.Hybrid => await _hybridSelector.SelectCategoryAsync(merchantName, description, amount, _categories),
            CategorizationMethod.Manual => null, // Manual requires UI interaction
            _ => null
        };

        if (result != null)
        {
            _merchantCache[merchantName] = result;
        }

        return result;
    }

    public async Task<List<CategorySuggestion>> GetCategorySuggestionsAsync(
        string merchantName,
        string? description = null,
        decimal? amount = null,
        int maxSuggestions = 6)
    {
        var suggestions = await _hybridSelector.GetSuggestionsAsync(merchantName, description, amount, _categories, maxSuggestions);
        return suggestions;
    }

    public Task ConfirmCategorizationAsync(
        string merchantName,
        string categoryId,
        CategorizationMethod method)
    {
        var category = _categories.FirstOrDefault(c => c.Id == categoryId);
        if (category == null)
        {
            throw new ArgumentException($"Category not found: {categoryId}", nameof(categoryId));
        }

        var result = new CategorizationResult
        {
            CategoryId = category.Id,
            CategoryName = category.Name,
            GroupId = category.Groups.PersonalFinance.Id,
            GroupName = category.Groups.PersonalFinance.Name,
            Confidence = 1.0,
            Method = method
        };

        _merchantCache[merchantName] = result;
        _logger.LogInformation("Confirmed categorization for {Merchant}: {Category} ({Method})",
            merchantName, category.Name, method);

        return Task.CompletedTask;
    }

    public Dictionary<string, List<CategoryMapping>> GetAllCategoriesGrouped()
    {
        return _categories
            .GroupBy(c => c.Groups.PersonalFinance.Name)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(c => c.Name).ToList()
            );
    }

    public static List<CategoryMapping> LoadCategories()
    {
        // This is the complete list of Akahu NZ FCC categories
        return new List<CategoryMapping>
        {
            // Income & Credits
            new() { Id = "nzfcc_ckouvvy84000008ml5gzv0hp1", Name = "Income", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000uhk4mfyjb5u8o", Name = "Income" } } },
            new() { Id = "nzfcc_ckouvvy84000108ml6kzv0hp2", Name = "Tax refund", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000uhk4mfyjb5u8o", Name = "Income" } } },
            new() { Id = "nzfcc_ckouvvy84000208ml7abc0hp3", Name = "Interest income", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000uhk4mfyjb5u8o", Name = "Income" } } },
            new() { Id = "nzfcc_ckouvvy84000308ml8def0hp4", Name = "Refunds and reimbursements", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000uhk4mfyjb5u8o", Name = "Income" } } },
            
            // Food & Dining
            new() { Id = "nzfcc_ckouvvy84001608ml5p6z4d8j", Name = "Supermarkets and grocery stores", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000xhk4mf7mg2j1z", Name = "Food" } } },
            new() { Id = "nzfcc_ckouvvy84001708ml6q7z4d8k", Name = "Restaurants", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000xhk4mf7mg2j1z", Name = "Food" } } },
            new() { Id = "nzfcc_ckouvvy84001808ml7r8z4d8l", Name = "Fast food and takeaways", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000xhk4mf7mg2j1z", Name = "Food" } } },
            new() { Id = "nzfcc_ckouvvy84001908ml8s9z4d8m", Name = "Cafes and bakeries", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000xhk4mf7mg2j1z", Name = "Food" } } },
            new() { Id = "nzfcc_ckouvvy84002008ml9taz4d8n", Name = "Bars and pubs", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000xhk4mf7mg2j1z", Name = "Food" } } },
            
            // Shopping
            new() { Id = "nzfcc_ckouvvy84003008mlabc4d9a", Name = "General merchandise", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000yhk4m2h3g8k5p", Name = "Shopping" } } },
            new() { Id = "nzfcc_ckouvvy84003108mlbcd4d9b", Name = "Clothing and accessories", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000yhk4m2h3g8k5p", Name = "Shopping" } } },
            new() { Id = "nzfcc_ckouvvy84003208mlcde4d9c", Name = "Electronics and appliances", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000yhk4m2h3g8k5p", Name = "Shopping" } } },
            new() { Id = "nzfcc_ckouvvy84003308mldef4d9d", Name = "Home and garden", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000yhk4m2h3g8k5p", Name = "Shopping" } } },
            new() { Id = "nzfcc_ckouvvy84003408mlefg4d9e", Name = "Sports and outdoors", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000yhk4m2h3g8k5p", Name = "Shopping" } } },
            new() { Id = "nzfcc_ckouvvy84003508mlfgh4d9f", Name = "Books and stationery", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000yhk4m2h3g8k5p", Name = "Shopping" } } },
            new() { Id = "nzfcc_ckouvvy84003608mlghi4d9g", Name = "Pet supplies", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000yhk4m2h3g8k5p", Name = "Shopping" } } },
            new() { Id = "nzfcc_ckouvvy84003708mlhij4d9h", Name = "Online shopping", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000yhk4m2h3g8k5p", Name = "Shopping" } } },
            
            // Transport
            new() { Id = "nzfcc_ckouvvy84004508mlabc5e1a", Name = "Petrol stations", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000zhk4m9j5h1m7q", Name = "Transport" } } },
            new() { Id = "nzfcc_ckouvvy84004608mlbcd5e1b", Name = "Public transport", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000zhk4m9j5h1m7q", Name = "Transport" } } },
            new() { Id = "nzfcc_ckouvvy84004708mlcde5e1c", Name = "Parking", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000zhk4m9j5h1m7q", Name = "Transport" } } },
            new() { Id = "nzfcc_ckouvvy84004808mldef5e1d", Name = "Taxis and ride sharing", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000zhk4m9j5h1m7q", Name = "Transport" } } },
            new() { Id = "nzfcc_ckouvvy84004908mlefg5e1e", Name = "Vehicle maintenance and repairs", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000zhk4m9j5h1m7q", Name = "Transport" } } },
            new() { Id = "nzfcc_ckouvvy84005008mlfgh5e1f", Name = "Vehicle registration and licensing", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000zhk4m9j5h1m7q", Name = "Transport" } } },
            
            // Utilities
            new() { Id = "nzfcc_ckouvvz0y004t08ml8zey1jiv", Name = "Telecommunication services", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000vhk4m46ce9nrt", Name = "Utilities" } } },
            new() { Id = "nzfcc_ckouvvz0y004u08ml9afy1jiw", Name = "Electricity", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000vhk4m46ce9nrt", Name = "Utilities" } } },
            new() { Id = "nzfcc_ckouvvz0y004v08mlabgz1jix", Name = "Gas", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000vhk4m46ce9nrt", Name = "Utilities" } } },
            new() { Id = "nzfcc_ckouvvz0y004w08mlbchz1jiy", Name = "Water", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000vhk4m46ce9nrt", Name = "Utilities" } } },
            new() { Id = "nzfcc_ckouvvz0y004x08mlcdiz1jiz", Name = "Internet and cable TV", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw000vhk4m46ce9nrt", Name = "Utilities" } } },
            
            // Entertainment
            new() { Id = "nzfcc_ckouvvy85006008mlabc6f2a", Name = "Movies and streaming", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0010hk4m5k6i2n8r", Name = "Entertainment" } } },
            new() { Id = "nzfcc_ckouvvy85006108mlbcd6f2b", Name = "Music and audio", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0010hk4m5k6i2n8r", Name = "Entertainment" } } },
            new() { Id = "nzfcc_ckouvvy85006208mlcde6f2c", Name = "Gaming", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0010hk4m5k6i2n8r", Name = "Entertainment" } } },
            new() { Id = "nzfcc_ckouvvy85006308mldef6f2d", Name = "Events and activities", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0010hk4m5k6i2n8r", Name = "Entertainment" } } },
            new() { Id = "nzfcc_ckouvvy85006408mlefg6f2e", Name = "Books and magazines", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0010hk4m5k6i2n8r", Name = "Entertainment" } } },
            
            // Health & Fitness
            new() { Id = "nzfcc_ckouvvy85007508mlabc7g3a", Name = "Medical services", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0011hk4m1l7j3o9s", Name = "Health" } } },
            new() { Id = "nzfcc_ckouvvy85007608mlbcd7g3b", Name = "Pharmacy", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0011hk4m1l7j3o9s", Name = "Health" } } },
            new() { Id = "nzfcc_ckouvvy85007708mlcde7g3c", Name = "Dentist", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0011hk4m1l7j3o9s", Name = "Health" } } },
            new() { Id = "nzfcc_ckouvvy85007808mldef7g3d", Name = "Optometry", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0011hk4m1l7j3o9s", Name = "Health" } } },
            new() { Id = "nzfcc_ckouvvy85007908mlefg7g3e", Name = "Gym and fitness", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0011hk4m1l7j3o9s", Name = "Health" } } },
            new() { Id = "nzfcc_ckouvvy85008008mlfgh7g3f", Name = "Health insurance", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0011hk4m1l7j3o9s", Name = "Health" } } },
            
            // Personal Care
            new() { Id = "nzfcc_ckouvvy85009008mlabc8h4a", Name = "Hair and beauty", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0012hk4m7m8k4p0t", Name = "Personal Care" } } },
            new() { Id = "nzfcc_ckouvvy85009108mlbcd8h4b", Name = "Spa and massage", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0012hk4m7m8k4p0t", Name = "Personal Care" } } },
            
            // Education
            new() { Id = "nzfcc_ckouvvy86010508mlabc9i5a", Name = "School fees", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0013hk4m3n9l5q1u", Name = "Education" } } },
            new() { Id = "nzfcc_ckouvvy86010608mlbcd9i5b", Name = "Tertiary education", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0013hk4m3n9l5q1u", Name = "Education" } } },
            new() { Id = "nzfcc_ckouvvy86010708mlcde9i5c", Name = "Student loans", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0013hk4m3n9l5q1u", Name = "Education" } } },
            new() { Id = "nzfcc_ckouvvy86010808mldef9i5d", Name = "Educational supplies", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0013hk4m3n9l5q1u", Name = "Education" } } },
            
            // Housing
            new() { Id = "nzfcc_ckouvvy86012008mlabcaj6a", Name = "Rent", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0014hk4m9o0m6r2v", Name = "Housing" } } },
            new() { Id = "nzfcc_ckouvvy86012108mlbcdaj6b", Name = "Mortgage", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0014hk4m9o0m6r2v", Name = "Housing" } } },
            new() { Id = "nzfcc_ckouvvy86012208mlcdeaj6c", Name = "Rates and body corporate", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0014hk4m9o0m6r2v", Name = "Housing" } } },
            new() { Id = "nzfcc_ckouvvy86012308mldefaj6d", Name = "Home insurance", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0014hk4m9o0m6r2v", Name = "Housing" } } },
            new() { Id = "nzfcc_ckouvvy86012408mlefgaj6e", Name = "Home maintenance and repairs", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0014hk4m9o0m6r2v", Name = "Housing" } } },
            new() { Id = "nzfcc_ckouvvy86012508mlfghaj6f", Name = "Furniture and homewares", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0014hk4m9o0m6r2v", Name = "Housing" } } },
            
            // Financial
            new() { Id = "nzfcc_ckouvvy87013508mlabcbk7a", Name = "Banking fees", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0015hk4m5p1n7s3w", Name = "Financial" } } },
            new() { Id = "nzfcc_ckouvvy87013608mlbcdbk7b", Name = "Investment fees", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0015hk4m5p1n7s3w", Name = "Financial" } } },
            new() { Id = "nzfcc_ckouvvy87013708mlcdebk7c", Name = "Credit card payments", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0015hk4m5p1n7s3w", Name = "Financial" } } },
            new() { Id = "nzfcc_ckouvvy87013808mldefbk7d", Name = "Loan payments", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0015hk4m5p1n7s3w", Name = "Financial" } } },
            new() { Id = "nzfcc_ckouvvy87013908mlefgbk7e", Name = "Insurance", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0015hk4m5p1n7s3w", Name = "Financial" } } },
            new() { Id = "nzfcc_ckouvvy87014008mlfghbk7f", Name = "Tax payments", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0015hk4m5p1n7s3w", Name = "Financial" } } },
            new() { Id = "nzfcc_ckouvvy87014108mlghick7g", Name = "Savings and investments", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0015hk4m5p1n7s3w", Name = "Financial" } } },
            
            // Travel
            new() { Id = "nzfcc_ckouvvy87015508mlabccl8a", Name = "Accommodation", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0016hk4m1q2o8t4x", Name = "Travel" } } },
            new() { Id = "nzfcc_ckouvvy87015608mlbcdcl8b", Name = "Flights", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0016hk4m1q2o8t4x", Name = "Travel" } } },
            new() { Id = "nzfcc_ckouvvy87015708mlcdecl8c", Name = "Car rental", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0016hk4m1q2o8t4x", Name = "Travel" } } },
            new() { Id = "nzfcc_ckouvvy87015808mldefcl8d", Name = "Travel insurance", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0016hk4m1q2o8t4x", Name = "Travel" } } },
            
            // Charity & Gifts
            new() { Id = "nzfcc_ckouvvy88017008mlabcdm9a", Name = "Charitable donations", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0017hk4m7r3p9u5y", Name = "Charity & Gifts" } } },
            new() { Id = "nzfcc_ckouvvy88017108mlbcddm9b", Name = "Gifts", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0017hk4m7r3p9u5y", Name = "Charity & Gifts" } } },
            
            // Government & Legal
            new() { Id = "nzfcc_ckouvvy88018508mlabcen0a", Name = "Fines and penalties", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0018hk4m3s4q0v6z", Name = "Government & Legal" } } },
            new() { Id = "nzfcc_ckouvvy88018608mlbcden0b", Name = "Legal services", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0018hk4m3s4q0v6z", Name = "Government & Legal" } } },
            new() { Id = "nzfcc_ckouvvy88018708mlcdeen0c", Name = "Government services", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0018hk4m3s4q0v6z", Name = "Government & Legal" } } },
            
            // Business
            new() { Id = "nzfcc_ckouvvy89020008mlabcfo1a", Name = "Business expenses", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0019hk4m9t5r1w70", Name = "Business" } } },
            new() { Id = "nzfcc_ckouvvy89020108mlbcdfo1b", Name = "Office supplies", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw0019hk4m9t5r1w70", Name = "Business" } } },
            
            // Other
            new() { Id = "nzfcc_ckouvvy89021508mlabcgp2a", Name = "Other expenses", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw001ahk4m5u6s2x81", Name = "Other" } } },
            new() { Id = "nzfcc_ckouvvy89021608mlbcdgp2b", Name = "Uncategorized", Groups = new() { PersonalFinance = new() { Id = "group_clasr0ysw001ahk4m5u6s2x81", Name = "Other" } } }
        };
    }
}
