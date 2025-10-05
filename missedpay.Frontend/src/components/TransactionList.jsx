import './TransactionList.css';

const TransactionList = ({ transactions, accountId }) => {
  const formatCurrency = (amount, currency = 'NZD') => {
    return new Intl.NumberFormat('en-NZ', {
      style: 'currency',
      currency: currency,
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
      return new Intl.DateTimeFormat('en-NZ', {
        day: 'numeric',
        month: 'short',
        year: date.getFullYear() !== today.getFullYear() ? 'numeric' : undefined,
      }).format(date);
    }
  };

  const getTransactionIcon = (type) => {
    const icons = {
      Payment: '💳',
      Transfer: '↔️',
      Eftpos: '🏪',
      Credit: '⬇️',
      Debit: '⬆️',
      Interest: '📈',
      Fee: '📋',
      DirectDebit: '🔄',
      DirectCredit: '💰',
      Atm: '🏧',
      StandingOrder: '📅',
    };
    return icons[type] || '💵';
  };

  const filteredTransactions = accountId
    ? transactions.filter((t) => t._account === accountId)
    : transactions;

  const sortedTransactions = [...filteredTransactions].sort(
    (a, b) => new Date(b.date) - new Date(a.date)
  );

  if (sortedTransactions.length === 0) {
    return (
      <div className="transactions-empty">
        <div className="empty-icon">📭</div>
        <h3>No transactions yet</h3>
        <p>Transactions for this account will appear here</p>
      </div>
    );
  }

  return (
    <div className="transactions-list">
      <div className="transactions-header">
        <h2>Transactions</h2>
        <span className="transaction-count">{sortedTransactions.length}</span>
      </div>

      <div className="transactions-items">
        {sortedTransactions.map((transaction, index) => (
          <div
            key={transaction._id}
            className="transaction-item"
            style={{
              animationDelay: `${index * 0.05}s`,
            }}
          >
            <div className="transaction-icon">
              {transaction.merchant?.name || transaction.category?.name ? (
                <div className="transaction-logo">
                  {transaction.meta?.logo ? (
                    <img src={transaction.meta.logo} alt="" />
                  ) : (
                    <span>{getTransactionIcon(transaction.type)}</span>
                  )}
                </div>
              ) : (
                <span>{getTransactionIcon(transaction.type)}</span>
              )}
            </div>

            <div className="transaction-details">
              <div className="transaction-primary">
                <h4 className="transaction-description">
                  {transaction.merchant?.name || transaction.description}
                </h4>
                <span className={`transaction-amount ${transaction.amount < 0 ? 'negative' : 'positive'}`}>
                  {transaction.amount < 0 ? '' : '+'}
                  {formatCurrency(transaction.amount)}
                </span>
              </div>
              <div className="transaction-secondary">
                <span className="transaction-date">{formatDate(transaction.date)}</span>
                {transaction.category && (
                  <span className="transaction-category">{transaction.category.name}</span>
                )}
                {transaction.balance !== null && transaction.balance !== undefined && (
                  <span className="transaction-balance">
                    Balance: {formatCurrency(transaction.balance)}
                  </span>
                )}
              </div>
              {transaction.meta && (
                <div className="transaction-meta">
                  {transaction.meta.particulars && (
                    <span className="meta-item">{transaction.meta.particulars}</span>
                  )}
                  {transaction.meta.reference && (
                    <span className="meta-item">{transaction.meta.reference}</span>
                  )}
                  {transaction.meta.cardSuffix && (
                    <span className="meta-item">Card •••• {transaction.meta.cardSuffix}</span>
                  )}
                </div>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default TransactionList;
