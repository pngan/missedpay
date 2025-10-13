const AccountCard = ({ account, isSelected, onClick, transactions = [] }) => {
  const formatCurrency = (amount, currency = 'NZD') => {
    return '$' + new Intl.NumberFormat('en-US', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(amount);
  };

  const getTransactionsLast31Days = () => {
    const cutoffDate = new Date();
    cutoffDate.setDate(cutoffDate.getDate() - 31);
    return transactions.filter(t => new Date(t.date) >= cutoffDate);
  };

  const calculateIncome = () => {
    return getTransactionsLast31Days()
      .filter(t => t.amount > 0)
      .reduce((sum, t) => sum + t.amount, 0);
  };

  const calculateExpenses = () => {
    return getTransactionsLast31Days()
      .filter(t => t.amount < 0)
      .reduce((sum, t) => sum + Math.abs(t.amount), 0);
  };

  const getAccountIcon = (type) => {
    if (type === 'Savings' || account.name.toLowerCase().includes('savings')) {
      return '🏦';
    }
    return '💳';
  };

  const formatAccountNumber = (formatted) => {
    if (!formatted) return '';
    // Convert "12-3456-7890123-00" to "****6789"
    const parts = formatted.split('-');
    if (parts.length >= 3) {
      const accountPart = parts[2];
      return `****${accountPart.slice(-4)}`;
    }
    return formatted;
  };

  return (
    <div 
      onClick={() => onClick(account)}
      style={{ 
        cursor: 'pointer',
        backgroundColor: '#fff',
        border: '1px solid #e5e7eb',
        borderRadius: '12px',
        padding: '20px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        transition: 'all 0.2s ease',
        boxShadow: isSelected ? '0 4px 12px rgba(0, 0, 0, 0.1)' : '0 1px 3px rgba(0, 0, 0, 0.05)',
        transform: isSelected ? 'translateY(-2px)' : 'none',
        borderColor: isSelected ? '#3b82f6' : '#e5e7eb'
      }}
      onMouseEnter={(e) => {
        if (!isSelected) {
          e.currentTarget.style.boxShadow = '0 4px 12px rgba(0, 0, 0, 0.08)';
        }
      }}
      onMouseLeave={(e) => {
        if (!isSelected) {
          e.currentTarget.style.boxShadow = '0 1px 3px rgba(0, 0, 0, 0.05)';
        }
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: '16px', flex: 1 }}>
        <div style={{
          width: '48px',
          height: '48px',
          backgroundColor: '#f3f4f6',
          borderRadius: '8px',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontSize: '24px',
          flexShrink: 0
        }}>
          {getAccountIcon(account.type)}
        </div>
        
        <div style={{ flex: 1 }}>
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

      <div style={{ 
        display: 'flex',
        alignItems: 'center',
        gap: '16px',
        flexShrink: 0
      }}>
        <div style={{ textAlign: 'right' }}>
          <div style={{ 
            fontSize: '18px',
            fontWeight: '600',
            color: '#111',
            marginBottom: '8px'
          }}>
            {formatCurrency(account.balance.current, account.balance.currency)}
          </div>
          <div style={{ 
            display: 'flex',
            gap: '12px',
            fontSize: '12px',
            marginBottom: '4px'
          }}>
            <div style={{ textAlign: 'right' }}>
              <div style={{ color: '#9ca3af', marginBottom: '2px' }}>Income</div>
              <div style={{ color: '#059669', fontWeight: '600' }}>
                {formatCurrency(calculateIncome())}
              </div>
            </div>
            <div style={{ textAlign: 'right' }}>
              <div style={{ color: '#9ca3af', marginBottom: '2px' }}>Expenses</div>
              <div style={{ color: '#dc2626', fontWeight: '600' }}>
                {formatCurrency(calculateExpenses())}
              </div>
            </div>
          </div>
          <div style={{ 
            fontSize: '11px',
            color: '#9ca3af'
          }}>
            {transactions.length} transaction{transactions.length !== 1 ? 's' : ''}
          </div>
        </div>
        
        <div style={{
          fontSize: '18px',
          color: '#9ca3af'
        }}>
          ›
        </div>
      </div>
    </div>
  );
};

export default AccountCard;
