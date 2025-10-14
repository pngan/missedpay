# Transaction Categorization UI

## Overview
This document describes the UI implementation for transaction categorization in the budgeting view. Users can now categorize transactions with minimal keystrokes using keyboard shortcuts.

## Features

### 1. Categorization Button
- **Location**: Added to each transaction in the BudgetingView right panel
- **Appearance**: Blue outlined button with "📝 Categorize" text
- **Behavior**: 
  - Opens category picker modal when clicked
  - Shows loading state ("...") while categorization is in progress
  - Disabled during categorization to prevent multiple clicks

### 2. Category Picker Modal
- **Component**: `CategoryPicker.jsx` with `CategoryPicker.css`
- **Features**:
  - Modal overlay with dark semi-transparent background
  - Search box for filtering categories
  - Single-depth flat list of categories (format: "Category Name - Group Name")
  - Keyboard shortcuts for first 9 items (1-9 keys)
  - ESC key to close modal
  - Shows current category if already categorized
  - Displays merchant name being categorized

### 3. Keyboard Shortcuts
- **1-9 Keys**: Select first 9 visible categories
- **ESC Key**: Close the modal
- **Search**: Filter categories in real-time
- **Enter**: (when searching) Select first result

### 4. Category Display
- Categories are displayed as: `"Supermarkets and grocery stores - Food"`
- Sorted alphabetically for easy scanning
- Number badges (1-9) shown on first 9 items for keyboard shortcuts
- Current category highlighted with yellow background

## Components Modified

### 1. BudgetingView.jsx
**Changes**:
- Added `categorizingTransaction` state to track which transaction is being categorized
- Added `categorizingInProgress` state to show loading state on button
- Added `handleCategorySelect` function to save category selection
- Added "Categorize" button to each transaction item
- Integrated `CategoryPicker` component
- Added `onTransactionUpdate` prop to refresh data after categorization

**New Props**:
- `onTransactionUpdate`: Callback function to refresh transactions after categorization

### 2. CategoryPicker.jsx (NEW)
**Purpose**: Modal component for selecting transaction categories

**Props**:
- `isOpen` (boolean): Controls modal visibility
- `onClose` (function): Callback to close modal
- `onSelect` (function): Callback when category selected
- `merchantName` (string): Merchant being categorized
- `currentCategory` (object): Current category if already set

**Features**:
- Fetches all categories from API on open
- Flattens grouped categories to single-depth list
- Real-time search filtering
- Keyboard event handling (1-9, ESC)
- Loading and error states
- Responsive design

### 3. CategoryPicker.css (NEW)
**Styling**:
- Modern modal with backdrop blur
- Smooth animations (slide up on open)
- Hover states for interactive elements
- Responsive layout
- Custom scrollbar styling
- Keyboard shortcut badges

### 4. api.js
**Added**: `categorizationApi` service with two methods:
- `getAllCategories()`: GET /api/Categorization/categories
- `confirmCategory(merchantName, categoryId, categoryName, groupName)`: POST /api/Categorization/confirm

### 5. App.jsx
**Changes**:
- Pass `loadData` function as `onTransactionUpdate` prop to BudgetingView
- This ensures transactions are refreshed after categorization

## User Flow

1. **Navigate to Budget Tab**: User clicks "Budgeting" tab
2. **Select Category**: User expands a spending group and clicks a category
3. **View Transactions**: Transactions for that category are displayed
4. **Click Categorize**: User clicks "📝 Categorize" button on a transaction
5. **Modal Opens**: CategoryPicker modal appears with search box and category list
6. **Search (Optional)**: User can type to filter categories
7. **Select Category**: 
   - Click on a category, OR
   - Press number key (1-9) for quick selection
8. **Save**: 
   - API call to `/api/Categorization/confirm` saves merchant→category mapping
   - Transaction list refreshes to show updated categorization
   - Modal closes automatically
9. **Future Auto-Categorization**: Next time this merchant appears, it will be auto-categorized

## API Integration

### Endpoint Used: POST /api/Categorization/confirm

**Request Body**:
```json
{
  "merchantName": "Woolworths Auckland",
  "categoryId": "nzfcc_supermarkets_grocery",
  "categoryName": "Supermarkets and grocery stores",
  "groupName": "Food"
}
```

**Response**:
```json
{
  "success": true,
  "message": "Category mapping saved for merchant: Woolworths Auckland"
}
```

**Purpose**:
- Saves merchant→category mapping to database
- Future transactions from this merchant will be auto-categorized
- Mapping is tenant-specific

### Endpoint Used: GET /api/Categorization/categories

**Response**:
```json
{
  "Food": [
    {
      "id": "nzfcc_supermarkets_grocery",
      "name": "Supermarkets and grocery stores"
    },
    ...
  ],
  "Transport": [
    {
      "id": "nzfcc_public_transportation",
      "name": "Public transportation"
    },
    ...
  ],
  ...
}
```

