using System.Text.Json;
using System.Text.Json.Serialization;
using missedpay.ApiService.Models;

namespace missedpay.ApiService.Services;

public class AkahuService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AkahuService> _logger;

    public AkahuService(HttpClient httpClient, IConfiguration configuration, ILogger<AkahuService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        
        // Configure base address
        _httpClient.BaseAddress = new Uri("https://api.akahu.io/v1/");
    }

    private void ConfigureHeaders()
    {
        var userToken = _configuration["Akahu:UserToken"];
        var appToken = _configuration["Akahu:AppToken"];

        if (string.IsNullOrEmpty(userToken) || string.IsNullOrEmpty(appToken))
        {
            throw new InvalidOperationException("Akahu tokens are not configured. Please set AKAHU_USER_TOKEN and AKAHU_APP_TOKEN in your .env file or appsettings.json");
        }

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {userToken}");
        _httpClient.DefaultRequestHeaders.Add("X-Akahu-Id", appToken);
    }

    public async Task<List<Account>> GetAccountsAsync()
    {
        ConfigureHeaders();
        
        try
        {
            var response = await _httpClient.GetAsync("accounts");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var akahuResponse = JsonSerializer.Deserialize<AkahuAccountsResponse>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (akahuResponse?.Items == null)
            {
                _logger.LogWarning("No accounts returned from Akahu API");
                return new List<Account>();
            }

            // Map Akahu accounts to our Account model
            var accounts = akahuResponse.Items.Select(MapAkahuAccount).ToList();
            
            _logger.LogInformation($"Retrieved {accounts.Count} accounts from Akahu");
            return accounts;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling Akahu accounts API");
            throw new Exception("Failed to retrieve accounts from Akahu", ex);
        }
    }

    public async Task<List<Transaction>> GetTransactionsAsync()
    {
        ConfigureHeaders();
        
        try
        {
            var response = await _httpClient.GetAsync("transactions");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var akahuResponse = JsonSerializer.Deserialize<AkahuTransactionsResponse>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (akahuResponse?.Items == null)
            {
                _logger.LogWarning("No transactions returned from Akahu API");
                return new List<Transaction>();
            }

            // Map Akahu transactions to our Transaction model
            var transactions = akahuResponse.Items.Select(MapAkahuTransaction).ToList();
            
            _logger.LogInformation($"Retrieved {transactions.Count} transactions from Akahu");
            return transactions;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling Akahu transactions API");
            throw new Exception("Failed to retrieve transactions from Akahu", ex);
        }
    }

    private Account MapAkahuAccount(AkahuAccount akahuAccount)
    {
        return new Account
        {
            Id = akahuAccount.Id,
            Authorisation = akahuAccount.Connection?.Id ?? string.Empty,
            Connection = akahuAccount.Connection != null ? new Connection
            {
                Id = akahuAccount.Connection.Id,
                Name = akahuAccount.Connection.Name,
                Logo = akahuAccount.Connection.Logo ?? string.Empty,
                ConnectionType = ParseConnectionType(akahuAccount.Connection.ConnectionType)
            } : new Connection(),
            Name = akahuAccount.Name,
            Status = ParseAccountStatus(akahuAccount.Status),
            FormattedAccount = akahuAccount.FormattedAccount,
            Balance = akahuAccount.Balance != null ? new Balance
            {
                Current = akahuAccount.Balance.Current,
                Available = akahuAccount.Balance.Available,
                Currency = akahuAccount.Balance.Currency ?? "NZD"
            } : new Balance(),
            Type = ParseAccountType(akahuAccount.Type),
            Attributes = ParseAccountAttributes(akahuAccount.Attributes)
        };
    }

    private Transaction MapAkahuTransaction(AkahuTransaction akahuTransaction)
    {
        return new Transaction
        {
            Id = akahuTransaction.Id,
            AccountId = akahuTransaction.Account,
            ConnectionId = akahuTransaction.Connection ?? string.Empty,
            CreatedAt = akahuTransaction.CreatedAt,
            Date = akahuTransaction.Date,
            Description = akahuTransaction.Description,
            Amount = akahuTransaction.Amount,
            Balance = akahuTransaction.Balance,
            Type = ParseTransactionType(akahuTransaction.Type),
            Category = akahuTransaction.Category != null ? new TransactionCategory
            {
                Id = akahuTransaction.Category.Id,
                Name = akahuTransaction.Category.Name,
                Groups = new Dictionary<string, CategoryGroup>()
            } : null,
            Merchant = akahuTransaction.Merchant != null ? new Merchant
            {
                Id = akahuTransaction.Merchant.Id,
                Name = akahuTransaction.Merchant.Name,
                Website = akahuTransaction.Merchant.Website
            } : null,
            Meta = akahuTransaction.Meta != null ? new TransactionMeta
            {
                Logo = akahuTransaction.Meta.Logo,
                Particulars = akahuTransaction.Meta.Particulars,
                Code = akahuTransaction.Meta.Code,
                Reference = akahuTransaction.Meta.Reference,
                OtherAccount = akahuTransaction.Meta.OtherAccount,
                CardSuffix = akahuTransaction.Meta.CardSuffix
            } : null
        };
    }

    private ConnectionType ParseConnectionType(string? type)
    {
        return type?.ToUpper() switch
        {
            "CLASSIC" => ConnectionType.Classic,
            "OFFICIAL" => ConnectionType.Official,
            _ => ConnectionType.Classic
        };
    }

    private List<AccountAttribute> ParseAccountAttributes(List<string>? attributes)
    {
        if (attributes == null || attributes.Count == 0)
            return new List<AccountAttribute>();

        return attributes
            .Select(attr => attr.ToUpper() switch
            {
                "TRANSACTIONS" => AccountAttribute.Transactions,
                "TRANSFER_TO" => AccountAttribute.TransferTo,
                "TRANSFER_FROM" => AccountAttribute.TransferFrom,
                "PAYMENT_TO" => AccountAttribute.PaymentTo,
                "PAYMENT_FROM" => AccountAttribute.PaymentFrom,
                _ => (AccountAttribute?)null
            })
            .Where(attr => attr.HasValue)
            .Select(attr => attr!.Value)
            .ToList();
    }

    private AccountStatus ParseAccountStatus(string? status)
    {
        return status?.ToUpper() switch
        {
            "ACTIVE" => AccountStatus.Active,
            "INACTIVE" => AccountStatus.Inactive,
            _ => AccountStatus.Active
        };
    }

    private AccountType ParseAccountType(string? type)
    {
        return type?.ToUpper() switch
        {
            "CHECKING" => AccountType.Checking,
            "SAVINGS" => AccountType.Savings,
            "CREDITCARD" => AccountType.CreditCard,
            "LOAN" => AccountType.Loan,
            _ => AccountType.Checking
        };
    }

    private TransactionType ParseTransactionType(string? type)
    {
        return type?.ToUpper() switch
        {
            "PAYMENT" => TransactionType.Payment,
            "TRANSFER" => TransactionType.Transfer,
            "EFTPOS" => TransactionType.Eftpos,
            "CREDIT" => TransactionType.Credit,
            "DEBIT" => TransactionType.Debit,
            "INTEREST" => TransactionType.Interest,
            "FEE" => TransactionType.Fee,
            "DIRECTDEBIT" => TransactionType.DirectDebit,
            "DIRECTCREDIT" => TransactionType.DirectCredit,
            "ATM" => TransactionType.Atm,
            "STANDINGORDER" => TransactionType.StandingOrder,
            _ => TransactionType.Payment
        };
    }
}

