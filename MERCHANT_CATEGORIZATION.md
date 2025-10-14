# Merchant Categorization Service

## Overview

The Merchant Categorization Service provides flexible, extensible transaction categorization based on merchant names. It supports multiple categorization strategies:

1. **AI Categorization** - Uses local Ollama LLM (llama3.1:8b) to automatically categorize merchants
2. **Manual Selection** - User selects from complete list of categories via UI
3. **Hybrid Approach** - AI suggests top 6 categories, user makes final selection

## Architecture

### Core Components

#### 1. `IMerchantCategorizationService`
Main service interface that orchestrates categorization.

**Key Methods:**
- `CategorizeAsync()` - Categorize using specified method (AI, Manual, Hybrid)
- `GetCategorySuggestionsAsync()` - Get AI-powered suggestions for hybrid/manual selection  
- `ConfirmCategorizationAsync()` - Confirm and cache user's category selection
- `GetAllCategoriesGrouped()` - Get all 65+ categories grouped by personal finance category

#### 2. `ICategorySelector` (Strategy Pattern)
Interface for different categorization strategies.

**Implementations:**
- `AiCategorySelector` - AI-powered categorization using Ollama
- `HybridCategorySelector` - Generates suggestions for user selection

#### 3. Category Data Model
- `CategoryMapping` - Category with ID, name, and group
- `CategoryGroups` - Contains personal finance grouping
- `PersonalFinanceGroup` - Group like "Food", "Transport", "Utilities", etc.

### Data Models

```csharp
public record CategorizationResult
{
    string CategoryId;        // e.g., "nzfcc_ckouvvy84001608ml5p6z4d8j"
    string CategoryName;      // e.g., "Supermarkets and grocery stores"
    string GroupId;          // e.g., "group_clasr0ysw000xhk4mf7mg2j1z"
    string GroupName;        // e.g., "Food"
    double Confidence;       // 0.0 - 1.0
    CategorizationMethod Method; // AI, Manual, Hybrid, Cached
}

public record CategorySuggestion
{
    string CategoryId;
    string CategoryName;
    string GroupId;
    string GroupName;
    double Score;           // Relevance score 0.0 - 1.0
    string Reason;          // AI's explanation
}
```

## API Endpoints

### POST `/api/Categorization/categorize`
Categorize a merchant using AI.

**Request:**
```json
{
  "merchantName": "Woolworths",
  "description": "WOOLWORTHS NZ 9235 MT EDEN",
  "amount": -42.00
}
```

**Response:**
```json
{
  "categoryId": "nzfcc_ckouvvy84001608ml5p6z4d8j",
  "categoryName": "Supermarkets and grocery stores",
  "groupId": "group_clasr0ysw000xhk4mf7mg2j1z",
  "groupName": "Food",
  "confidence": 0.95,
  "method": "AI"
}
```

### POST `/api/Categorization/suggestions`
Get top N category suggestions (for hybrid/manual selection).

**Request:**
```json
{
  "merchantName": "Starbucks",
  "description": "Coffee purchase",
  "amount": -6.50,
  "maxSuggestions": 6
}
```

**Response:**
```json
[
  {
    "categoryId": "nzfcc_ckouvvy84001908ml8s9z4d8m",
    "categoryName": "Cafes and bakeries",
    "groupId": "group_clasr0ysw000xhk4mf7mg2j1z",
    "groupName": "Food",
    "score": 0.95,
    "reason": "Starbucks is a well-known cafe chain"
  },
  {
    "categoryId": "nzfcc_ckouvvy84001708ml6q7z4d8k",
    "categoryName": "Restaurants",
    "groupId": "group_clasr0ysw000xhk4mf7mg2j1z",
    "groupName": "Food",
    "score": 0.75,
    "reason": "Could be categorized as dining out"
  }
]
```

### POST `/api/Categorization/confirm`
Confirm category selection (caches for future use).

**Request:**
```json
{
  "merchantName": "Starbucks",
  "categoryId": "nzfcc_ckouvvy84001908ml8s9z4d8m",
  "method": "Manual"
}
```

