# UI Component Reference Guide

This document provides a complete reference of all named UI elements in the MissedPay application. Use these IDs and data attributes when making specific requests about UI components.

## Component Hierarchy

```
app-container
├── top-navigation-bar
│   ├── view-navigation
│   │   ├── accounts-tab
│   │   └── budgeting-tab
│   ├── refresh-button
│   └── refresh-status-message
├── page-header
└── Main Content
    ├── accounts-view (when activeView === 'accounts')
    │   ├── accounts-panel
    │   │   ├── total-balance-card
    │   │   └── accounts-list-card
    │   │       └── account-card-{accountId} (multiple)
    │   └── transactions-panel
    │       └── transactions-card
    │           └── transaction-list-container
    └── budgeting-view (when activeView === 'budgeting')
```

---

## App Container (`App.jsx`)

### Root Container
- **ID**: `app-container`
- **Component**: `app`
- **Description**: Main application container

### Navigation Bar
- **ID**: `top-navigation-bar`
- **Component**: `navigation`
- **Description**: Sticky navigation bar at top
- **Children**:
  - **Logo/Title**: "MissedPay" text
  - **View Navigation** (`view-navigation`)
    - `accounts-tab` - Accounts view tab button
    - `budgeting-tab` - Budgeting view tab button
  - **Refresh Button** (`refresh-button`)
    - Syncs data from Akahu
    - Has `data-refreshing` attribute (true/false)

### Status Messages
- **ID**: `refresh-status-message`
- **Element**: `status-message`
- **Attributes**: 
  - `data-status-type`: "success" or "error"
- **Description**: Shows refresh operation results

### Page Header
- **ID**: `page-header`
- **Component**: `page-header`
- **Attributes**: 
  - `data-active-view`: "accounts" or "budgeting"
- **Description**: Page title and description

---

## Accounts View

### Main Container
- **ID**: `accounts-view`
- **Component**: `accounts-view`
- **Description**: Container for accounts and transactions panels

### Left Panel - Accounts

#### Accounts Panel Container
- **ID**: `accounts-panel`
- **Panel**: `accounts`
- **Description**: Left sidebar with balance summary and account list

#### Total Balance Card
- **ID**: `total-balance-card`
- **Component**: `balance-summary`
- **Description**: Shows total balance across all accounts
- **Contains**:
  - Total balance amount
  - Total income (last 31 days)
  - Total expenses (last 31 days)

#### Accounts List
- **ID**: `accounts-list-card`
- **Component**: `accounts-list`
- **Description**: Container for all account cards
- **Children**: Multiple `AccountCard` components

### Right Panel - Transactions

#### Transactions Panel Container
- **ID**: `transactions-panel`
- **Panel**: `transactions`
- **Attributes**:
  - `data-account-id`: Currently selected account ID
- **Description**: Right panel showing transaction list

#### Transactions Card
- **ID**: `transactions-card`
- **Component**: `transactions-container`
- **Description**: Wrapper for TransactionList component
- **Children**: `TransactionList` component

---

## AccountCard Component (`AccountCard.jsx`)

Each account card has the following structure:

### Card Container
- **ID**: `account-card-{accountId}`
  - Example: `account-card-acc_cmgj3enx9000202l76w565jyb`
- **Component**: `account-card`
- **Attributes**:
  - `data-account-id`: Akahu account ID
  - `data-account-name`: Account name (e.g., "Everyday Account")
  - `data-account-type`: Account type (e.g., "Savings", "Transaction")
  - `data-is-selected`: Boolean indicating if card is selected

### Card Elements

#### Account Icon
- **Element**: `account-icon`
- **Description**: Visual icon for account (💳 or 🏦)

#### Account Info
- **Element**: `account-info`
- **Contains**:
  - **Account Name** (`account-name`): Full account name
  - **Account Number** (`account-number`): Masked account number (e.g., "****6789")
  - **Account Type** (`account-type`): Type label

#### Account Summary
- **Element**: `account-summary`
- **Contains**:
  - **Account Balance** (`account-balance`): Current balance
  - **Income/Expenses** (`account-income-expenses`):
    - Income Summary (`income-summary`)
      - Income Amount (`income-amount`)
    - Expenses Summary (`expenses-summary`)
      - Expenses Amount (`expenses-amount`)
  - **Transaction Count** (`transaction-count`): Number of transactions

#### Chevron Icon
- **Element**: `chevron-icon`
- **Description**: Navigation arrow (›)

---

## TransactionList Component (`TransactionList.jsx`)

### Main Container
- **ID**: `transaction-list-container`
- **Component**: `transaction-list`
- **Description**: Container for all transaction-related elements

### Empty State
- **ID**: `transaction-list-empty-state`
- **Component**: `empty-state`
- **Description**: Shown when no transactions exist

### Account Header (when account selected)
- **ID**: `transaction-list-account-header`
- **Component**: `account-header`
- **Attributes**: 
  - `data-account-id`: Currently displayed account
- **Contains**:
  - **Account Icon** (`account-header-icon`)
  - **Account Details** (`account-header-details`):
    - Account Name (`account-header-name`)
    - Account Number (`account-header-number`)
    - Account Type (`account-header-type`)
  - **Account Summary** (`account-header-summary`):
    - Balance (`account-header-balance`)
    - Transaction Count (`account-header-transaction-count`)
  - **Chevron** (`account-header-chevron`)

### Section Title
- **ID**: `transaction-list-title`
- **Element**: `section-title`
- **Content**: "Recent Transactions (Last 31 Days)"

### Transaction Items Container
- **ID**: `transaction-list-items`
- **Component**: `transaction-items`
- **Description**: Container for all transaction rows

