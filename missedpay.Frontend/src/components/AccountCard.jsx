const AccountCard = ({ account, isSelected, onClick }) => {
  const formatCurrency = (amount, currency = 'NZD') => {
    return new Intl.NumberFormat('en-NZ', {
      style: 'currency',
      currency: currency,
    }).format(amount);
  };

  return (
    <li 
      onClick={() => onClick(account)}
      style={{ cursor: 'pointer', marginBottom: '10px', border: isSelected ? '2px solid blue' : '1px solid gray', padding: '10px' }}
    >
      <div>
        <span style={{ fontWeight: 'bold' }}>{account.name}</span>
        {' - '}
        <span>{account.connection.name}</span>
        {' '}
        <span>[{account.status}]</span>
      </div>
      
      {account.formattedAccount && (
        <div><span>{account.formattedAccount}</span></div>
      )}
      
      <div>
        <span>Current: {formatCurrency(account.balance.current, account.balance.currency)}</span>
        {account.balance.available !== null && account.balance.available !== undefined && (
          <span> | Available: {formatCurrency(account.balance.available, account.balance.currency)}</span>
        )}
      </div>
      
      <div>
        <span>Type: {account.type}</span>
      </div>
    </li>
  );
};

export default AccountCard;