**Response:**
```json
{
  "message": "Category confirmed successfully"
}
```

### GET `/api/Categorization/categories`
Get all available categories grouped by personal finance group.

**Response:**
```json
{
  "Food": [
    {
      "id": "nzfcc_ckouvvy84001608ml5p6z4d8j",
      "name": "Supermarkets and grocery stores",
      "groups": { "personalFinance": { "id": "...", "name": "Food" } }
    },
    ...
  ],
  "Transport": [...],
  ...
}
```

### GET `/api/Categorization/categories/{groupName}`
Get categories for a specific group (e.g., "Food", "Transport").

## Available Categories

The service includes 65+ Akahu NZ FCC (Financial Category Classification) categories grouped into:

- **Income** - Income, tax refunds, interest, refunds
- **Food** - Supermarkets, restaurants, fast food, cafes, bars
- **Shopping** - General merchandise, clothing, electronics, home, sports, books, pets, online
- **Transport** - Petrol, public transport, parking, taxis, vehicle maintenance
- **Utilities** - Telecom, electricity, gas, water, internet/cable
- **Entertainment** - Movies/streaming, music, gaming, events, books/magazines
- **Health** - Medical, pharmacy, dentist, optometry, gym, health insurance
- **Personal Care** - Hair/beauty, spa/massage
- **Education** - School fees, tertiary, student loans, supplies
- **Housing** - Rent, mortgage, rates, home insurance, maintenance, furniture
- **Financial** - Banking fees, investments, credit cards, loans, insurance, tax, savings
- **Travel** - Accommodation, flights, car rental, travel insurance
- **Charity & Gifts** - Donations, gifts
- **Government & Legal** - Fines, legal services, government services
- **Business** - Business expenses, office supplies
- **Other** - Other expenses, uncategorized

## Usage Examples

### Example 1: AI Categorization
```http
POST http://localhost:5349/api/Categorization/categorize
Content-Type: application/json

{
  "merchantName": "Spark",
  "description": "Spark NZ Trading 29019229371",
  "amount": -112.99
}
```

Result: Category "Telecommunication services", Group "Utilities"

### Example 2: Hybrid Workflow (UI Implementation)

1. User has uncategorized transaction for "BP Service Station"
2. Frontend calls `/api/Categorization/suggestions`:
   ```javascript
   const response = await fetch('/api/Categorization/suggestions', {
     method: 'POST',
     body: JSON.stringify({
       merchantName: "BP",
       description: "BP Service Station",
       amount: -85.00,
       maxSuggestions: 6
     })
   });
   const suggestions = await response.json();
   ```

3. Frontend displays top 6 suggestions to user:
   - ✅ Petrol stations (Transport) - 0.95
   - Vehicle maintenance and repairs (Transport) - 0.70
   - Supermarkets and grocery stores (Food) - 0.15
   - ...

4. User selects "Petrol stations"

5. Frontend confirms selection:
   ```javascript
   await fetch('/api/Categorization/confirm', {
     method: 'POST',
     body: JSON.stringify({
       merchantName: "BP",
       categoryId: "nzfcc_ckouvvy84004508mlabc5e1a",
       method: "Hybrid"
     })
   });
   ```

6. Future transactions from "BP" automatically use cached category

### Example 3: Manual Selection (UI Implementation)

1. User wants to manually categorize "ABC Company"
2. Frontend gets all categories:
   ```javascript
   const categories = await fetch('/api/Categorization/categories').then(r => r.json());
   ```

3. Display grouped dropdown/list:
   ```
   📦 Food
     └ Supermarkets and grocery stores
     └ Restaurants
     └ Fast food and takeaways
     └ Cafes and bakeries
     └ Bars and pubs
   
   🚗 Transport
     └ Petrol stations
     └ Public transport
     └ Parking
     ...
   ```

4. User selects category, frontend calls `/api/Categorization/confirm`

## Caching

The service maintains an in-memory cache of merchant categorizations:
- First categorization (AI/Manual/Hybrid) is cached
- Subsequent transactions from same merchant use cached category
- Cache is per-instance (resets on service restart)
- Future: Can be persisted to database