### Individual Transaction Item
- **ID**: `transaction-item-{transactionId}`
  - Example: `transaction-item-trans_cmgj3f1q8062c02l4cxg20s5d`
- **Component**: `transaction-item`
- **Attributes**:
  - `data-transaction-id`: Akahu transaction ID
  - `data-merchant`: Merchant name (if available)
  - `data-amount`: Transaction amount
  - `data-category`: Category name (if categorized)

#### Transaction Item Elements
Each transaction row contains:

1. **Direction Indicator** (`transaction-direction-indicator`)
   - Arrow showing income (↙) or expense (↗)
   - Background color: red for expenses, green for income

2. **Category Icon** (`transaction-category-icon`)
   - Emoji representing transaction category
   - Examples: ☕ (food), 🚗 (transport), ⚡ (utilities), 💳 (general)

3. **Description** (`transaction-description`)
   - Merchant name or transaction description
   - Truncated if too long

4. **Date** (`transaction-date`)
   - Formatted date: "Today", "Yesterday", or "Oct 9"

5. **Category Name** (`transaction-category-name`)
   - Only shown if transaction is categorized
   - Examples: "Supermarkets and grocery stores", "Petrol stations"

6. **Amount** (`transaction-amount`)
   - Transaction amount with currency formatting
   - Red for expenses (with minus sign)
   - Green for income

---

## Common Prompts Using These IDs

### Example Prompts

1. **"Add a button to `transaction-item` for categorizing transactions"**
   - This will add a button inside each transaction row

2. **"Change the color of `account-header-balance` to blue"**
   - Modifies the balance display style

3. **"Add a dropdown menu to `transaction-category-name`"**
   - Adds interactive categorization to the category label

4. **"Show a modal when clicking `refresh-button`"**
   - Adds functionality to the refresh action

5. **"Add filtering controls above `transaction-list-items`"**
   - Adds filter UI before the transaction list

6. **"Make `account-card` draggable"**
   - Adds drag-and-drop to account cards

7. **"Add an edit icon to `account-header-name`"**
   - Adds inline editing capability

8. **"Show a tooltip on `transaction-category-icon`"**
   - Adds hover information

9. **"Add bulk selection checkboxes to each `transaction-item`"**
   - Enables multi-select transactions

10. **"Create a search bar above `accounts-list-card`"**
    - Adds account filtering

---

## Data Attributes Reference

### Common Patterns

#### Selection State
- `data-is-selected="true"` or `"false"`
- Used on: `account-card`

#### Active State
- `data-active="true"` or `"false"`
- Used on: `accounts-tab`, `budgeting-tab`

#### Loading State
- `data-refreshing="true"` or `"false"`
- Used on: `refresh-button`

#### View State
- `data-active-view="accounts"` or `"budgeting"`
- Used on: `page-header`

#### Component Type
- `data-component="[component-name]"`
- Used on: All major components for CSS targeting

#### Element Role
- `data-element="[element-name]"`
- Used on: Sub-elements within components

#### Panel Type
- `data-panel="accounts"` or `"transactions"`
- Used on: Major panel containers

---

## CSS Selectors Examples

```css
/* Target all account cards */
[data-component="account-card"] { }

/* Target selected account card */
[data-component="account-card"][data-is-selected="true"] { }

/* Target all transaction items */
[data-component="transaction-item"] { }

/* Target specific transaction by ID */
#transaction-item-trans_abc123 { }

/* Target all transaction amounts */
[data-element="transaction-amount"] { }

/* Target expense transactions (negative amounts) */
[data-component="transaction-item"][data-amount^="-"] { }

/* Target active tab */
[data-tab][data-active="true"] { }

/* Target refresh button when refreshing */
#refresh-button[data-refreshing="true"] { }
```

---

## JavaScript Selectors Examples

```javascript
// Get specific transaction
const transaction = document.getElementById('transaction-item-trans_abc123');

// Get all transaction items
const transactions = document.querySelectorAll('[data-component="transaction-item"]');

// Get selected account card
const selectedCard = document.querySelector('[data-component="account-card"][data-is-selected="true"]');

// Get all uncategorized transactions
const uncategorized = document.querySelectorAll('[data-component="transaction-item"]:not([data-category])');

// Get transaction by merchant
const woolworthsTransactions = document.querySelectorAll('[data-merchant="Woolworths"]');

// Get refresh button
const refreshButton = document.getElementById('refresh-button');

// Check if refreshing
const isRefreshing = refreshButton.dataset.refreshing === 'true';
```

---

## Future Components (To Be Added)

When requesting new features, consider these areas:

### Categorization UI
- Category dropdown selector
- AI suggestion list
- Confirmation modal
- Bulk categorization interface

### Filtering & Search
- Transaction search bar
- Date range picker
- Category filter dropdown
- Amount range slider

### Analytics
- Spending charts
- Category breakdown pie chart
- Income vs expenses trend line
- Monthly comparison

### Settings
- Account management
- Category customization
- Budget configuration
- Notification preferences

---

## Notes for Prompts

When making UI requests, it's helpful to:

1. **Be specific about the container**: 
   - ✅ "Add a button inside `transaction-item`"
   - ❌ "Add a button for transactions"

2. **Reference existing elements**:
   - ✅ "Place the dropdown next to `transaction-category-name`"
   - ❌ "Add a dropdown somewhere"

3. **Specify the data you need**:
   - ✅ "Use `data-transaction-id` to identify the transaction"
   - ❌ "Somehow identify the transaction"

4. **Mention related components**:
   - ✅ "When clicking `account-card`, update `transactions-panel`"
   - ❌ "Make accounts clickable"

5. **Reference states**:
   - ✅ "Show the modal when `data-refreshing='true'`"
   - ❌ "Show modal while loading"
