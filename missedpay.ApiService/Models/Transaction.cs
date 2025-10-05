using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace missedpay.ApiService.Models;

public class Transaction : BaseEntity
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("_account")]
    public string AccountId { get; set; } = string.Empty;

    [JsonPropertyName("_connection")]
    public string ConnectionId { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("balance")]
    public decimal? Balance { get; set; }

    [JsonPropertyName("type")]
    public TransactionType Type { get; set; }

    // Enriched data (requires additional permissions)
    [JsonPropertyName("category")]
    public TransactionCategory? Category { get; set; }

    [JsonPropertyName("merchant")]
    public Merchant? Merchant { get; set; }

    [JsonPropertyName("meta")]
    public TransactionMeta? Meta { get; set; }
}

public enum TransactionType
{
    [JsonPropertyName("CREDIT")]
    Credit,

    [JsonPropertyName("DEBIT")]
    Debit,

    [JsonPropertyName("PAYMENT")]
    Payment,

    [JsonPropertyName("TRANSFER")]
    Transfer,

    [JsonPropertyName("STANDING ORDER")]
    StandingOrder,

    [JsonPropertyName("EFTPOS")]
    Eftpos,

    [JsonPropertyName("INTEREST")]
    Interest,

    [JsonPropertyName("FEE")]
    Fee,

    [JsonPropertyName("TAX")]
    Tax,

    [JsonPropertyName("CREDIT CARD")]
    CreditCard,

    [JsonPropertyName("DIRECT DEBIT")]
    DirectDebit,

    [JsonPropertyName("DIRECT CREDIT")]
    DirectCredit,

    [JsonPropertyName("ATM")]
    Atm,

    [JsonPropertyName("LOAN")]
    Loan
}

public class TransactionCategory
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("groups")]
    public Dictionary<string, CategoryGroup> Groups { get; set; } = new();
}

public class CategoryGroup
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class Merchant
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("website")]
    public string? Website { get; set; }
}

public class TransactionMeta
{
    [JsonPropertyName("particulars")]
    public string? Particulars { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("other_account")]
    public string? OtherAccount { get; set; }

    [JsonPropertyName("conversion")]
    public CurrencyConversion? Conversion { get; set; }

    [JsonPropertyName("card_suffix")]
    public string? CardSuffix { get; set; }

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }
}

public class CurrencyConversion
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("rate")]
    public decimal Rate { get; set; }
}
