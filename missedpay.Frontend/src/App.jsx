import { useState, useEffect } from 'react';
import AccountCard from './components/AccountCard';
import TransactionList from './components/TransactionList';
import { accountsApi, transactionsApi } from './services/api';

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
      <div>
        <p>Loading your accounts...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div>
        <h2>Oops! Something went wrong</h2>
        <p>{error}</p>
        <button onClick={loadData}>
          Try Again
        </button>
      </div>
    );
  }

  return (
    <div>
      <header>
        <h1>MissedPay</h1>
        <button onClick={loadData} title="Refresh">
          Refresh
        </button>
      </header>

      <main>
        <section>
          <h2>Your Accounts ({accounts.length})</h2>
            
          {accounts.length === 0 ? (
            <div>
              <h3>No accounts found</h3>
              <p>Add your first account to get started</p>
            </div>
          ) : (
            <ul style={{ listStyle: 'none', padding: 0 }}>
              {accounts.map((account) => (
                <AccountCard
                  key={account._id}
                  account={account}
                  isSelected={selectedAccount?._id === account._id}
                  onClick={handleAccountClick}
                />
              ))}
            </ul>
          )}
        </section>

        {selectedAccount && (
          <section>
            <TransactionList
              transactions={transactions}
              accountId={selectedAccount._id}
            />
          </section>
        )}
      </main>
    </div>
  );
}

export default App;
