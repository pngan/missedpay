using System.Text;
using System.Text.Json;
using OllamaSharp;

namespace missedpay.ApiService.Services;

/// <summary>
/// AI-based category selector using Ollama LLM
/// </summary>
public class AiCategorySelector : ICategorySelector
{
    private readonly IOllamaApiClient _ollama;
    private readonly ILogger<AiCategorySelector> _logger;

    public CategorizationMethod Method => CategorizationMethod.AI;

    public AiCategorySelector(
        IOllamaApiClient ollama,
        ILogger<AiCategorySelector> logger)
    {
        _ollama = ollama;
        _logger = logger;
    }

    public async Task<CategorizationResult?> SelectCategoryAsync(
        string merchantName,
        string? description,
        decimal? amount,
        List<CategoryMapping> availableCategories)
    {
        try
        {
            var prompt = BuildCategorizationPrompt(merchantName, description, amount, availableCategories);
            
            var chat = new Chat(_ollama, "llama3.1:8b");
            var responseBuilder = new StringBuilder();
            
            await foreach (var chunk in chat.SendAsync(prompt))
            {
                responseBuilder.Append(chunk);
            }
            
            var response = responseBuilder.ToString();
            _logger.LogInformation("AI Categorization Response for {Merchant}: {Response}", 
                merchantName, response);
            
            return ParseCategorizationResponse(response, availableCategories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error categorizing merchant {Merchant} with AI", merchantName);
            return null;
        }
    }

    public async Task<List<CategorySuggestion>> GetSuggestionsAsync(
        string merchantName,
        string? description,
        decimal? amount,
        List<CategoryMapping> availableCategories,
        int maxSuggestions = 6)
    {
        try
        {
            var prompt = BuildSuggestionsPrompt(merchantName, description, amount, availableCategories, maxSuggestions);
            
            var chat = new Chat(_ollama, "llama3.1:8b");
            var responseBuilder = new StringBuilder();
            
            await foreach (var chunk in chat.SendAsync(prompt))
            {
                responseBuilder.Append(chunk);
            }
            
            var response = responseBuilder.ToString();
            _logger.LogInformation("AI Suggestions Response for {Merchant}: {Response}", 
                merchantName, response);
            
            return ParseSuggestionsResponse(response, availableCategories, maxSuggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggestions for merchant {Merchant}", merchantName);
            return new List<CategorySuggestion>();
        }
    }

    private string BuildCategorizationPrompt(
        string merchantName,
        string? description,
        decimal? amount,
        List<CategoryMapping> availableCategories)
    {
        var groupedCategories = availableCategories
            .GroupBy(c => c.Groups.PersonalFinance.Name)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Select(c => c.Name).OrderBy(n => n).ToList());

        var categoriesJson = JsonSerializer.Serialize(groupedCategories, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });

        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("You are a financial transaction categorization expert for New Zealand.");
        promptBuilder.AppendLine("Analyze the following transaction and categorize it into ONE of the available categories.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"Merchant: {merchantName}");
        
        if (!string.IsNullOrWhiteSpace(description))
        {
            promptBuilder.AppendLine($"Description: {description}");
        }
        
        if (amount.HasValue)
        {
            promptBuilder.AppendLine($"Amount: ${amount.Value:F2}");
        }
        
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Available categories grouped by type:");
        promptBuilder.AppendLine(categoriesJson);
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("IMPORTANT: Respond ONLY with a JSON object in this exact format:");
        promptBuilder.AppendLine("{");
        promptBuilder.AppendLine("  \"category\": \"exact category name from the list\",");
        promptBuilder.AppendLine("  \"group\": \"group name that contains this category\",");
        promptBuilder.AppendLine("  \"confidence\": 0.95,");
        promptBuilder.AppendLine("  \"reason\": \"brief explanation\"");
        promptBuilder.AppendLine("}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Do NOT include any other text outside the JSON object.");

        return promptBuilder.ToString();
    }

    private string BuildSuggestionsPrompt(
        string merchantName,
        string? description,
        decimal? amount,
        List<CategoryMapping> availableCategories,
        int maxSuggestions)
    {
        var groupedCategories = availableCategories
            .GroupBy(c => c.Groups.PersonalFinance.Name)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Select(c => c.Name).OrderBy(n => n).ToList());

        var categoriesJson = JsonSerializer.Serialize(groupedCategories, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });

        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("You are a financial transaction categorization expert for New Zealand.");
        promptBuilder.AppendLine($"Analyze the following transaction and suggest the top {maxSuggestions} most likely categories.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"Merchant: {merchantName}");
        
        if (!string.IsNullOrWhiteSpace(description))
        {
            promptBuilder.AppendLine($"Description: {description}");
        }
        
        if (amount.HasValue)
        {
            promptBuilder.AppendLine($"Amount: ${amount.Value:F2}");
        }
        
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Available categories grouped by type:");
        promptBuilder.AppendLine(categoriesJson);
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"IMPORTANT: Respond ONLY with a JSON array of the top {maxSuggestions} suggestions in this exact format:");
        promptBuilder.AppendLine("[");
        promptBuilder.AppendLine("  {");
        promptBuilder.AppendLine("    \"category\": \"exact category name from the list\",");
        promptBuilder.AppendLine("    \"group\": \"group name that contains this category\",");
        promptBuilder.AppendLine("    \"score\": 0.95,");
        promptBuilder.AppendLine("    \"reason\": \"brief explanation\"");
        promptBuilder.AppendLine("  }");
        promptBuilder.AppendLine("]");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Order suggestions from most likely to least likely. Do NOT include any other text outside the JSON array.");