// Akahu API Response Models
public class AkahuAccountsResponse
{
    [JsonPropertyName("items")]
    public List<AkahuAccount> Items { get; set; } = new();
}

public class AkahuAccount
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("connection")]
    public AkahuConnection? Connection { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    
    [JsonPropertyName("formatted_account")]
    public string? FormattedAccount { get; set; }
    
    [JsonPropertyName("balance")]
    public AkahuBalance? Balance { get; set; }
    
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("attributes")]
    public List<string>? Attributes { get; set; }
}

public class AkahuConnection
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("logo")]
    public string? Logo { get; set; }
    
    [JsonPropertyName("connection_type")]
    public string? ConnectionType { get; set; }
}

public class AkahuBalance
{
    [JsonPropertyName("current")]
    public decimal Current { get; set; }
    
    [JsonPropertyName("available")]
    public decimal? Available { get; set; }
    
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
}

public class AkahuTransactionsResponse
{
    [JsonPropertyName("items")]
    public List<AkahuTransaction> Items { get; set; } = new();
}

public class AkahuTransaction
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("_account")]
    public string Account { get; set; } = string.Empty;
    
    [JsonPropertyName("_connection")]
    public string? Connection { get; set; }
    
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
    public string? Type { get; set; }
    
    [JsonPropertyName("category")]
    public AkahuCategory? Category { get; set; }
    
    [JsonPropertyName("merchant")]
    public AkahuMerchant? Merchant { get; set; }
    
    [JsonPropertyName("meta")]
    public AkahuTransactionMeta? Meta { get; set; }
}

public class AkahuCategory
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("groups")]
    public AkahuCategoryGroups? Groups { get; set; }
}

public class AkahuCategoryGroups
{
    [JsonPropertyName("personal_finance")]
    public AkahuCategoryGroup? PersonalFinance { get; set; }
}

public class AkahuCategoryGroup
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class AkahuMerchant
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("website")]
    public string? Website { get; set; }
}

public class AkahuTransactionMeta
{
    [JsonPropertyName("logo")]
    public string? Logo { get; set; }
    
    [JsonPropertyName("particulars")]
    public string? Particulars { get; set; }
    
    [JsonPropertyName("code")]
    public string? Code { get; set; }
    
    [JsonPropertyName("reference")]
    public string? Reference { get; set; }
    
    [JsonPropertyName("other_account")]
    public string? OtherAccount { get; set; }
    
    [JsonPropertyName("card_suffix")]
    public string? CardSuffix { get; set; }
}
