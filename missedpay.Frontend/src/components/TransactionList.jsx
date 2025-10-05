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

  const filteredTransactions = accountId
    ? transactions.filter((t) => t._account === accountId)
    : transactions;

  const sortedTransactions = [...filteredTransactions].sort(
    (a, b) => new Date(b.date) - new Date(a.date)
  );

  if (sortedTransactions.length === 0) {
    return (
      <div>
        <h3>No transactions yet</h3>
        <p>Transactions for this account will appear here</p>
      </div>
    );
  }

  return (
    <div>
      <h2>Transactions ({sortedTransactions.length})</h2>
      <ul style={{ listStyle: 'none', padding: 0 }}>
        {sortedTransactions.map((transaction) => (
          <li key={transaction._id} style={{ marginBottom: '15px', padding: '10px', border: '1px solid #ddd' }}>
            <div>
              <span style={{ fontWeight: 'bold' }}>
                {transaction.merchant?.name || transaction.description}
              </span>
              {' - '}
              <span style={{ color: transaction.amount < 0 ? 'red' : 'green' }}>
                {transaction.amount < 0 ? '' : '+'}
                {formatCurrency(transaction.amount)}
              </span>
            </div>
            <div>
              <span>{formatDate(transaction.date)}</span>
              {transaction.category && (
                <span> | Category: {transaction.category.name}</span>
              )}
              {transaction.balance !== null && transaction.balance !== undefined && (
                <span> | Balance: {formatCurrency(transaction.balance)}</span>
              )}
            </div>
            {transaction.meta && (
              <div style={{ fontSize: '0.9em', color: '#666' }}>
                {transaction.meta.particulars && <span>{transaction.meta.particulars} </span>}
                {transaction.meta.reference && <span>{transaction.meta.reference} </span>}
                {transaction.meta.cardSuffix && <span>Card •••• {transaction.meta.cardSuffix}</span>}
              </div>
            )}
          </li>
        ))}
      </ul>
    </div>
  );
};

export default TransactionList;
