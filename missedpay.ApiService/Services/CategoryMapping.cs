using System.Text.Json.Serialization;

namespace missedpay.ApiService.Services;

public class CategoryMapping
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("groups")]
    public CategoryGroups Groups { get; set; } = new();
}

public class CategoryGroups
{
    [JsonPropertyName("personal_finance")]
    public PersonalFinanceGroup PersonalFinance { get; set; } = new();
}

public class PersonalFinanceGroup
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
