using System.Text.Json;
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
            Authorisation = akahuAccount.Connection?.Id,
            Connection = akahuAccount.Connection != null ? new Connection
            {
                Id = akahuAccount.Connection.Id,
                Name = akahuAccount.Connection.Name,
                Logo = akahuAccount.Connection.Logo,
                ConnectionType = akahuAccount.Connection.ConnectionType ?? "Classic"
            } : null,
            Name = akahuAccount.Name,
            Status = ParseAccountStatus(akahuAccount.Status),
            FormattedAccount = akahuAccount.FormattedAccount,
            Balance = akahuAccount.Balance != null ? new Balance
            {
                Current = akahuAccount.Balance.Current,
                Available = akahuAccount.Balance.Available,
                Currency = akahuAccount.Balance.Currency ?? "NZD"
            } : null,
            Type = ParseAccountType(akahuAccount.Type),
            Attributes = akahuAccount.Attributes ?? new List<string>()
        };
    }

    private Transaction MapAkahuTransaction(AkahuTransaction akahuTransaction)
    {
        return new Transaction
        {
            Id = akahuTransaction.Id,
            Account = akahuTransaction.Account,
            Connection = akahuTransaction.Connection,
            CreatedAt = akahuTransaction.CreatedAt,
            Date = akahuTransaction.Date,
            Description = akahuTransaction.Description,
            Amount = akahuTransaction.Amount,
            Balance = akahuTransaction.Balance,
            Type = ParseTransactionType(akahuTransaction.Type),
            Category = akahuTransaction.Category != null ? new Category
            {
                Id = akahuTransaction.Category.Id,
                Name = akahuTransaction.Category.Name,
                Groups = akahuTransaction.Category.Groups
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

    private AccountStatus ParseAccountStatus(string? status)
    {
        return status?.ToUpper() switch
        {
            "ACTIVE" => AccountStatus.Active,
            "INACTIVE" => AccountStatus.Inactive,
            "CLOSED" => AccountStatus.Closed,
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
    public List<AkahuAccount> Items { get; set; } = new();
}

public class AkahuAccount
{
    public string Id { get; set; } = string.Empty;
    public AkahuConnection? Connection { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? FormattedAccount { get; set; }
    public AkahuBalance? Balance { get; set; }
    public string? Type { get; set; }
    public List<string>? Attributes { get; set; }
}

public class AkahuConnection
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public string? ConnectionType { get; set; }
}

public class AkahuBalance
{
    public decimal Current { get; set; }
    public decimal? Available { get; set; }
    public string? Currency { get; set; }
}

public class AkahuTransactionsResponse
{
    public List<AkahuTransaction> Items { get; set; } = new();
}

public class AkahuTransaction
{
    public string Id { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
    public string? Connection { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal? Balance { get; set; }
    public string? Type { get; set; }
    public AkahuCategory? Category { get; set; }
    public AkahuMerchant? Merchant { get; set; }
    public AkahuTransactionMeta? Meta { get; set; }
}

public class AkahuCategory
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AkahuCategoryGroups? Groups { get; set; }
}

public class AkahuCategoryGroups
{
    public string? PersonalFinance { get; set; }
}

public class AkahuMerchant
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Website { get; set; }
}

public class AkahuTransactionMeta
{
    public string? Logo { get; set; }
    public string? Particulars { get; set; }
    public string? Code { get; set; }
    public string? Reference { get; set; }
    public string? OtherAccount { get; set; }
    public string? CardSuffix { get; set; }
}
