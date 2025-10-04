using System.Text.Json.Serialization;

namespace missedpay.ApiService.Models;

public class Account
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("_migrated")]
    public string? Migrated { get; set; }

    [JsonPropertyName("_authorisation")]
    public string Authorisation { get; set; } = string.Empty;

    [JsonPropertyName("_credentials")]
    [Obsolete("Use Authorisation instead")]
    public string? Credentials { get; set; }

    [JsonPropertyName("connection")]
    public Connection Connection { get; set; } = new();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public AccountStatus Status { get; set; }

    [JsonPropertyName("formatted_account")]
    public string? FormattedAccount { get; set; }

    [JsonPropertyName("meta")]
    public AccountMeta Meta { get; set; } = new();

    [JsonPropertyName("refreshed")]
    public RefreshedTimestamps Refreshed { get; set; } = new();

    [JsonPropertyName("balance")]
    public Balance Balance { get; set; } = new();

    [JsonPropertyName("type")]
    public AccountType Type { get; set; }

    [JsonPropertyName("attributes")]
    public List<AccountAttribute> Attributes { get; set; } = new();
}

public class Connection
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("logo")]
    public string Logo { get; set; } = string.Empty;

    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("connection_type")]
    public ConnectionType ConnectionType { get; set; }
}

public enum ConnectionType
{
    [JsonPropertyName("classic")]
    Classic,

    [JsonPropertyName("official")]
    Official
}

public enum AccountStatus
{
    [JsonPropertyName("ACTIVE")]
    Active,

    [JsonPropertyName("INACTIVE")]
    Inactive
}

public class AccountMeta
{
    [JsonPropertyName("holder")]
    public string? Holder { get; set; }

    [JsonPropertyName("has_unlisted_holders")]
    public bool? HasUnlistedHolders { get; set; }

    [JsonPropertyName("payment_details")]
    public PaymentDetails? PaymentDetails { get; set; }

    [JsonPropertyName("loan_details")]
    public LoanDetails? LoanDetails { get; set; }

    [JsonPropertyName("breakdown")]
    public object? Breakdown { get; set; }

    [JsonPropertyName("portfolio")]
    public object? Portfolio { get; set; }
}

public class PaymentDetails
{
    [JsonPropertyName("account_holder")]
    public string AccountHolder { get; set; } = string.Empty;

    [JsonPropertyName("account_number")]
    public string AccountNumber { get; set; } = string.Empty;

    [JsonPropertyName("particulars")]
    public string? Particulars { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("minimum_amount")]
    public decimal? MinimumAmount { get; set; }
}

public class LoanDetails
{
    [JsonPropertyName("purpose")]
    public string? Purpose { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("interest")]
    public LoanInterest? Interest { get; set; }

    [JsonPropertyName("is_interest_only")]
    public bool? IsInterestOnly { get; set; }

    [JsonPropertyName("interest_only_expires_at")]
    public DateTime? InterestOnlyExpiresAt { get; set; }

    [JsonPropertyName("term")]
    public string? Term { get; set; }

    [JsonPropertyName("matures_at")]
    public DateTime? MaturesAt { get; set; }

    [JsonPropertyName("initial_principal")]
    public decimal? InitialPrincipal { get; set; }

    [JsonPropertyName("repayment")]
    public LoanRepayment? Repayment { get; set; }
}

public class LoanInterest
{
    [JsonPropertyName("rate")]
    public decimal Rate { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("expires_at")]
    public DateTime? ExpiresAt { get; set; }
}

public class LoanRepayment
{
    [JsonPropertyName("frequency")]
    public string Frequency { get; set; } = string.Empty;

    [JsonPropertyName("next_date")]
    public DateTime? NextDate { get; set; }

    [JsonPropertyName("next_amount")]
    public decimal? NextAmount { get; set; }
}

public class RefreshedTimestamps
{
    [JsonPropertyName("balance")]
    public DateTime? Balance { get; set; }

    [JsonPropertyName("meta")]
    public DateTime? Meta { get; set; }

    [JsonPropertyName("transactions")]
    public DateTime? Transactions { get; set; }

    [JsonPropertyName("party")]
    public DateTime? Party { get; set; }
}

public class Balance
{
    [JsonPropertyName("current")]
    public decimal Current { get; set; }

    [JsonPropertyName("available")]
    public decimal? Available { get; set; }

    [JsonPropertyName("limit")]
    public decimal? Limit { get; set; }

    [JsonPropertyName("overdrawn")]
    public bool? Overdrawn { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "NZD";
}

public enum AccountType
{
    [JsonPropertyName("CHECKING")]
    Checking,

    [JsonPropertyName("SAVINGS")]
    Savings,

    [JsonPropertyName("CREDITCARD")]
    CreditCard,

    [JsonPropertyName("LOAN")]
    Loan,

    [JsonPropertyName("KIWISAVER")]
    KiwiSaver,

    [JsonPropertyName("INVESTMENT")]
    Investment,

    [JsonPropertyName("TERMDEPOSIT")]
    TermDeposit,

    [JsonPropertyName("FOREIGN")]
    Foreign,

    [JsonPropertyName("TAX")]
    Tax,

    [JsonPropertyName("REWARDS")]
    Rewards,

    [JsonPropertyName("WALLET")]
    Wallet
}

public enum AccountAttribute
{
    [JsonPropertyName("TRANSACTIONS")]
    Transactions,

    [JsonPropertyName("TRANSFER_TO")]
    TransferTo,

    [JsonPropertyName("TRANSFER_FROM")]
    TransferFrom,

    [JsonPropertyName("PAYMENT_TO")]
    PaymentTo,

    [JsonPropertyName("PAYMENT_FROM")]
    PaymentFrom
}
