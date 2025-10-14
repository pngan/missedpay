# Auto-Advance Categorization - Bug Fix

## Problem
The modal was closing after categorization instead of staying open and loading the next transaction.

## Root Cause
When `onTransactionUpdate()` was called to refresh data:
1. Parent component (`App.jsx`) fetched fresh transactions from API
2. Updated the `transactions` prop passed to `BudgetingView`
3. This caused `BudgetingView` to re-render with new transaction objects
4. The `categorizingTransaction` state held a reference to an OLD transaction object
5. When trying to set `categorizingTransaction` to the "next" transaction, the object reference was stale
6. The modal closed because the state update didn't work properly with stale references

## Solution
Instead of storing the entire transaction object, we now:

1. **Store transaction ID separately**: Added `nextTransactionId` state
2. **Find next ID BEFORE refresh**: Get the ID of the next uncategorized transaction before data changes
3. **Let refresh complete**: Call `onTransactionUpdate()` and let parent refresh finish
4. **React to refresh with useEffect**: When data updates, useEffect finds the transaction by ID
5. **Load fresh transaction**: Set `categorizingTransaction` with the fresh object from new data

### Code Changes

#### 1. Added new state
```javascript
const [nextTransactionId, setNextTransactionId] = useState(null);
```

#### 2. Added useEffect to handle transaction loading
```javascript
useEffect(() => {
  if (nextTransactionId && groupAggregates.length > 0) {
    // Find the transaction by ID in the refreshed data
    let foundTransaction = null;
    
    for (const group of groupAggregates) {
      for (const category of Object.values(group.categories)) {
        foundTransaction = category.transactions.find(t => t._id === nextTransactionId);
        if (foundTransaction) break;
      }
      if (foundTransaction) break;
    }
    
    if (foundTransaction) {
      setCategorizingTransaction(foundTransaction);
    }
    
    setNextTransactionId(null);
  }
}, [nextTransactionId, groupAggregates]);
```

#### 3. Modified findNextUncategorizedTransaction to return ID
Changed from:
```javascript
return uncategorizedTransactions[currentIndex + 1];
```

To:
```javascript
return uncategorizedTransactions[currentIndex + 1]._id;
```

#### 4. Updated handleCategorySelect workflow
```javascript
// OLD: Find next transaction object after refresh (uses stale data)
await onTransactionUpdate();
await setTimeout(100);
const nextTransaction = findNext(...);
setCategorizingTransaction(nextTransaction); // ❌ Stale object reference

// NEW: Find next ID before refresh, load after refresh
const nextId = findNext(...);  // Returns ID, not object
await onTransactionUpdate();
setNextTransactionId(nextId);  // ✅ useEffect will load fresh object
```

## Benefits
- **Eliminates timing issues**: No more race conditions with state updates
- **Uses fresh data**: Transaction object always comes from refreshed data
- **Proper React patterns**: Uses useEffect to react to prop changes
- **Reliable state synchronization**: ID-based tracking prevents stale references

## Testing
To verify the fix works:
1. Open the budget tab
2. Click categorize on an uncategorized transaction
3. Select a category (or use keyboard shortcut 1-9)
4. Modal should stay open ✅
5. Next uncategorized transaction should load ✅
6. Count should update ✅
7. Repeat until all categorized
8. Modal should close when no more transactions ✅
