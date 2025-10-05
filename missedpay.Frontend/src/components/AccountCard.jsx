import './AccountCard.css';

const AccountCard = ({ account, isSelected, onClick }) => {
  const formatCurrency = (amount, currency = 'NZD') => {
    return new Intl.NumberFormat('en-NZ', {
      style: 'currency',
      currency: currency,
    }).format(amount);
  };

  return (
    <div 
      className={`account-card ${isSelected ? 'selected' : ''}`}
      onClick={() => onClick(account)}
    >
      <div className="account-header">
        <div className="account-logo">
          {account.connection.logo ? (
            <img src={account.connection.logo} alt={account.connection.name} />
          ) : (
            <div className="account-logo-placeholder">
              {account.connection.name.charAt(0)}
            </div>
          )}
        </div>
        <div className="account-info">
          <h3 className="account-name">{account.name}</h3>
          <p className="account-bank">{account.connection.name}</p>
        </div>
        <div className={`account-status ${account.status.toLowerCase()}`}>
          {account.status}
        </div>
      </div>
      
      <div className="account-details">
        {account.formattedAccount && (
          <p className="account-number">{account.formattedAccount}</p>
        )}
        <div className="account-balance">
          <div className="balance-main">
            <span className="balance-label">Current Balance</span>
            <span className="balance-amount">
              {formatCurrency(account.balance.current, account.balance.currency)}
            </span>
          </div>
          {account.balance.available !== null && account.balance.available !== undefined && (
            <div className="balance-available">
              <span className="balance-label">Available</span>
              <span className="balance-amount">
                {formatCurrency(account.balance.available, account.balance.currency)}
              </span>
            </div>
          )}
        </div>
      </div>

      <div className="account-type">
        <span className="type-badge">{account.type}</span>
      </div>
    </div>
  );
};

export default AccountCard;