## Extension Points

### Adding New Categorization Methods

Create a new class implementing `ICategorySelector`:

```csharp
public class RuleBasedCategorySelector : ICategorySelector
{
    public CategorizationMethod Method => CategorizationMethod.RuleBased;
    
    public Task<CategorizationResult?> SelectCategoryAsync(
        string merchantName, 
        string? description, 
        decimal? amount,
        List<CategoryMapping> availableCategories)
    {
        // Implement rule-based logic
        // e.g., if merchantName.Contains("BP") => Petrol stations
    }
}
```

Register in `Program.cs`:
```csharp
builder.Services.AddSingleton<ICategorySelector, RuleBasedCategorySelector>();
```

### Customizing AI Prompts

Modify `AiCategorySelector.BuildCategorizationPrompt()` to adjust how the AI categorizes:
- Add example transactions
- Adjust confidence thresholds
- Include transaction patterns
- Add NZ-specific merchant recognition

### Adding Category Sources

Currently uses hardcoded Akahu NZ FCC categories. Can be extended to:
- Load from JSON file
- Fetch from database
- Pull from external API
- Allow custom user-defined categories

## Testing

Test endpoints available in `missedpay.ApiService.http`:

```http
### Categorize Woolworths
POST {{ApiService_HostAddress}}/api/Categorization/categorize
Content-Type: application/json

{
  "merchantName": "Woolworths",
  "description": "WOOLWORTHS NZ 9235 MT EDEN",
  "amount": -42.00
}

### Get suggestions for Starbucks
POST {{ApiService_HostAddress}}/api/Categorization/suggestions
Content-Type: application/json

{
  "merchantName": "Starbucks",
  "maxSuggestions": 6
}

### Get all categories
GET {{ApiService_HostAddress}}/api/Categorization/categories

### Get Food categories only
GET {{ApiService_HostAddress}}/api/Categorization/categories/Food
```

## Dependencies

- **OllamaSharp** - AI integration via Ollama
- **CommunityToolkit.Aspire.OllamaSharp** - Aspire integration
- **Ollama with llama3.1:8b model** - Local LLM

## Performance Considerations

- **AI Categorization**: ~2-5 seconds (depends on Ollama response time)
- **Suggestions**: ~3-8 seconds (generates multiple suggestions)
- **Cached**: Instant (in-memory lookup)
- **Manual/Categories**: Instant (returns static list)

## Future Enhancements

1. **Persistent Caching** - Store merchant categorizations in database
2. **Learning System** - Learn from user corrections to improve AI accuracy
3. **Bulk Categorization** - Categorize multiple transactions in one request
4. **Category Confidence Tuning** - Adjust confidence thresholds
5. **Multi-Merchant Patterns** - Recognize merchant variations (e.g., "BP" vs "BP Service Station")
6. **Transaction History** - Use past transactions to improve categorization
7. **Custom Categories** - Allow users to define their own categories
8. **Export/Import** - Share categorization rules between users

## Troubleshooting

### AI categorization returns null
- Check Ollama container is running: `docker ps`
- Verify llama3.1:8b model is loaded: `docker exec ollama-xyz ollama list`
- Check API service can reach Ollama (check logs for connection errors)

### Suggestions are poor quality
- AI might need better prompts - edit `BuildSuggestionsPrompt()`
- Consider adding example transactions to the prompt
- Try different models (e.g., llama3.2:3b for faster, llama3.1:70b for better)

### Categories don't match Akahu
- Update `LoadCategories()` with latest Akahu category list
- Akahu categories change occasionally - check their API documentation

## Related Files

- `/Services/MerchantCategorizationService.cs` - Main service
- `/Services/ICategorySelector.cs` - Strategy interface  
- `/Services/AiCategorySelector.cs` - AI implementation
- `/Services/HybridCategorySelector.cs` - Hybrid implementation
- `/Services/CategoryMapping.cs` - Data models
- `/Controllers/CategorizationController.cs` - API endpoints
- `/missedpay.ApiService.http` - Test requests
