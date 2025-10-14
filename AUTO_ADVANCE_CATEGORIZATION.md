# Auto-Advance Categorization Feature

## Overview
When a user categorizes a transaction, the modal automatically advances to the next uncategorized transaction, allowing for rapid categorization of multiple transactions without closing and reopening the modal.

## Features

### 1. Automatic Merchant Mapping
- When you categorize a transaction, ALL transactions from the same merchant are automatically categorized
- Backend applies cached merchant→category mappings to all transactions when fetching from the database
- Immediate visual feedback in the UI showing all transactions from that merchant in the correct category

### 2. Auto-Advance to Next Transaction
- After categorizing, the modal automatically loads the next uncategorized transaction
- Skips transactions from the merchant you just categorized (they're already updated)
- Seamlessly continues until all transactions are categorized
- Automatically closes when no more uncategorized transactions remain

### 3. Progress Tracking
- Header shows count of remaining uncategorized transactions
- Example: "3 uncategorized transactions remaining"
- Footer shows "Auto-advances to next transaction" when multiple remain
- Real-time updates as you categorize

### 4. Smart Transaction Selection
- Finds next uncategorized transaction in the list
- Excludes transactions from just-categorized merchant
- Falls back to first uncategorized if at the end of the list
- Handles edge cases gracefully

## User Flow

### Before Auto-Advance
1. Click "📝 Categorize" on transaction
2. Select category
3. Modal closes
4. Click "📝 Categorize" on next transaction
5. Select category
6. Repeat...

### With Auto-Advance (NEW)
1. Click "📝 Categorize" on any transaction
2. Select category → **Modal stays open**
3. Next uncategorized transaction loads automatically
4. Select category → **Modal stays open**
5. Next transaction loads
6. Continue until all done → **Modal closes automatically**

**Result**: Much faster workflow! Can categorize 10 transactions with just 10 selections instead of 20 clicks.

## Implementation Details

### Frontend Changes

#### BudgetingView.jsx

**1. Updated `handleCategorySelect`**:
```javascript
const handleCategorySelect = async (category) => {
  // ... save category ...
  
  // Wait for transactions to refresh
  await onTransactionUpdate();
  
  // Find next uncategorized transaction
  const nextUncategorized = findNextUncategorizedTransaction(
    currentTransactionId, 
    merchantName
  );
  
  if (nextUncategorized) {
    // Keep modal open, load next transaction
    setCategorizingTransaction(nextUncategorized);
  } else {
    // All done, close modal
    setCategorizingTransaction(null);
  }
};
```

**2. Added `findNextUncategorizedTransaction`**:
- Gets all transactions from all categories
- Filters to only uncategorized ones
- Excludes transactions from just-categorized merchant
- Returns next in sequence, or null if none remain

**3. Added `countUncategorizedTransactions`**:
- Counts total uncategorized transactions
- Passed to CategoryPicker for progress display

#### CategoryPicker.jsx

**1. Added `remainingCount` prop**:
- Displays in header: "X uncategorized transactions remaining"
- Updates footer when multiple remain
- Provides visual feedback on progress

**2. Updated header layout**:
- Now uses flex layout to accommodate count
- Shows transaction count below title
- Maintains clean, professional appearance

### Backend Changes

#### TransactionController.cs

**1. Added `ApplyCachedCategorizations` method**:
```csharp
private List<Transaction> ApplyCachedCategorizations(List<Transaction> transactions)
{
    foreach (var transaction in transactions)
    {
        var merchantName = transaction.Merchant?.Name ?? transaction.Description;
        var cached = _categorizationService.GetCachedCategorization(merchantName);
        
        if (cached != null)
        {
            // Apply cached category to transaction
            transaction.Category.Id = cached.CategoryId;
            transaction.Category.Name = cached.CategoryName;
            transaction.Category.Groups["personal_finance"] = new CategoryGroup
            {
                Id = cached.GroupId,
                Name = cached.GroupName
            };
        }
    }
    return transactions;
}
```

**2. Updated `GetTransaction` endpoint**:
- Calls `ApplyCachedCategorizations` after fetching from database
- Applies all cached merchant mappings before returning to frontend
- Ensures UI always shows latest categorizations

#### MerchantCategorizationService.cs

**1. Added `GetCachedCategorization` method**:
```csharp
public CategorizationResult? GetCachedCategorization(string merchantName)
{
    return _merchantCache.TryGetValue(merchantName, out var result) ? result : null;
}
```

**2. Added to interface**:
```csharp
CategorizationResult? GetCachedCategorization(string merchantName);
```

## Edge Cases Handled

### 1. No More Uncategorized Transactions
- **Behavior**: Modal closes automatically
- **User Experience**: Clean exit when done

### 2. Just Categorized Merchant Has Multiple Transactions
- **Behavior**: Skips all transactions from that merchant
- **Reason**: They're all updated by backend's cached mapping
- **User Experience**: Moves to next merchant immediately

### 3. Single Uncategorized Transaction
- **Behavior**: Categorize it → modal closes
- **User Experience**: No unnecessary modal persistence

### 4. API Error During Save
- **Behavior**: Shows error, keeps current transaction loaded
- **User Experience**: Can retry without losing progress

### 5. Last Transaction in List
- **Behavior**: Wraps to first uncategorized, or closes if none
- **User Experience**: Seamless continuation or completion

## Benefits

### For Users
✅ **Faster workflow** - Categorize multiple transactions in rapid succession
✅ **Less clicking** - No need to close and reopen modal repeatedly  
✅ **Progress tracking** - Always know how many transactions remain
✅ **Batch processing** - Efficient for categorizing many transactions at once
✅ **Automatic application** - All transactions from same merchant update instantly

### For Developer
✅ **Clean separation** - Backend handles merchant mapping, frontend handles UI flow
✅ **Reusable logic** - `findNextUncategorizedTransaction` can be used elsewhere
✅ **Maintainable** - Clear function responsibilities
✅ **Testable** - Each function has single responsibility

## Future Enhancements

### Potential Improvements
1. **Bulk categorization** - Select multiple merchants at once
2. **Category suggestions** - AI-powered suggestions based on merchant name
3. **Keyboard shortcuts** - Press → to skip to next, ← to go back
4. **Undo functionality** - Revert last categorization
5. **Persistent mappings** - Save merchant→category to database (currently in-memory)
6. **Smart ordering** - Prioritize merchants with most transactions
7. **Preview** - Show all transactions that will be affected before confirming
8. **Batch confirmation** - Review multiple categorizations before saving

## Testing Scenarios

### Happy Path
1. Have 5 uncategorized transactions from different merchants
2. Click "Categorize" on any one
3. Select category
4. Verify modal stays open
5. Verify next transaction loads
6. Repeat until all 5 are done
7. Verify modal closes automatically

### Merchant Grouping
1. Have 3 transactions from "Countdown" (uncategorized)
2. Have 2 transactions from "BP" (uncategorized)
3. Categorize one Countdown transaction
4. Verify: All 3 Countdown transactions update
5. Verify: Modal advances to BP transaction (not another Countdown)
6. Categorize BP transaction
7. Verify: Both BP transactions update
8. Verify: Modal closes (all done)

### Error Handling
1. Start categorizing
2. Disconnect network
3. Select category
4. Verify: Error message shown
5. Verify: Current transaction still loaded
6. Reconnect network
7. Try again
8. Verify: Success, advances to next

## Performance Considerations

### Frontend
- **Transaction lookup**: O(n) where n = total transactions
- **Filtering**: O(n) for each categorization
- **Optimization**: Could cache uncategorized list, update incrementally
- **Impact**: Minimal for typical use (100-1000 transactions)

### Backend
- **Mapping application**: O(n × m) where n = transactions, m = cached merchants
- **Optimization**: Dictionary lookup is O(1), overall still O(n)
- **Impact**: Negligible for typical datasets
- **Future**: Could apply mappings at database query level for larger datasets

## Summary

This feature dramatically improves the categorization workflow by:
1. **Keeping modal open** after each categorization
2. **Auto-loading next** uncategorized transaction
3. **Applying merchant mappings** to all related transactions
4. **Showing progress** with remaining count
5. **Closing automatically** when done

Users can now categorize dozens of transactions in a fraction of the time, with clear progress tracking and automatic application to all transactions from the same merchant.
