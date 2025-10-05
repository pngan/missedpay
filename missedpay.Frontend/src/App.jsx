import { useState, useEffect } from 'react';
import AccountCard from './components/AccountCard';
import TransactionList from './components/TransactionList';
import { accountsApi, transactionsApi } from './services/api';
import './App.css';

function App() {
  const [accounts, setAccounts] = useState([]);
  const [transactions, setTransactions] = useState([]);
  const [selectedAccount, setSelectedAccount] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const [accountsData, transactionsData] = await Promise.all([
        accountsApi.getAll(),
        transactionsApi.getAll(),
      ]);
      setAccounts(accountsData);
      setTransactions(transactionsData);
    } catch (err) {
      setError(err.message);
      console.error('Error loading data:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleAccountClick = (account) => {
    setSelectedAccount(selectedAccount?._id === account._id ? null : account);
  };

  if (loading) {
    return (
      <div className="app-loading">
        <div className="loader"></div>
        <p>Loading your accounts...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="app-error">
        <div className="error-icon">⚠️</div>
        <h2>Oops! Something went wrong</h2>
        <p>{error}</p>
        <button onClick={loadData} className="retry-button">
          Try Again
        </button>
      </div>
    );
  }

  return (
    <div className="app">
      <header className="app-header">
        <div className="header-content">
          <h1 className="app-title">
            <span className="title-icon">💰</span>
            MissedPay
          </h1>
          <button onClick={loadData} className="refresh-button" title="Refresh">
            🔄
          </button>
        </div>
      </header>

      <main className="app-main">
        <div className="content-container">
          <section className="accounts-section">
            <div className="section-header">
              <h2>Your Accounts</h2>
              <span className="account-count">{accounts.length}</span>
            </div>
            
            {accounts.length === 0 ? (
              <div className="empty-state">
                <div className="empty-icon">🏦</div>
                <h3>No accounts found</h3>
                <p>Add your first account to get started</p>
              </div>
            ) : (
              <div className="accounts-grid">
                {accounts.map((account) => (
                  <AccountCard
                    key={account._id}
                    account={account}
                    isSelected={selectedAccount?._id === account._id}
                    onClick={handleAccountClick}
                  />
                ))}
              </div>
            )}
          </section>

          {selectedAccount && (
            <section className="transactions-section">
              <TransactionList
                transactions={transactions}
                accountId={selectedAccount._id}
              />
            </section>
          )}
        </div>
      </main>
    </div>
  );
}

export default App;