        return promptBuilder.ToString();
    }

    private CategorizationResult? ParseCategorizationResponse(string response, List<CategoryMapping> availableCategories)
    {
        try
        {
            // Extract JSON from response (might have extra text)
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            
            if (jsonStart == -1 || jsonEnd == -1)
            {
                _logger.LogWarning("No JSON object found in response");
                return null;
            }
            
            var jsonText = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            var aiResponse = JsonSerializer.Deserialize<AiCategorizationResponse>(jsonText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (aiResponse == null)
            {
                _logger.LogWarning("Failed to deserialize AI response");
                return null;
            }
            
            // Find the matching category
            var category = availableCategories.FirstOrDefault(c => 
                c.Name.Equals(aiResponse.Category, StringComparison.OrdinalIgnoreCase));
            
            if (category == null)
            {
                _logger.LogWarning("AI suggested category '{Category}' not found in available categories", 
                    aiResponse.Category);
                return null;
            }
            
            return new CategorizationResult
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                GroupId = category.Groups.PersonalFinance.Id,
                GroupName = category.Groups.PersonalFinance.Name,
                Confidence = aiResponse.Confidence,
                Method = CategorizationMethod.AI
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing AI categorization response: {Response}", response);
            return null;
        }
    }

    private List<CategorySuggestion> ParseSuggestionsResponse(
        string response, 
        List<CategoryMapping> availableCategories,
        int maxSuggestions)
    {
        try
        {
            // Extract JSON array from response
            var jsonStart = response.IndexOf('[');
            var jsonEnd = response.LastIndexOf(']');
            
            if (jsonStart == -1 || jsonEnd == -1)
            {
                _logger.LogWarning("No JSON array found in suggestions response");
                return new List<CategorySuggestion>();
            }
            
            var jsonText = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            var aiSuggestions = JsonSerializer.Deserialize<List<AiSuggestionResponse>>(jsonText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (aiSuggestions == null)
            {
                _logger.LogWarning("Failed to deserialize AI suggestions");
                return new List<CategorySuggestion>();
            }
            
            var suggestions = new List<CategorySuggestion>();
            
            foreach (var aiSuggestion in aiSuggestions.Take(maxSuggestions))
            {
                var category = availableCategories.FirstOrDefault(c => 
                    c.Name.Equals(aiSuggestion.Category, StringComparison.OrdinalIgnoreCase));
                
                if (category != null)
                {
                    suggestions.Add(new CategorySuggestion
                    {
                        CategoryId = category.Id,
                        CategoryName = category.Name,
                        GroupId = category.Groups.PersonalFinance.Id,
                        GroupName = category.Groups.PersonalFinance.Name,
                        Score = aiSuggestion.Score,
                        Reason = aiSuggestion.Reason
                    });
                }
                else
                {
                    _logger.LogWarning("AI suggested category '{Category}' not found", aiSuggestion.Category);
                }
            }
            
            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing AI suggestions response: {Response}", response);
            return new List<CategorySuggestion>();
        }
    }

    private class AiCategorizationResponse
    {
        public string Category { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    private class AiSuggestionResponse
    {
        public string Category { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public double Score { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
