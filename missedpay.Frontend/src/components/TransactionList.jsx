const TransactionList = ({ transactions, accountId, account }) => {
  const formatCurrency = (amount, currency = 'NZD') => {
    return '$' + new Intl.NumberFormat('en-US', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(amount);
  };

  const formatDate = (dateString) => {
    const date = new Date(dateString);
    const today = new Date();
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);

    if (date.toDateString() === today.toDateString()) {
      return 'Today';
    } else if (date.toDateString() === yesterday.toDateString()) {
      return 'Yesterday';
    } else {
      return new Intl.DateTimeFormat('en-US', {
        day: 'numeric',
        month: 'short',
        year: date.getFullYear() !== today.getFullYear() ? 'numeric' : undefined,
      }).format(date);
    }
  };

  const getTransactionIcon = (transaction) => {
    const type = transaction.type?.toLowerCase() || '';
    const description = transaction.description?.toLowerCase() || '';
    const categoryName = transaction.category?.name?.toLowerCase() || '';
    
    // Check category first
    if (categoryName.includes('food') || categoryName.includes('cafe') || categoryName.includes('restaurant')) {
      return '☕';
    }
    if (categoryName.includes('utilities') || description.includes('electric') || description.includes('bill')) {
      return '⚡';
    }
    if (categoryName.includes('transport') || description.includes('gas') || description.includes('fuel')) {
      return '🚗';
    }
    if (categoryName.includes('income') || description.includes('salary') || description.includes('deposit')) {
      return '🛒';
    }
    if (description.includes('grocery') || description.includes('store')) {
      return '☕';
    }
    if (description.includes('coffee')) {
      return '☕';
    }
    
    // Default based on type
    if (type === 'eftpos' || type === 'payment') {
      return '☕';
    }
    if (type === 'transfer' || type === 'directcredit') {
      return '🛒';
    }
    
    return '💳';
  };

  const getIconBackgroundColor = (amount) => {
    return amount < 0 ? '#fee2e2' : '#d1fae5';
  };

  const getIconColor = (amount) => {
    return amount < 0 ? '#dc2626' : '#059669';
  };

  const formatAccountNumber = (formatted) => {
    if (!formatted) return '';
    const parts = formatted.split('-');
    if (parts.length >= 3) {
      const accountPart = parts[2];
      return `****${accountPart.slice(-4)}`;
    }
    return formatted;
  };

  const filteredTransactions = accountId
    ? transactions.filter((t) => t._account === accountId)
    : transactions;

  const sortedTransactions = [...filteredTransactions].sort(
    (a, b) => new Date(b.date) - new Date(a.date)
  );

  if (sortedTransactions.length === 0) {
    return (
      <div style={{
        textAlign: 'center',
        padding: '40px',
        backgroundColor: '#f9fafb',
        borderRadius: '12px'
      }}>
        <h3 style={{ fontSize: '18px', marginBottom: '8px', color: '#111' }}>No transactions yet</h3>
        <p style={{ fontSize: '14px', color: '#6b7280' }}>Transactions for this account will appear here</p>
      </div>
    );
  }

  return (
    <div>
      {/* Expanded Account Card */}
      {account && (
        <div style={{
          backgroundColor: '#f3f4f6',
          border: '1px solid #e5e7eb',
          borderRadius: '12px',
          padding: '20px',
          marginBottom: '24px',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between'
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
            <div style={{
              width: '48px',
              height: '48px',
              backgroundColor: '#fff',
              borderRadius: '8px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: '24px'
            }}>
              💳
            </div>
            
            <div>
              <div style={{ marginBottom: '4px' }}>
                <span style={{ 
                  fontSize: '16px',
                  fontWeight: '600',
                  color: '#111',
                  marginRight: '8px'
                }}>
                  {account.name}
                </span>
                <span style={{ 
                  fontSize: '14px',
                  color: '#6b7280'
                }}>
                  {formatAccountNumber(account.formattedAccount)}
                </span>
              </div>
              <div style={{ 
                fontSize: '14px',
                color: '#9ca3af'
              }}>
                {account.type}
              </div>
            </div>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
            <div style={{ textAlign: 'right' }}>
              <div style={{ 
                fontSize: '18px',
                fontWeight: '600',
                color: '#111',
                marginBottom: '2px'
              }}>
                {formatCurrency(account.balance.current, account.balance.currency)}
              </div>
              <div style={{ 
                fontSize: '13px',
                color: '#6b7280'
              }}>
                {sortedTransactions.length} transaction{sortedTransactions.length !== 1 ? 's' : ''}
              </div>
            </div>
            
            <div style={{
              fontSize: '18px',
              color: '#9ca3af',
              transform: 'rotate(90deg)'
            }}>
              ›
            </div>
          </div>
        </div>
      )}

      <h2 style={{
        fontSize: '20px',
        fontWeight: '600',
        color: '#111',
        marginBottom: '16px'
      }}>
        Recent Transactions
      </h2>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
        {sortedTransactions.map((transaction) => (
          <div 
            key={transaction._id} 
            style={{ 
              backgroundColor: '#fff',
              border: '1px solid #e5e7eb',
              borderRadius: '8px',
              padding: '12px 16px',
              display: 'flex',
              alignItems: 'center',
              gap: '12px',
              transition: 'box-shadow 0.2s ease'
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.boxShadow = '0 4px 12px rgba(0, 0, 0, 0.08)';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.boxShadow = 'none';
            }}
          >
            <div style={{
              width: '32px',
              height: '32px',
              backgroundColor: getIconBackgroundColor(transaction.amount),
              borderRadius: '6px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: '16px',
              flexShrink: 0
            }}>
              <span style={{
                filter: 'grayscale(1)',
                opacity: 0.8
              }}>
                {transaction.amount < 0 ? '↗' : '↙'}
              </span>
            </div>

            <span style={{ fontSize: '16px', flexShrink: 0 }}>{getTransactionIcon(transaction)}</span>

            <span style={{ 
              fontSize: '15px',
              fontWeight: '500',
              color: '#111',
              flex: '1',
              minWidth: '0',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap'
            }}>
              {transaction.merchant?.name || transaction.description}
            </span>

            <span style={{ 
              fontSize: '13px',
              color: '#9ca3af',
              flexShrink: 0
            }}>
              {formatDate(transaction.date)}
            </span>

            {transaction.category && (
              <span style={{ 
                fontSize: '13px',
                color: '#6b7280',
                flexShrink: 0,
                paddingLeft: '8px',
                borderLeft: '1px solid #e5e7eb'
              }}>
                {transaction.category.name}
              </span>
            )}

            <div style={{ 
              fontSize: '15px',
              fontWeight: '600',
              color: transaction.amount < 0 ? '#dc2626' : '#059669',
              textAlign: 'right',
              flexShrink: 0,
              minWidth: '80px'
            }}>
              {transaction.amount < 0 ? '-' : ''}
              {formatCurrency(Math.abs(transaction.amount))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default TransactionList;