**Purpose**:
- Returns all 65+ Akahu categories grouped by personal finance group
- Used to populate category picker list

## Design Decisions

### 1. Single-Depth List Format
**Why**: User requested "single depth list where the category and groups are combined in pairs"
**Implementation**: Display as "Category Name - Group Name" instead of nested structure
**Benefits**: 
- Easier to scan visually
- No need to expand/collapse groups
- Works well with keyboard shortcuts

### 2. Keyboard Shortcuts (1-9)
**Why**: User requested "minimum number of key strokes. Even down to choosing a single number on the keyboard"
**Implementation**: Number badges on first 9 items, keydown event listener
**Benefits**:
- Power users can categorize in 2 keystrokes: Click button + Number key
- Familiar pattern from many UIs (e.g., Slack channels)

### 3. Search Box
**Why**: With 65+ categories, need quick way to filter
**Implementation**: Real-time filtering on display text, auto-focus on open
**Benefits**:
- Find categories without scrolling
- Works with keyboard shortcuts (filtered results get numbers 1-9)

### 4. Modal vs Dropdown
**Decision**: Use modal instead of dropdown
**Why**: 
- More space for long category names
- Better keyboard navigation
- Can show merchant name and current category
- Search box fits naturally

### 5. Auto-Refresh After Categorization
**Implementation**: Call `onTransactionUpdate` callback to reload all transactions
**Why**: 
- Ensures UI shows updated categorization immediately
- Handles backend auto-categorization of other transactions from same merchant
- Simple and reliable approach

## Testing Scenarios

### Happy Path
1. Click "Categorize" on uncategorized transaction
2. Search for category (e.g., "supermarket")
3. Press "1" to select first result
4. Verify modal closes
5. Verify transaction list refreshes
6. Verify transaction now shows in correct category group

### Keyboard Shortcuts
1. Click "Categorize"
2. Press "5" to select 5th category
3. Verify selection works
4. Click "Categorize" on another transaction
5. Press ESC to close without selecting
6. Verify modal closes without saving

### Search Filtering
1. Click "Categorize"
2. Type "trans" in search box
3. Verify only transport-related categories shown
4. Verify keyboard shortcuts (1-9) apply to filtered results
5. Select category and verify save

### Error Handling
1. Disconnect from API (stop backend)
2. Click "Categorize"
3. Verify error message shown in modal
4. Verify reload button works
5. Reconnect and verify categories load

### Loading States
1. Click "Categorize" quickly on multiple transactions
2. Verify only one modal opens
3. Verify button shows "..." loading state
4. Verify button is disabled during save

## Future Enhancements

### Potential Improvements
1. **Toast Notifications**: Show success/error messages instead of console.log
2. **Undo Functionality**: Allow reverting categorization
3. **Bulk Categorization**: Select multiple transactions and categorize at once
4. **Smart Suggestions**: Use AI to suggest categories based on merchant name
5. **Recent Categories**: Show most recently used categories at top
6. **Category Icons**: Add icons to categories for visual recognition
7. **Keyboard Navigation**: Arrow keys to navigate list
8. **Autocomplete**: Suggest categories as you type in search
9. **Category Stats**: Show transaction count for each category
10. **Merchant History**: Show what category was used for this merchant before

### Integration with AI Categorization
The existing backend has AI categorization endpoints that could be integrated:
- **POST /api/Categorization/suggestions**: Get top N AI suggestions
- Could pre-populate modal with AI suggestions
- Show confidence scores
- Allow user to override AI choice

## Files Changed

### New Files
1. `missedpay.Frontend/src/components/CategoryPicker.jsx` (150 lines)
2. `missedpay.Frontend/src/components/CategoryPicker.css` (200 lines)
3. `CATEGORIZATION_UI.md` (this file)

### Modified Files
1. `missedpay.Frontend/src/components/BudgetingView.jsx`
   - Added imports (CategoryPicker, categorizationApi)
   - Added state (categorizingTransaction, categorizingInProgress)
   - Added handleCategorySelect function
   - Added categorize button to each transaction
   - Added CategoryPicker component

2. `missedpay.Frontend/src/services/api.js`
   - Added categorizationApi with getAllCategories and confirmCategory methods

3. `missedpay.Frontend/src/App.jsx`
   - Pass loadData as onTransactionUpdate to BudgetingView

## Summary

This implementation provides a complete, keyboard-friendly UI for manual transaction categorization. Users can categorize transactions with minimal effort, and the system remembers merchant mappings for automatic categorization of future transactions. The design prioritizes speed and efficiency, with keyboard shortcuts and search functionality making it easy to work with 65+ categories.
